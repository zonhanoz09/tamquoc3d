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
    private readonly RedisCache _cache = new(configuration);

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
        var cacheKey = $"profile:{userId}";
        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var cachedProfile = JsonSerializer.Deserialize<Dictionary<string, object?>>(cached);
            if (cachedProfile is not null) return cachedProfile;
        }

        var profile = await QuerySingleAsync("""
            SELECT u.id, u.status, p.display_name, p.avatar_id, p.frame_id,
                   p.level, p.exp, p.vip_level, p.combat_power, p.last_login_at
            FROM users u
            JOIN user_profiles p ON p.user_id = u.id
            WHERE u.id = @user_id;
            """, cancellationToken, ("user_id", userId));
        if (profile is null) throw new KeyNotFoundException("Player not found.");
        await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(profile), 60, cancellationToken);
        return profile;
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

    public async Task<IReadOnlyList<object>> GetCampaignProgressAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"campaign:{userId}";
        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var cachedRows = JsonSerializer.Deserialize<List<object>>(cached);
            if (cachedRows is not null) return cachedRows;
        }

        var rows = await QueryListAsync("""
            SELECT stage_id, stars, first_clear_claimed, clear_count, best_record_turns, updated_at
            FROM campaign_progress WHERE user_id = @user_id ORDER BY stage_id ASC;
            """, cancellationToken, ("user_id", userId));
        await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(rows), 60, cancellationToken);
        return rows;
    }

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
        await _cache.DeleteAsync($"campaign:{userId}", cancellationToken);
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

        var isArena = request.BattleType == "ASYNC_ARENA";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var attackerTeam = await LoadTeamAsync(connection, userId, cancellationToken);
        if (attackerTeam.Count == 0)
        {
            throw new InvalidGameRequestException("Player has no heroes to battle with.");
        }

        var seed = Random.Shared.Next(100_000, 1_000_000);
        var random = new Random(seed);

        var defenderId = ParseOptionalGuid(request.DefenderId);
        var realDefender = defenderId.HasValue && defenderId.Value != userId;
        List<TeamHero> defenderTeam;
        string defenderName;
        if (realDefender)
        {
            defenderTeam = await LoadTeamAsync(connection, defenderId!.Value, cancellationToken);
            if (defenderTeam.Count == 0)
            {
                realDefender = false;
                defenderTeam = await LoadNpcTeamAsync(connection, random, attackerTeam.Sum(h => h.Power), cancellationToken);
                defenderName = "Đội Thủ Vệ Hoàng Gia (NPC)";
                defenderId = null;
            }
            else
            {
                defenderName = await GetDisplayNameAsync(connection, defenderId.Value, cancellationToken) ?? "Đối thủ";
            }
        }
        else
        {
            defenderTeam = await LoadNpcTeamAsync(connection, random, attackerTeam.Sum(h => h.Power), cancellationToken);
            defenderName = "Đội Thủ Vệ Hoàng Gia (NPC)";
            defenderId = null;
        }

        var (attackerWon, rounds, combatLog) = SimulateTeams(attackerTeam, defenderTeam, random);

        var rewardSilver = 0;
        var rewardTokens = 0;
        int? mmrAfter = null;
        if (isArena)
        {
            rewardSilver = attackerWon ? 300 : 50;
            rewardTokens = attackerWon ? 3 : 1;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await EnsureArenaProfileAsync(connection, transaction, userId, cancellationToken);
            if (realDefender && defenderId.HasValue)
            {
                await EnsureArenaProfileAsync(connection, transaction, defenderId.Value, cancellationToken);
            }
            await ExecAsync(connection, transaction,
                "UPDATE user_wallets SET silver = silver + @s, arena_tokens = arena_tokens + @tk, updated_at = NOW() WHERE user_id = @u;",
                cancellationToken, ("s", rewardSilver), ("tk", rewardTokens), ("u", userId));
            await ApplyArenaResultAsync(connection, transaction, userId, attackerWon, cancellationToken);
            if (realDefender && defenderId.HasValue)
            {
                await ApplyArenaResultAsync(connection, transaction, defenderId.Value, !attackerWon, cancellationToken);
            }
            await using var mmrCommand = new NpgsqlCommand(
                "SELECT mmr FROM arena_profiles WHERE user_id = @u;", connection, transaction);
            mmrCommand.Parameters.AddWithValue("u", userId);
            mmrAfter = Convert.ToInt32(await mmrCommand.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
        }

        var logJson = JsonSerializer.Serialize(combatLog);
        var replay = await QuerySingleAsync("""
            INSERT INTO battle_replays
                (battle_type, attacker_id, defender_id, is_attacker_win, seed, input_actions, created_at)
            VALUES (@battle_type, @attacker_id, @defender_id, @attacker_won, @seed, @input_actions::jsonb, NOW())
            RETURNING id, created_at;
            """, cancellationToken,
            ("battle_type", request.BattleType),
            ("attacker_id", userId),
            ("defender_id", (object?)defenderId ?? DBNull.Value),
            ("attacker_won", attackerWon),
            ("seed", seed),
            ("input_actions", logJson));

        return new
        {
            replay_id = (replay as Dictionary<string, object?>)?["id"],
            seed,
            attacker_win = attackerWon,
            rounds,
            log = combatLog,
            defender_name = defenderName,
            attacker_power = attackerTeam.Sum(h => h.Power),
            defender_power = defenderTeam.Sum(h => h.Power),
            rewards = new { silver = rewardSilver, arena_tokens = rewardTokens },
            mmr_after = mmrAfter
        };
    }

    // ==================== Phase 4: Shop / Inventory / Arena / Tower ====================

    private sealed class TeamHero
    {
        public required string HeroId { get; init; }
        public required string Name { get; init; }
        public int HpMax { get; set; }
        public int Atk { get; init; }
        public int Speed { get; init; }
        public double CritRate { get; init; }
        public long Power { get; init; }
        public int Hp { get; set; }
    }

    private static double ProgressionScale(int level, int star) =>
        (1 + 0.05 * (level - 1)) * (1 + 0.10 * (star - 1));

    private static long ComputePower(int hp, int atkPhys, int atkMag, int defPhys, int defMag, int speed, int level, int star) =>
        (long)Math.Round((hp / 10.0 + (atkPhys + atkMag) * 2.0 + defPhys + defMag + speed) * ProgressionScale(level, star));

    private static async Task ExecAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = transaction is null
            ? new NpgsqlCommand(sql, connection)
            : new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Load a player's battle team: heroes from the saved PVE formation first,
    /// remaining slots filled by highest combat power (max 5).
    /// </summary>
    private async Task<List<TeamHero>> LoadTeamAsync(
        NpgsqlConnection connection,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var formationIds = new List<string>();
        await using (var formationCommand = new NpgsqlCommand(
            "SELECT grid_layout::text FROM user_formations WHERE user_id = @u AND formation_type = 'PVE_DEFAULT';",
            connection))
        {
            formationCommand.Parameters.AddWithValue("u", userId);
            if (await formationCommand.ExecuteScalarAsync(cancellationToken) is string layoutJson)
            {
                using var doc = JsonDocument.Parse(layoutJson);
                foreach (var row in doc.RootElement.EnumerateObject())
                {
                    foreach (var cell in row.Value.EnumerateArray())
                    {
                        if (cell.ValueKind == JsonValueKind.String && cell.GetString() is { Length: > 0 } heroId)
                        {
                            formationIds.Add(heroId);
                        }
                    }
                }
            }
        }

        var heroes = new List<TeamHero>();
        await using var command = new NpgsqlCommand("""
            SELECT uh.hero_id, d.hero_name, uh.level, uh.star,
                   (d.base_stats->>'hp_max')::int, (d.base_stats->>'atk_phys')::int, (d.base_stats->>'atk_mag')::int,
                   (d.base_stats->>'def_phys')::int, (d.base_stats->>'def_mag')::int, (d.base_stats->>'speed')::int,
                   COALESCE((d.base_stats->>'crit_rate')::float, 0.05)
            FROM user_heroes uh
            JOIN hero_definitions d ON d.hero_id = uh.hero_id
            WHERE uh.user_id = @u;
            """, connection);
        command.Parameters.AddWithValue("u", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var level = reader.GetInt32(2);
            var star = reader.GetInt32(3);
            var hp = reader.GetInt32(4);
            var atkPhys = reader.GetInt32(5);
            var atkMag = reader.GetInt32(6);
            var defPhys = reader.GetInt32(7);
            var defMag = reader.GetInt32(8);
            var speed = reader.GetInt32(9);
            var scale = ProgressionScale(level, star);
            var hero = new TeamHero
            {
                HeroId = reader.GetString(0),
                Name = reader.GetString(1),
                HpMax = (int)Math.Round(hp * scale),
                Atk = (int)Math.Round((atkPhys + atkMag) * scale),
                Speed = speed,
                CritRate = reader.GetDouble(10),
                Power = ComputePower(hp, atkPhys, atkMag, defPhys, defMag, speed, level, star)
            };
            hero.Hp = hero.HpMax;
            heroes.Add(hero);
        }
        await reader.CloseAsync();

        var byId = heroes.ToDictionary(h => h.HeroId);
        var team = new List<TeamHero>();
        foreach (var heroId in formationIds)
        {
            if (byId.TryGetValue(heroId, out var hero) && !team.Contains(hero))
            {
                team.Add(hero);
            }
        }
        foreach (var hero in heroes.OrderByDescending(h => h.Power))
        {
            if (team.Count >= 5) break;
            if (!team.Contains(hero)) team.Add(hero);
        }
        return team.Take(5).ToList();
    }

    private async Task<List<TeamHero>> LoadNpcTeamAsync(
        NpgsqlConnection connection,
        Random random,
        long attackerTeamPower,
        CancellationToken cancellationToken)
    {
        var team = new List<TeamHero>();
        await using var command = new NpgsqlCommand(
            "SELECT hero_id, hero_name, base_stats::text FROM hero_definitions WHERE hero_id <> 'default_hero' ORDER BY random() LIMIT 5;",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(string Id, string Name, string Stats)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }
        await reader.CloseAsync();

        var perHeroTarget = Math.Max(300, attackerTeamPower / Math.Max(1, rows.Count));
        foreach (var (heroId, name, statsJson) in rows)
        {
            using var doc = JsonDocument.Parse(statsJson);
            var stats = doc.RootElement;
            int GetInt(string key) => stats.TryGetProperty(key, out var v) ? v.GetInt32() : 0;
            double GetDouble(string key) => stats.TryGetProperty(key, out var v) ? v.GetDouble() : 0;
            var level = 1 + random.Next(0, 5);
            var star = 1 + random.Next(0, 3);
            var power = ComputePower(GetInt("hp_max"), GetInt("atk_phys"), GetInt("atk_mag"),
                GetInt("def_phys"), GetInt("def_mag"), GetInt("speed"), level, star);
            var adjust = Math.Clamp(perHeroTarget / (double)Math.Max(1, power), 0.5, 2.0);
            var scale = ProgressionScale(level, star) * adjust;
            var hero = new TeamHero
            {
                HeroId = heroId,
                Name = name,
                HpMax = (int)Math.Round(GetInt("hp_max") * scale),
                Atk = (int)Math.Round((GetInt("atk_phys") + GetInt("atk_mag")) * scale),
                Speed = GetInt("speed"),
                CritRate = GetDouble("crit_rate"),
                Power = (long)Math.Round(power * adjust)
            };
            hero.Hp = hero.HpMax;
            team.Add(hero);
        }
        return team;
    }

    private static (bool AttackerWon, int Rounds, List<string> Log) SimulateTeams(
        List<TeamHero> attackers,
        List<TeamHero> defenders,
        Random random)
    {
        var log = new List<string>();
        var order = attackers.Select(h => (Hero: h, Side: 0))
            .Concat(defenders.Select(h => (Hero: h, Side: 1)))
            .OrderByDescending(x => x.Hero.Speed)
            .ToList();

        var round = 0;
        while (round < 20 && attackers.Any(h => h.Hp > 0) && defenders.Any(h => h.Hp > 0))
        {
            round++;
            log.Add($"=== Hiệp {round} ===");
            foreach (var (hero, side) in order)
            {
                if (hero.Hp <= 0) continue;
                var enemies = side == 0 ? defenders : attackers;
                var alive = enemies.Where(e => e.Hp > 0).ToList();
                if (alive.Count == 0) break;
                var target = alive[random.Next(alive.Count)];
                var damage = Math.Max(1, (int)(hero.Atk * (0.85 + random.NextDouble() * 0.4)));
                var critical = random.NextDouble() < hero.CritRate;
                if (critical) damage = (int)(damage * 1.5);
                target.Hp = Math.Max(0, target.Hp - damage);
                log.Add($"⚔️ {hero.Name} đánh {target.Name}: -{damage} HP{(critical ? " (BẠO KÍCH! 🔥)" : string.Empty)}{(target.Hp <= 0 ? $" — {target.Name} gục ngã! 💀" : string.Empty)}");
            }
        }

        var defenderWiped = defenders.All(h => h.Hp <= 0);
        var attackerWiped = attackers.All(h => h.Hp <= 0);
        bool attackerWon;
        if (defenderWiped && !attackerWiped)
        {
            attackerWon = true;
        }
        else if (attackerWiped)
        {
            attackerWon = false;
        }
        else
        {
            var atkFraction = (double)attackers.Sum(h => h.Hp) / Math.Max(1, attackers.Sum(h => h.HpMax));
            var defFraction = (double)defenders.Sum(h => h.Hp) / Math.Max(1, defenders.Sum(h => h.HpMax));
            attackerWon = atkFraction >= defFraction;
        }
        log.Add(attackerWon ? "=== Kết Quả: CHIẾN THẮNG! 🏆 ===" : "=== Kết Quả: THẤT BẠI! ===");
        return (attackerWon, Math.Max(1, round), log);
    }

    private static async Task<string?> GetDisplayNameAsync(
        NpgsqlConnection connection,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT display_name FROM user_profiles WHERE user_id = @u;", connection);
        command.Parameters.AddWithValue("u", userId);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private static Task EnsureArenaProfileAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid userId,
        CancellationToken cancellationToken) =>
        ExecAsync(connection, transaction,
            "INSERT INTO arena_profiles (user_id) VALUES (@u) ON CONFLICT (user_id) DO NOTHING;",
            cancellationToken, ("u", userId));

    private static Task ApplyArenaResultAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        bool won,
        CancellationToken cancellationToken)
    {
        var delta = won ? 25 : -15;
        return ExecAsync(connection, transaction, """
            UPDATE arena_profiles SET
                mmr = GREATEST(0, mmr + @delta),
                win_count = win_count + @win,
                loss_count = loss_count + @loss,
                current_winning_streak = CASE WHEN @win = 1 THEN current_winning_streak + 1 ELSE 0 END,
                rank_tier = CASE
                    WHEN GREATEST(0, mmr + @delta) >= 2000 THEN 'DIAMOND'
                    WHEN GREATEST(0, mmr + @delta) >= 1600 THEN 'PLATINUM'
                    WHEN GREATEST(0, mmr + @delta) >= 1300 THEN 'GOLD'
                    WHEN GREATEST(0, mmr + @delta) >= 1100 THEN 'SILVER'
                    ELSE 'BRONZE' END,
                updated_at = NOW()
            WHERE user_id = @u;
            """, cancellationToken,
            ("delta", delta), ("win", won ? 1 : 0), ("loss", won ? 0 : 1), ("u", userId));
    }

    // ---------- Shop ----------

    public Task<IReadOnlyList<object>> GetShopItemsAsync(CancellationToken cancellationToken) =>
        QueryListAsync("""
            SELECT item_id, name, description, currency, price, grant_type, grant_amount, grant_item_id
            FROM shop_items WHERE is_active = TRUE ORDER BY price ASC;
            """, cancellationToken);

    public async Task<object> BuyShopItemAsync(
        Guid userId,
        ShopBuyRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        string currency;
        string grantType;
        int price;
        int grantAmount;
        string? grantItemId;
        await using (var itemCommand = new NpgsqlCommand(
            "SELECT currency, price, grant_type, grant_amount, grant_item_id FROM shop_items WHERE item_id = @item AND is_active = TRUE;",
            connection, transaction))
        {
            itemCommand.Parameters.AddWithValue("item", request.ItemId);
            await using var itemReader = await itemCommand.ExecuteReaderAsync(cancellationToken);
            if (!await itemReader.ReadAsync(cancellationToken))
            {
                throw new InvalidGameRequestException("Shop item does not exist.");
            }
            currency = itemReader.GetString(0);
            price = itemReader.GetInt32(1);
            grantType = itemReader.GetString(2);
            grantAmount = itemReader.GetInt32(3);
            grantItemId = itemReader.IsDBNull(4) ? null : itemReader.GetString(4);
        }

        var totalCost = price * request.Quantity;
        long balance;
        await using (var walletCommand = new NpgsqlCommand(
            $"SELECT {currency} FROM user_wallets WHERE user_id = @u FOR UPDATE;", connection, transaction))
        {
            walletCommand.Parameters.AddWithValue("u", userId);
            var value = await walletCommand.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
            {
                throw new InvalidGameRequestException("Wallet not found.");
            }
            balance = Convert.ToInt64(value);
        }
        if (balance < totalCost)
        {
            throw new InvalidGameRequestException(currency == "gold"
                ? "Không đủ Kim Cương!"
                : "Không đủ Bạc!");
        }

        await ExecAsync(connection, transaction,
            $"UPDATE user_wallets SET {currency} = {currency} - @cost, updated_at = NOW() WHERE user_id = @u;",
            cancellationToken, ("cost", totalCost), ("u", userId));

        switch (grantType)
        {
            case "STAMINA":
                await ExecAsync(connection, transaction,
                    "UPDATE user_wallets SET stamina = LEAST(max_stamina, stamina + @amount), updated_at = NOW() WHERE user_id = @u;",
                    cancellationToken, ("amount", grantAmount * request.Quantity), ("u", userId));
                break;
            case "SILVER":
                await ExecAsync(connection, transaction,
                    "UPDATE user_wallets SET silver = silver + @amount, updated_at = NOW() WHERE user_id = @u;",
                    cancellationToken, ("amount", grantAmount * request.Quantity), ("u", userId));
                break;
            case "ITEM":
                if (string.IsNullOrWhiteSpace(grantItemId))
                {
                    throw new InvalidGameRequestException("Shop item grant is misconfigured.");
                }
                await ExecAsync(connection, transaction, """
                    INSERT INTO user_inventories (user_id, item_id, quantity)
                    VALUES (@u, @item, @qty)
                    ON CONFLICT (user_id, item_id)
                    DO UPDATE SET quantity = user_inventories.quantity + EXCLUDED.quantity, updated_at = NOW();
                    """, cancellationToken,
                    ("u", userId), ("item", grantItemId), ("qty", grantAmount * request.Quantity));
                break;
            default:
                throw new InvalidGameRequestException("Unsupported shop grant type.");
        }

        await transaction.CommitAsync(cancellationToken);
        return new
        {
            item_id = request.ItemId,
            quantity = request.Quantity,
            total_cost = totalCost,
            currency,
            grant_type = grantType,
            grant_amount = grantAmount * request.Quantity,
            grant_item_id = grantItemId
        };
    }

    // ---------- Inventory ----------

    public Task<IReadOnlyList<object>> GetInventoryAsync(Guid userId, CancellationToken cancellationToken) =>
        QueryListAsync("""
            SELECT item_id, quantity, updated_at
            FROM user_inventories WHERE user_id = @user_id AND quantity > 0 ORDER BY item_id ASC;
            """, cancellationToken, ("user_id", userId));

    public async Task<object> UseItemAsync(
        Guid userId,
        UseItemRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ItemId != "sach_exp")
        {
            throw new InvalidGameRequestException("Item cannot be used this way.");
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var itemCommand = new NpgsqlCommand(
            "SELECT quantity FROM user_inventories WHERE user_id = @u AND item_id = @item FOR UPDATE;",
            connection, transaction))
        {
            itemCommand.Parameters.AddWithValue("u", userId);
            itemCommand.Parameters.AddWithValue("item", request.ItemId);
            var quantity = await itemCommand.ExecuteScalarAsync(cancellationToken);
            if (quantity is null or DBNull || Convert.ToInt64(quantity) <= 0)
            {
                throw new InvalidGameRequestException("Item not in inventory.");
            }
        }

        long newExp;
        await using (var heroCommand = new NpgsqlCommand(
            "SELECT exp FROM user_heroes WHERE user_id = @u AND hero_id = @hero FOR UPDATE;",
            connection, transaction))
        {
            heroCommand.Parameters.AddWithValue("u", userId);
            heroCommand.Parameters.AddWithValue("hero", request.HeroId);
            var exp = await heroCommand.ExecuteScalarAsync(cancellationToken);
            if (exp is null or DBNull)
            {
                throw new InvalidGameRequestException("Hero is not owned by this player.");
            }
            newExp = Convert.ToInt64(exp) + 300;
        }

        await ExecAsync(connection, transaction,
            "UPDATE user_inventories SET quantity = quantity - 1, updated_at = NOW() WHERE user_id = @u AND item_id = @item;",
            cancellationToken, ("u", userId), ("item", request.ItemId));
        await ExecAsync(connection, transaction,
            "UPDATE user_heroes SET exp = @exp, level = @level, updated_at = NOW() WHERE user_id = @u AND hero_id = @hero;",
            cancellationToken, ("exp", newExp), ("level", LevelFromExp(newExp)), ("u", userId), ("hero", request.HeroId));

        await transaction.CommitAsync(cancellationToken);
        return new { hero_id = request.HeroId, exp = newExp, level = LevelFromExp(newExp), exp_granted = 300 };
    }

    // ---------- Arena ----------

    public async Task<object> GetArenaProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnsureArenaProfileAsync(connection, null, userId, cancellationToken);
        var team = await LoadTeamAsync(connection, userId, cancellationToken);
        var profile = await QuerySingleAsync("""
            SELECT a.user_id::text AS user_id, p.display_name, a.rank_tier, a.mmr,
                   a.win_count, a.loss_count, a.current_winning_streak
            FROM arena_profiles a
            JOIN user_profiles p ON p.user_id = a.user_id
            WHERE a.user_id = @user_id;
            """, cancellationToken, ("user_id", userId)) as Dictionary<string, object?>;
        return new
        {
            user_id = profile?["user_id"],
            display_name = profile?["display_name"],
            rank_tier = profile?["rank_tier"],
            mmr = profile?["mmr"],
            win_count = profile?["win_count"],
            loss_count = profile?["loss_count"],
            current_winning_streak = profile?["current_winning_streak"],
            combat_power = team.Sum(h => h.Power)
        };
    }

    public async Task<IReadOnlyList<object>> GetArenaOpponentsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnsureArenaProfileAsync(connection, null, userId, cancellationToken);
        int myMmr;
        await using (var mmrCommand = new NpgsqlCommand(
            "SELECT mmr FROM arena_profiles WHERE user_id = @u;", connection))
        {
            mmrCommand.Parameters.AddWithValue("u", userId);
            myMmr = Convert.ToInt32(await mmrCommand.ExecuteScalarAsync(cancellationToken));
        }

        var rows = await QueryListAsync("""
            SELECT user_id::text AS user_id, mmr, win_count, loss_count
            FROM arena_profiles WHERE user_id <> @user_id
            ORDER BY ABS(mmr - @my_mmr) ASC LIMIT 10;
            """, cancellationToken, ("user_id", userId), ("my_mmr", myMmr));

        var opponents = new List<object>();
        foreach (var row in rows.Cast<Dictionary<string, object?>>())
        {
            var opponentId = Guid.Parse((string)row["user_id"]!);
            var team = await LoadTeamAsync(connection, opponentId, cancellationToken);
            opponents.Add(new
            {
                user_id = row["user_id"],
                display_name = await GetDisplayNameAsync(connection, opponentId, cancellationToken) ?? "Đối thủ",
                mmr = row["mmr"],
                win_count = row["win_count"],
                loss_count = row["loss_count"],
                combat_power = team.Sum(h => h.Power),
                hero_ids = team.Select(h => h.HeroId).ToList()
            });
        }
        return opponents;
    }

    // ---------- Tower ----------

    public async Task<object> GetTowerProgressAsync(Guid userId, CancellationToken cancellationToken)
    {
        await QuerySingleAsync("""
            INSERT INTO tower_progress (user_id) VALUES (@user_id)
            ON CONFLICT (user_id) DO NOTHING
            RETURNING user_id;
            """, cancellationToken, ("user_id", userId));
        var row = await QuerySingleAsync(
            "SELECT highest_floor FROM tower_progress WHERE user_id = @user_id;",
            cancellationToken, ("user_id", userId)) as Dictionary<string, object?>;
        return new { highest_floor = row?["highest_floor"] ?? 0 };
    }

    public async Task<object> ClearTowerFloorAsync(
        Guid userId,
        TowerClearRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecAsync(connection, transaction,
            "INSERT INTO tower_progress (user_id) VALUES (@u) ON CONFLICT (user_id) DO NOTHING;",
            cancellationToken, ("u", userId));

        int highest;
        await using (var progressCommand = new NpgsqlCommand(
            "SELECT highest_floor FROM tower_progress WHERE user_id = @u FOR UPDATE;",
            connection, transaction))
        {
            progressCommand.Parameters.AddWithValue("u", userId);
            highest = Convert.ToInt32(await progressCommand.ExecuteScalarAsync(cancellationToken));
        }

        if (request.Floor <= highest)
        {
            await transaction.CommitAsync(cancellationToken);
            return new
            {
                status = "already_cleared",
                floor = request.Floor,
                highest_floor = highest,
                reward_silver = 0,
                reward_gold = 0
            };
        }
        if (request.Floor != highest + 1)
        {
            throw new InvalidGameRequestException(
                $"Must clear floor {highest + 1} before floor {request.Floor}.");
        }

        var rewardSilver = 100 * request.Floor;
        var rewardGold = request.Floor % 5 == 0 ? 20 : 0;
        await ExecAsync(connection, transaction,
            "UPDATE user_wallets SET silver = silver + @s, gold = gold + @g, updated_at = NOW() WHERE user_id = @u;",
            cancellationToken, ("s", rewardSilver), ("g", rewardGold), ("u", userId));
        await ExecAsync(connection, transaction,
            "UPDATE tower_progress SET highest_floor = @floor, updated_at = NOW() WHERE user_id = @u;",
            cancellationToken, ("floor", request.Floor), ("u", userId));

        await transaction.CommitAsync(cancellationToken);
        return new
        {
            status = "cleared",
            floor = request.Floor,
            highest_floor = request.Floor,
            reward_silver = rewardSilver,
            reward_gold = rewardGold
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
