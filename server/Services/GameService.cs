using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using TamQuoc3D.Server.Contracts;

namespace TamQuoc3D.Server.Services;

public sealed class GameService(
    NpgsqlDataSource dataSource,
    IConfiguration configuration,
    ILogger<GameService> logger) : IGameService
{
    /// <summary>Hero level is derived from total EXP: +1 level per 300 EXP, capped at 60.</summary>
    internal static int LevelFromExp(long exp) => (int)Math.Min(60, 1 + Math.Max(0, exp) / 300);

    private static readonly string[][] HeroPool =
    [
        ["lu_bo", "quan_vu"],
        ["trieu_van", "gia_cat_luong", "chu_du"],
        ["dieu_thuyen", "dien_vi", "tao_nhan", "hoang_trung", "ma_sieu"],
        ["hoang_cai", "sai_van_co", "bang_thong", "tieu_kieu"]
    ];

    private static readonly string[] OverviewTables =
    [
        "users", "user_auth_providers", "user_profiles", "user_wallets",
        "user_heroes", "user_formations", "user_inventories", "user_equipments",
        "campaign_progress", "tower_progress", "daily_dungeon_records",
        "user_gacha_pity", "gacha_audit_logs", "iap_transactions",
        "arena_profiles", "battle_replays", "system_mails", "mail_recipients", "system_configs",
        "user_password_credentials", "user_sessions"
    ];

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken)
    {
        var databaseConnected = false;
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1;");
            databaseConnected = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        }
        catch (NpgsqlException exception)
        {
            logger.LogWarning(exception, "PostgreSQL health probe failed.");
        }

        var redisConnected = await RedisIsReachableAsync(cancellationToken);
        return new HealthResponse(
            databaseConnected && redisConnected ? "healthy" : "degraded",
            databaseConnected,
            redisConnected,
            "1.0.0",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
    }

    public async Task<object> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await QuerySingleAsync("""
            SELECT u.id, u.status, p.display_name, p.avatar_id, p.frame_id,
                   p.level, p.exp, p.vip_level, p.combat_power, p.last_login_at
            FROM users u
            JOIN user_profiles p ON p.user_id = u.id
            WHERE u.id = @user_id;
            """, cancellationToken, ("user_id", userId));
        return profile ?? throw new KeyNotFoundException("Player not found.");
    }

    public async Task<object> GetWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await QuerySingleAsync("""
            SELECT silver, gold, paid_gold, stamina, max_stamina, arena_tokens,
                   last_stamina_recovery_at, updated_at
            FROM user_wallets WHERE user_id = @user_id;
            """, cancellationToken, ("user_id", userId));
        return wallet ?? throw new KeyNotFoundException("Wallet not found.");
    }

    public async Task<object> ClaimStaminaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await QuerySingleAsync("""
            UPDATE user_wallets
            SET stamina = LEAST(max_stamina, stamina + 20),
                last_stamina_recovery_at = NOW()
            WHERE user_id = @user_id
            RETURNING stamina, max_stamina;
            """, cancellationToken, ("user_id", userId));
        return wallet ?? throw new KeyNotFoundException("Wallet not found.");
    }

    public Task<IReadOnlyList<object>> GetHeroesAsync(Guid userId, CancellationToken cancellationToken) =>
        QueryListAsync("""
            SELECT uh.id, uh.hero_id,
                   COALESCE(NULLIF(uh.custom_name, ''), definition.hero_name) AS hero_name,
                   definition.title,
                   definition.element, definition.position, definition.rarity,
                   definition.base_stats, uh.star, uh.level, uh.exp, uh.is_commander,
                   uh.created_at, uh.updated_at,
                   COALESCE((
                       SELECT jsonb_object_agg(
                           CASE skill.skill_slot WHEN 'NORMAL' THEN 'normal' ELSE 'skill' END,
                           owned_skill.level)
                       FROM user_hero_skills owned_skill
                       JOIN hero_skills skill ON skill.hero_id = owned_skill.hero_id
                                              AND skill.skill_id = owned_skill.skill_id
                       WHERE owned_skill.user_hero_id = uh.id
                   ), '{}'::jsonb) AS skill_levels,
                   COALESCE((
                       SELECT jsonb_agg(owned_talent.talent_id ORDER BY owned_talent.talent_id)
                       FROM user_hero_talents owned_talent
                       WHERE owned_talent.user_hero_id = uh.id
                   ), '[]'::jsonb) AS unlocked_talents,
                   COALESCE((
                       SELECT jsonb_agg(jsonb_build_object(
                           'skill_id', skill.skill_id,
                           'name', skill.skill_name,
                           'slot', skill.skill_slot,
                           'type', skill.targeting_type,
                           'morale_cost', skill.morale_cost,
                           'damage_multiplier', skill.damage_multiplier,
                           'is_magic', skill.is_magic,
                           'target_count', skill.target_count,
                           'effects', skill.effects,
                           'level', owned_skill.level
                       ) ORDER BY skill.skill_slot)
                       FROM user_hero_skills owned_skill
                       JOIN hero_skills skill ON skill.hero_id = owned_skill.hero_id
                                              AND skill.skill_id = owned_skill.skill_id
                       WHERE owned_skill.user_hero_id = uh.id
                   ), '[]'::jsonb) AS skills
            FROM user_heroes uh
            JOIN hero_definitions definition ON definition.hero_id = uh.hero_id
            WHERE uh.user_id = @user_id
            ORDER BY uh.is_commander DESC, uh.level DESC, uh.star DESC;
            """, cancellationToken, ("user_id", userId));

    public Task<IReadOnlyList<object>> GetFormationsAsync(Guid userId, CancellationToken cancellationToken) =>
        QueryListAsync("""
            SELECT formation_type, grid_layout, is_active, updated_at
            FROM user_formations WHERE user_id = @user_id;
            """, cancellationToken, ("user_id", userId));

    public async Task<object> UpdateFormationAsync(
        Guid userId,
        FormationUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FormationType) ||
            request.FormationType.Length > 30 ||
            !HasValidGrid(request.GridLayout))
        {
            throw new InvalidGameRequestException("Formation type or 3x3 grid layout is invalid.");
        }

        var layout = JsonSerializer.Serialize(request.GridLayout);
        var updated = await QuerySingleJsonAsync("""
            INSERT INTO user_formations (user_id, formation_type, grid_layout, is_active)
            VALUES (@user_id, @formation_type, @grid_layout, true)
            ON CONFLICT (user_id, formation_type)
            DO UPDATE SET grid_layout = EXCLUDED.grid_layout, updated_at = NOW()
            RETURNING formation_type, grid_layout, updated_at;
            """, cancellationToken,
            ("user_id", userId),
            ("formation_type", request.FormationType),
            ("grid_layout", layout));
        return updated ?? throw new InvalidOperationException("Formation save did not return a record.");
    }

    public Task<IReadOnlyList<object>> GetCampaignProgressAsync(Guid userId, CancellationToken cancellationToken) =>
        QueryListAsync("""
            SELECT stage_id, stars, first_clear_claimed, clear_count, best_record_turns, updated_at
            FROM campaign_progress WHERE user_id = @user_id ORDER BY stage_id ASC;
            """, cancellationToken, ("user_id", userId));

    public async Task<object> ClearCampaignStageAsync(
        Guid userId,
        CampaignClearRequest request,
        CancellationToken cancellationToken)
    {
        // NOTE: rewards and stamina cost come ONLY from the stage_rewards table.
        // The client reports stage_id/stars/turns; it can no longer mint currency
        // by sending inflated reward values (previous behaviour).
        if (string.IsNullOrWhiteSpace(request.StageId) ||
            request.StageId.Length > 50 ||
            request.Stars is < 0 or > 3 ||
            request.Turns < 1)
        {
            throw new InvalidGameRequestException("Campaign clear request contains invalid values.");
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int staminaCost;
        int firstClearSilver;
        int firstClearGold;
        int normalSilver;
        int firstClearExp;
        int normalExp;
        await using (var rewardCommand = new NpgsqlCommand("""
            SELECT stamina_cost, first_clear_silver, first_clear_gold, normal_silver,
                   first_clear_exp, normal_exp
            FROM stage_rewards WHERE stage_id = @stage_id;
            """, connection, transaction))
        {
            rewardCommand.Parameters.AddWithValue("stage_id", request.StageId);
            await using var reader = await rewardCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidGameRequestException("Unknown campaign stage.");
            }
            staminaCost = reader.GetInt32(0);
            firstClearSilver = reader.GetInt32(1);
            firstClearGold = reader.GetInt32(2);
            normalSilver = reader.GetInt32(3);
            firstClearExp = reader.GetInt32(4);
            normalExp = reader.GetInt32(5);
        }

        bool firstClear;
        await using (var progressCommand = new NpgsqlCommand(
            "SELECT first_clear_claimed FROM campaign_progress WHERE user_id = @user_id AND stage_id = @stage_id FOR UPDATE;",
            connection, transaction))
        {
            progressCommand.Parameters.AddWithValue("user_id", userId);
            progressCommand.Parameters.AddWithValue("stage_id", request.StageId);
            var claimed = await progressCommand.ExecuteScalarAsync(cancellationToken);
            firstClear = claimed is null or DBNull || !Convert.ToBoolean(claimed);
        }

        var rewardSilver = firstClear ? firstClearSilver : normalSilver;
        var rewardGold = firstClear ? firstClearGold : 0;

        await using (var lockWallet = new NpgsqlCommand(
            "SELECT stamina FROM user_wallets WHERE user_id = @user_id FOR UPDATE;",
            connection, transaction))
        {
            lockWallet.Parameters.AddWithValue("user_id", userId);
            var stamina = await lockWallet.ExecuteScalarAsync(cancellationToken);
            if (stamina is null || Convert.ToInt32(stamina) < staminaCost)
            {
                throw new InvalidGameRequestException("Không đủ Thể Lực để vượt ải!");
            }
        }

        await using (var updateWallet = new NpgsqlCommand("""
            UPDATE user_wallets
            SET stamina = stamina - @stamina_cost,
                silver = silver + @gold_reward,
                gold = gold + @diamond_reward,
                updated_at = NOW()
            WHERE user_id = @user_id;
            """, connection, transaction))
        {
            updateWallet.Parameters.AddWithValue("stamina_cost", staminaCost);
            updateWallet.Parameters.AddWithValue("gold_reward", rewardSilver);
            updateWallet.Parameters.AddWithValue("diamond_reward", rewardGold);
            updateWallet.Parameters.AddWithValue("user_id", userId);
            await updateWallet.ExecuteNonQueryAsync(cancellationToken);
        }

        object stage;
        await using (var updateProgress = new NpgsqlCommand("""
            INSERT INTO campaign_progress
                (user_id, stage_id, stars, first_clear_claimed, clear_count, best_record_turns)
            VALUES (@user_id, @stage_id, @stars, true, 1, @turns)
            ON CONFLICT (user_id, stage_id) DO UPDATE
            SET stars = GREATEST(campaign_progress.stars, EXCLUDED.stars),
                clear_count = campaign_progress.clear_count + 1,
                best_record_turns = LEAST(COALESCE(campaign_progress.best_record_turns, 999), EXCLUDED.best_record_turns),
                updated_at = NOW()
            RETURNING stage_id, stars, clear_count, best_record_turns;
            """, connection, transaction))
        {
            updateProgress.Parameters.AddWithValue("user_id", userId);
            updateProgress.Parameters.AddWithValue("stage_id", request.StageId);
            updateProgress.Parameters.AddWithValue("stars", request.Stars);
            updateProgress.Parameters.AddWithValue("turns", request.Turns);
            stage = await ReadSingleAsync(updateProgress, cancellationToken)
                ?? throw new InvalidOperationException("Campaign progress save did not return a record.");
        }

        // Grant stage EXP to the heroes in the player's saved PvE formation.
        // Hero level is derived from EXP (see LevelFromExp) and stored denormalized.
        var stageExp = firstClear ? firstClearExp : normalExp;
        if (stageExp > 0)
        {
            await using var expCommand = new NpgsqlCommand("""
                WITH formation_heroes AS (
                    SELECT DISTINCT cell.hero_id
                    FROM user_formations f,
                         LATERAL jsonb_each(f.grid_layout) AS rows(row_key, cells),
                         LATERAL jsonb_array_elements_text(rows.cells) AS cell(hero_id)
                    WHERE f.user_id = @user_id AND f.formation_type = 'PVE_DEFAULT'
                )
                UPDATE user_heroes uh
                SET exp = uh.exp + @exp,
                    level = LEAST(60, 1 + ((uh.exp + @exp) / 300)),
                    updated_at = NOW()
                FROM formation_heroes fh
                WHERE uh.user_id = @user_id AND uh.hero_id = fh.hero_id;
                """, connection, transaction);
            expCommand.Parameters.AddWithValue("user_id", userId);
            expCommand.Parameters.AddWithValue("exp", stageExp);
            await expCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new
        {
            status = "cleared",
            first_clear = firstClear,
            stage,
            reward_silver = rewardSilver,
            reward_gold = rewardGold,
            stamina_cost = staminaCost,
            exp_granted = firstClear ? firstClearExp : normalExp
        };
    }

    public async Task<object> StarUpHeroAsync(
        Guid userId,
        HeroStarUpRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HeroId) || request.HeroId.Length > 50)
        {
            throw new InvalidGameRequestException("Hero ID is invalid.");
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int star;
        await using (var heroCommand = new NpgsqlCommand(
            "SELECT star FROM user_heroes WHERE user_id = @user_id AND hero_id = @hero_id FOR UPDATE;",
            connection, transaction))
        {
            heroCommand.Parameters.AddWithValue("user_id", userId);
            heroCommand.Parameters.AddWithValue("hero_id", request.HeroId);
            var value = await heroCommand.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
            {
                throw new InvalidGameRequestException("Hero is not owned by this player.");
            }
            star = Convert.ToInt32(value);
        }

        if (star >= 6)
        {
            throw new InvalidGameRequestException("Hero is already at maximum star (6).");
        }

        var cost = 50 * star;
        await using (var walletCommand = new NpgsqlCommand(
            "SELECT gold FROM user_wallets WHERE user_id = @user_id FOR UPDATE;",
            connection, transaction))
        {
            walletCommand.Parameters.AddWithValue("user_id", userId);
            var gold = await walletCommand.ExecuteScalarAsync(cancellationToken);
            if (gold is null or DBNull || Convert.ToInt32(gold) < cost)
            {
                throw new InvalidGameRequestException("Không đủ Kim Cương để lên sao!");
            }
        }

        await using (var deductCommand = new NpgsqlCommand(
            "UPDATE user_wallets SET gold = gold - @cost, updated_at = NOW() WHERE user_id = @user_id;",
            connection, transaction))
        {
            deductCommand.Parameters.AddWithValue("cost", cost);
            deductCommand.Parameters.AddWithValue("user_id", userId);
            await deductCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var starCommand = new NpgsqlCommand(
            "UPDATE user_heroes SET star = star + 1, updated_at = NOW() WHERE user_id = @user_id AND hero_id = @hero_id;",
            connection, transaction))
        {
            starCommand.Parameters.AddWithValue("user_id", userId);
            starCommand.Parameters.AddWithValue("hero_id", request.HeroId);
            await starCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new { hero_id = request.HeroId, star = star + 1, gold_spent = cost };
    }

    public async Task<object> PullGachaAsync(
        Guid userId,
        GachaPullRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PullCount is not (1 or 10) || string.IsNullOrWhiteSpace(request.BannerId) || request.BannerId.Length > 50)
        {
            throw new InvalidGameRequestException("Gacha pull count must be 1 or 10 and banner ID must be valid.");
        }

        var cost = request.PullCount * 100;
        var results = new List<object>(request.PullCount);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int gold;
        await using (var walletCommand = new NpgsqlCommand(
            "SELECT gold FROM user_wallets WHERE user_id = @user_id FOR UPDATE;",
            connection, transaction))
        {
            walletCommand.Parameters.AddWithValue("user_id", userId);
            var value = await walletCommand.ExecuteScalarAsync(cancellationToken);
            gold = value is null ? -1 : Convert.ToInt32(value);
        }
        if (gold < cost)
        {
            throw new InvalidGameRequestException("Không đủ KNB để triệu hồi!");
        }

        await using (var deductGold = new NpgsqlCommand(
            "UPDATE user_wallets SET gold = gold - @cost WHERE user_id = @user_id;",
            connection, transaction))
        {
            deductGold.Parameters.AddWithValue("cost", cost);
            deductGold.Parameters.AddWithValue("user_id", userId);
            await deductGold.ExecuteNonQueryAsync(cancellationToken);
        }

        var pitySsr = 0;
        var pityUr = 0;
        var guaranteed = false;
        await using (var pityCommand = new NpgsqlCommand("""
            SELECT pull_count_since_last_ssr, pull_count_since_last_ur, is_next_rate_up_guaranteed
            FROM user_gacha_pity WHERE user_id = @user_id AND banner_id = @banner_id
            FOR UPDATE;
            """, connection, transaction))
        {
            pityCommand.Parameters.AddWithValue("user_id", userId);
            pityCommand.Parameters.AddWithValue("banner_id", request.BannerId);
            await using var reader = await pityCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                pitySsr = reader.GetInt32(0);
                pityUr = reader.GetInt32(1);
                guaranteed = reader.GetBoolean(2);
            }
        }

        var random = Random.Shared;
        for (var pull = 0; pull < request.PullCount; pull++)
        {
            pitySsr++;
            pityUr++;
            var roll = random.NextDouble();
            string rarity;
            string hero;

            if (pityUr >= 80 || roll < 0.02)
            {
                rarity = "UR";
                hero = guaranteed || random.NextDouble() < 0.5
                    ? "lu_bo"
                    : Choose(HeroPool[0], random);
                pityUr = 0;
                pitySsr = 0;
                guaranteed = false;
            }
            else if (pitySsr >= 40 || roll < 0.08)
            {
                rarity = "SSR";
                hero = Choose(HeroPool[1], random);
                pitySsr = 0;
            }
            else if (roll < 0.35)
            {
                rarity = "SR";
                hero = Choose(HeroPool[2], random);
            }
            else
            {
                rarity = "R";
                hero = Choose(HeroPool[3], random);
            }

            results.Add(new { hero_id = hero, rarity, pity_ur = pityUr, pity_ssr = pitySsr });
            await using (var addHero = new NpgsqlCommand("""
                INSERT INTO user_heroes (user_id, hero_id, star, level, exp)
                VALUES (@user_id, @hero_id, 1, 1, 0)
                ON CONFLICT (user_id, hero_id)
                DO UPDATE SET star = LEAST(6, user_heroes.star + 1)
                RETURNING id;
                """, connection, transaction))
            {
                addHero.Parameters.AddWithValue("user_id", userId);
                addHero.Parameters.AddWithValue("hero_id", hero);
                var ownedHeroId = (long)(await addHero.ExecuteScalarAsync(cancellationToken)
                    ?? throw new InvalidOperationException("Gacha grant did not return the owned hero ID."));

                await using var addHeroSkills = new NpgsqlCommand("""
                    INSERT INTO user_hero_skills (user_hero_id, hero_id, skill_id, level)
                    SELECT @user_hero_id, hero_id, skill_id, 1
                    FROM hero_skills
                    WHERE hero_id = @hero_id
                    ON CONFLICT (user_hero_id, skill_id) DO NOTHING;
                    """, connection, transaction);
                addHeroSkills.Parameters.AddWithValue("user_hero_id", ownedHeroId);
                addHeroSkills.Parameters.AddWithValue("hero_id", hero);
                await addHeroSkills.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var audit = new NpgsqlCommand("""
                INSERT INTO gacha_audit_logs
                    (user_id, banner_id, result_type, result_id, rarity, pull_pity_count, currency_spent, created_at)
                VALUES (@user_id, @banner_id, 'HERO', @hero_id, @rarity, @pity, 'GOLD', NOW());
                """, connection, transaction))
            {
                audit.Parameters.AddWithValue("user_id", userId);
                audit.Parameters.AddWithValue("banner_id", request.BannerId);
                audit.Parameters.AddWithValue("hero_id", hero);
                audit.Parameters.AddWithValue("rarity", rarity);
                audit.Parameters.AddWithValue("pity", pityUr);
                await audit.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await using (var savePity = new NpgsqlCommand("""
            INSERT INTO user_gacha_pity
                (user_id, banner_id, pull_count_since_last_ssr, pull_count_since_last_ur, is_next_rate_up_guaranteed)
            VALUES (@user_id, @banner_id, @pity_ssr, @pity_ur, @guaranteed)
            ON CONFLICT (user_id, banner_id) DO UPDATE
            SET pull_count_since_last_ssr = EXCLUDED.pull_count_since_last_ssr,
                pull_count_since_last_ur = EXCLUDED.pull_count_since_last_ur,
                is_next_rate_up_guaranteed = EXCLUDED.is_next_rate_up_guaranteed,
                updated_at = NOW();
            """, connection, transaction))
        {
            savePity.Parameters.AddWithValue("user_id", userId);
            savePity.Parameters.AddWithValue("banner_id", request.BannerId);
            savePity.Parameters.AddWithValue("pity_ssr", pitySsr);
            savePity.Parameters.AddWithValue("pity_ur", pityUr);
            savePity.Parameters.AddWithValue("guaranteed", guaranteed);
            await savePity.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new { results, pity_ur = pityUr, pity_ssr = pitySsr, gold_spent = cost };
    }

    public async Task<object> SimulateBattleAsync(
        Guid userId,
        BattleSimulationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BattleType) || request.BattleType.Length > 30)
        {
            throw new InvalidGameRequestException("Battle type is invalid.");
        }

        var seed = Random.Shared.Next(100_000, 1_000_000);
        var random = new Random(seed);
        var combatLog = new List<string>();
        var round = 1;
        var attackerHp = 12_000;
        var defenderHp = 9_500;
        string[] attackerHeroes = ["lu_bo", "quan_vu", "trieu_van", "gia_cat_luong", "chu_du"];
        string[] defenderHeroes = ["dien_vi", "tao_nhan"];

        while (round <= 5 && attackerHp > 0 && defenderHp > 0)
        {
            combatLog.Add($"=== Hiệp {round} ===");
            var actor = attackerHeroes[random.Next(attackerHeroes.Length)];
            var damage = random.Next(1200, 2401);
            var critical = random.NextDouble() < 0.25;
            if (critical)
            {
                damage = (int)(damage * 1.8);
            }
            defenderHp = Math.Max(0, defenderHp - damage);
            combatLog.Add($"⚔️ {actor} tấn công đối phương, gây {damage} sát thương!{(critical ? " (BẠO KÍCH! 🔥)" : string.Empty)}");
            if (defenderHp <= 0)
            {
                break;
            }

            var defenderActor = defenderHeroes[random.Next(defenderHeroes.Length)];
            var defenderDamage = random.Next(800, 1501);
            attackerHp = Math.Max(0, attackerHp - defenderDamage);
            combatLog.Add($"🛡️ {defenderActor} phản công, gây {defenderDamage} sát thương lên đội Chúa Công!");
            round++;
        }

        var attackerWon = defenderHp <= 0;
        combatLog.Add(attackerWon ? "=== Kết Quả: CHIẾN THẮNG HUY HOÀNG! 🏆 ===" : "=== Kết Quả: THẤT BẠI TRONG GANG TẤC! ===");
        var logJson = JsonSerializer.Serialize(combatLog);
        var replay = await QuerySingleAsync("""
            INSERT INTO battle_replays
                (battle_type, attacker_id, defender_id, is_attacker_win, seed, input_actions, created_at)
            VALUES (@battle_type, @attacker_id, @defender_id, @attacker_won, @seed, @input_actions::jsonb, NOW())
            RETURNING id, created_at;
            """, cancellationToken,
            ("battle_type", request.BattleType),
            ("attacker_id", userId),
            ("defender_id", ParseOptionalGuid(request.DefenderId)),
            ("attacker_won", attackerWon),
            ("seed", seed),
            ("input_actions", logJson));

        return new
        {
            replay_id = (replay as Dictionary<string, object?>)?["id"],
            seed,
            attacker_win = attackerWon,
            rounds = round,
            log = combatLog
        };
    }

    public Task<IReadOnlyList<object>> GetMailAsync(Guid userId, CancellationToken cancellationToken) =>
        QueryListAsync("""
            SELECT m.id, m.title, m.content AS body, m.attached_rewards AS attachments,
                   COALESCE(r.is_read, false) AS is_read,
                   COALESCE(r.is_claimed, false) AS is_claimed,
                   m.expires_at AS expire_at, m.created_at
            FROM system_mails m
            LEFT JOIN mail_recipients r ON r.mail_id = m.id AND r.user_id = @user_id
            WHERE m.expires_at > NOW()
              AND (m.target_scope = 'ALL' OR r.user_id = @user_id)
            ORDER BY m.created_at DESC;
            """, cancellationToken, ("user_id", userId));

    public async Task<IReadOnlyDictionary<string, long>> GetDatabaseOverviewAsync(CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        foreach (var table in OverviewTables)
        {
            await using var command = new NpgsqlCommand(
                "SELECT to_regclass(@table_name)::text;", connection);
            command.Parameters.AddWithValue("table_name", $"public.{table}");
            var relation = await command.ExecuteScalarAsync(cancellationToken);
            if (relation is null or DBNull)
            {
                counts[table] = -1;
                continue;
            }

            await using var countCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM public.\"{table}\";", connection);
            counts[table] = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
        }
        return counts;
    }

    private async Task<bool> RedisIsReachableAsync(CancellationToken cancellationToken)
    {
        var redisUrl = configuration["REDIS_URL"];
        if (string.IsNullOrWhiteSpace(redisUrl) || !Uri.TryCreate(redisUrl, UriKind.Absolute, out var uri))
        {
            logger.LogWarning("Redis health probe is not configured.");
            return false;
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(uri.Host, uri.Port > 0 ? uri.Port : 6379, cancellationToken);
            await using var stream = client.GetStream();
            var ping = Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n");
            await stream.WriteAsync(ping, cancellationToken);
            var response = new byte[64];
            var read = await stream.ReadAsync(response, cancellationToken);
            return Encoding.ASCII.GetString(response, 0, read).StartsWith("+PONG", StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is SocketException or IOException or OperationCanceledException)
        {
            logger.LogWarning(exception, "Redis health probe failed.");
            return false;
        }
    }

    private async Task<object?> QuerySingleAsync(
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = dataSource.CreateCommand(sql);
        AddParameters(command, parameters);
        return await ReadSingleAsync(command, cancellationToken);
    }

    private async Task<object?> QuerySingleJsonAsync(
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = dataSource.CreateCommand(sql);
        foreach (var (name, value) in parameters)
        {
            if (name == "grid_layout" && value is string json)
            {
                command.Parameters.AddWithValue(name, NpgsqlDbType.Jsonb, json);
            }
            else
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
        }
        return await ReadSingleAsync(command, cancellationToken);
    }

    private async Task<IReadOnlyList<object>> QueryListAsync(
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = dataSource.CreateCommand(sql);
        AddParameters(command, parameters);
        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(await ReadRowAsync(reader, cancellationToken));
        }
        return rows;
    }

    private static async Task<object?> ReadSingleAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? await ReadRowAsync(reader, cancellationToken)
            : null;
    }

    private static async Task<Dictionary<string, object?>> ReadRowAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.Ordinal);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (await reader.IsDBNullAsync(i, cancellationToken))
            {
                row[reader.GetName(i)] = null;
                continue;
            }

            if (reader.GetDataTypeName(i) == "jsonb")
            {
                using var document = JsonDocument.Parse(reader.GetString(i));
                row[reader.GetName(i)] = document.RootElement.Clone();
            }
            else
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
        }
        return row;
    }

    private static void AddParameters(NpgsqlCommand command, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private static bool HasValidGrid(Dictionary<string, List<string?>>? grid) =>
        grid is not null &&
        new[] { "front", "mid", "back" }.All(row => grid.TryGetValue(row, out var cells) && cells.Count == 3);

    private static Guid? ParseOptionalGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;

    private static string Choose(string[] options, Random random) => options[random.Next(options.Length)];
}
