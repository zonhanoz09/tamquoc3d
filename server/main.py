import os
import time
import json
import random
import psycopg2
from psycopg2.extras import RealDictCursor
import redis
from fastapi import FastAPI, HTTPException
from fastapi.responses import HTMLResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, List, Dict, Any

DATABASE_URL = os.getenv("DATABASE_URL", "postgresql://tamquoc:tamquoc_secret@postgres:5432/tamquoc_db")
REDIS_URL = os.getenv("REDIS_URL", "redis://redis:6379/0")
DEFAULT_USER_ID = "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"

app = FastAPI(
    title="Project Pixel Tactics — Tam Quốc 3D Server API",
    description="Authoritative Game Backend with 7 Modules: Auth, Wallets, Heroes, Formations, Progression, Gacha, Arena & LiveOps",
    version="1.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

def get_db():
    return psycopg2.connect(DATABASE_URL, cursor_factory=RealDictCursor)

def get_redis():
    return redis.Redis.from_url(REDIS_URL, decode_responses=True)

# ---------------------------------------------------------------------------
# API MODELS
# ---------------------------------------------------------------------------
class FormationUpdateReq(BaseModel):
    formation_type: str = "PVE_DEFAULT"
    grid_layout: Dict[str, List[Optional[str]]]

class GachaPullReq(BaseModel):
    banner_id: str = "banner_lu_bo_limited"
    pull_count: int = 1 # 1 or 10

class BattleSimReq(BaseModel):
    battle_type: str = "ASYNC_ARENA"
    defender_id: Optional[str] = "b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22"

# ---------------------------------------------------------------------------
# HEALTHCHECK
# ---------------------------------------------------------------------------
@app.get("/health")
def healthcheck():
    db_ok = False
    redis_ok = False
    try:
        with get_db() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT 1 as healthy;")
                res = cur.fetchone()
                if res and res["healthy"] == 1:
                    db_ok = True
    except Exception as e:
        db_err = str(e)

    try:
        r = get_redis()
        if r.ping():
            redis_ok = True
    except Exception as e:
        redis_err = str(e)

    status = "healthy" if (db_ok and redis_ok) else "degraded"
    return {
        "status": status,
        "database_connected": db_ok,
        "redis_connected": redis_ok,
        "game_version": "1.0.0",
        "timestamp": time.time()
    }

# ---------------------------------------------------------------------------
# MODULE 1: AUTH & WALLETS
# ---------------------------------------------------------------------------
@app.get("/api/profile")
def get_profile(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT u.id, u.status, p.display_name, p.avatar_id, p.frame_id, 
                       p.level, p.exp, p.vip_level, p.combat_power, p.last_login_at
                FROM users u
                JOIN user_profiles p ON u.id = p.user_id
                WHERE u.id = %s;
            """, (user_id,))
            profile = cur.fetchone()
            if not profile:
                raise HTTPException(status_code=404, detail="Player not found")
            return profile

@app.get("/api/wallet")
def get_wallet(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT silver, gold, paid_gold, stamina, max_stamina, arena_tokens,
                       last_stamina_recovery_at, updated_at
                FROM user_wallets
                WHERE user_id = %s;
            """, (user_id,))
            wallet = cur.fetchone()
            if not wallet:
                raise HTTPException(status_code=404, detail="Wallet not found")
            return wallet

@app.post("/api/wallet/claim-stamina")
def claim_stamina(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                UPDATE user_wallets 
                SET stamina = LEAST(max_stamina, stamina + 20),
                    last_stamina_recovery_at = NOW()
                WHERE user_id = %s
                RETURNING stamina, max_stamina;
            """, (user_id,))
            conn.commit()
            return cur.fetchone()

# ---------------------------------------------------------------------------
# MODULE 2: HEROES & FORMATIONS
# ---------------------------------------------------------------------------
@app.get("/api/heroes")
def get_heroes(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT id, hero_id, star, level, exp, skill_levels, unlocked_talents, created_at
                FROM user_heroes
                WHERE user_id = %s
                ORDER BY level DESC, star DESC;
            """, (user_id,))
            return cur.fetchall()

@app.get("/api/formations")
def get_formations(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT formation_type, grid_layout, is_active, updated_at
                FROM user_formations
                WHERE user_id = %s;
            """, (user_id,))
            return cur.fetchall()

@app.post("/api/formations")
def update_formation(req: FormationUpdateReq, user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO user_formations (user_id, formation_type, grid_layout, is_active)
                VALUES (%s, %s, %s, true)
                ON CONFLICT (user_id, formation_type)
                DO UPDATE SET grid_layout = EXCLUDED.grid_layout, updated_at = NOW()
                RETURNING formation_type, grid_layout, updated_at;
            """, (user_id, req.formation_type, json.dumps(req.grid_layout)))
            conn.commit()
            return cur.fetchone()

# ---------------------------------------------------------------------------
# MODULE 5: GACHA WITH PITY & AUDIT LOGS
# ---------------------------------------------------------------------------
HERO_POOL = {
    "UR": ["lu_bo", "quan_vu"],
    "SSR": ["trieu_van", "gia_cat_luong", "chu_du"],
    "SR": ["dieu_thuyen", "dien_vi", "tao_nhan", "hoang_trung", "ma_sieu"],
    "R": ["hoang_cai", "sai_van_co", "bang_thong", "tieu_kieu"]
}

@app.post("/api/gacha/pull")
def pull_gacha(req: GachaPullReq, user_id: str = DEFAULT_USER_ID):
    results = []
    with get_db() as conn:
        with conn.cursor() as cur:
            # Check wallet for cost: 100 Gold per pull
            cost = req.pull_count * 100
            cur.execute("SELECT gold FROM user_wallets WHERE user_id = %s;", (user_id,))
            wallet = cur.fetchone()
            if not wallet or wallet["gold"] < cost:
                raise HTTPException(status_code=400, detail="Không đủ KNB để triệu hồi!")

            # Deduct gold
            cur.execute("UPDATE user_wallets SET gold = gold - %s WHERE user_id = %s;", (cost, user_id))

            # Fetch Pity
            cur.execute("""
                SELECT pull_count_since_last_ssr, pull_count_since_last_ur, is_next_rate_up_guaranteed
                FROM user_gacha_pity
                WHERE user_id = %s AND banner_id = %s;
            """, (user_id, req.banner_id))
            pity = cur.fetchone()
            pity_ssr = pity["pull_count_since_last_ssr"] if pity else 0
            pity_ur = pity["pull_count_since_last_ur"] if pity else 0
            guaranteed = pity["is_next_rate_up_guaranteed"] if pity else False

            for _ in range(req.pull_count):
                pity_ssr += 1
                pity_ur += 1
                rand = random.random()

                # Pity rules: Hard pity UR at 80 pulls, SSR at 40 pulls
                if pity_ur >= 80 or rand < 0.02:
                    rarity = "UR"
                    hero = "lu_bo" if (guaranteed or random.random() < 0.5) else random.choice(HERO_POOL["UR"])
                    pity_ur = 0
                    pity_ssr = 0
                    guaranteed = False
                elif pity_ssr >= 40 or rand < 0.08:
                    rarity = "SSR"
                    hero = random.choice(HERO_POOL["SSR"])
                    pity_ssr = 0
                elif rand < 0.35:
                    rarity = "SR"
                    hero = random.choice(HERO_POOL["SR"])
                else:
                    rarity = "R"
                    hero = random.choice(HERO_POOL["R"])

                results.append({"hero_id": hero, "rarity": rarity, "pity_ur": pity_ur, "pity_ssr": pity_ssr})

                # Insert to user_heroes or grant fragments if duplicate
                cur.execute("""
                    INSERT INTO user_heroes (user_id, hero_id, star, level, exp)
                    VALUES (%s, %s, 1, 1, 0)
                    ON CONFLICT (user_id, hero_id) DO UPDATE SET star = LEAST(6, user_heroes.star + 1);
                """, (user_id, hero))

                # Insert audit log into monthly partitioned table
                cur.execute("""
                    INSERT INTO gacha_audit_logs 
                        (user_id, banner_id, result_type, result_id, rarity, pull_pity_count, currency_spent, created_at)
                    VALUES (%s, %s, 'HERO', %s, %s, %s, 'GOLD', NOW());
                """, (user_id, req.banner_id, hero, rarity, pity_ur))

            # Update Pity
            cur.execute("""
                INSERT INTO user_gacha_pity (user_id, banner_id, pull_count_since_last_ssr, pull_count_since_last_ur, is_next_rate_up_guaranteed)
                VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT (user_id, banner_id) DO UPDATE
                SET pull_count_since_last_ssr = %s,
                    pull_count_since_last_ur = %s,
                    is_next_rate_up_guaranteed = %s,
                    updated_at = NOW();
            """, (user_id, req.banner_id, pity_ssr, pity_ur, guaranteed, pity_ssr, pity_ur, guaranteed))

            conn.commit()

    return {"results": results, "pity_ur": pity_ur, "pity_ssr": pity_ssr, "gold_spent": cost}

# ---------------------------------------------------------------------------
# MODULE 6: COMBAT ENGINE & DETERMINISTIC REPLAY
# ---------------------------------------------------------------------------
@app.post("/api/battle/simulate")
def simulate_battle(req: BattleSimReq, user_id: str = DEFAULT_USER_ID):
    seed = random.randint(100000, 999999)
    rng = random.Random(seed)

    # Fetch attacker and defender lineups
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT grid_layout FROM user_formations WHERE user_id = %s AND formation_type = 'PVE_DEFAULT';", (user_id,))
            atk_f = cur.fetchone()
            cur.execute("SELECT lineup_payload FROM arena_defense_snapshots WHERE user_id = %s;", (req.defender_id,))
            def_f = cur.fetchone()

    atk_heroes = ["lu_bo", "quan_vu", "trieu_van", "gia_cat_luong", "chu_du"]
    def_heroes = ["dien_vi", "tao_nhan"]

    # Run deterministic simulation (up to 5 rounds)
    combat_log = []
    round_num = 1
    atk_hp = 12000
    def_hp = 9500

    while round_num <= 5 and atk_hp > 0 and def_hp > 0:
        combat_log.append(f"=== Hiệp {round_num} ===")
        # Attacker action
        actor = rng.choice(atk_heroes)
        dmg = rng.randint(1200, 2400)
        is_crit = rng.random() < 0.25
        if is_crit:
            dmg = int(dmg * 1.8)
        def_hp = max(0, def_hp - dmg)
        crit_str = " (BẠO KÍCH! 🔥)" if is_crit else ""
        combat_log.append(f"⚔️ {actor} tấn công đối phương, gây {dmg} sát thương!{crit_str}")

        if def_hp <= 0:
            break

        # Defender action
        def_actor = rng.choice(def_heroes)
        def_dmg = rng.randint(800, 1500)
        atk_hp = max(0, atk_hp - def_dmg)
        combat_log.append(f"🛡️ {def_actor} phản công, gây {def_dmg} sát thương lên đội Chúa Công!")

        round_num += 1

    attacker_win = def_hp <= 0
    result_str = "CHIẾN THẮNG HUY HOÀNG! 🏆" if attacker_win else "THẤT BẠI TRONG GANG TẤC!"
    combat_log.append(f"=== Kết Quả: {result_str} ===")

    # Save to battle_replays partition table
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO battle_replays 
                    (battle_type, attacker_id, defender_id, is_attacker_win, seed, input_actions, created_at)
                VALUES (%s, %s, %s, %s, %s, %s, NOW())
                RETURNING id, created_at;
            """, (req.battle_type, user_id, req.defender_id, attacker_win, seed, json.dumps(combat_log)))
            conn.commit()
            replay_record = cur.fetchone()

    return {
        "replay_id": replay_record["id"],
        "seed": seed,
        "attacker_win": attacker_win,
        "rounds": round_num,
        "log": combat_log
    }

# ---------------------------------------------------------------------------
# MODULE 7: MAILS & REWARDS
# ---------------------------------------------------------------------------
@app.get("/api/mail")
def get_mails(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT m.id, m.title, m.content, m.attached_rewards, m.expires_at,
                       r.is_read, r.is_claimed
                FROM system_mails m
                JOIN mail_recipients r ON m.id = r.mail_id
                WHERE r.user_id = %s;
            """, (user_id,))
            return cur.fetchall()

@app.post("/api/mail/claim/{mail_id}")
def claim_mail(mail_id: int, user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT m.attached_rewards, r.is_claimed 
                FROM system_mails m
                JOIN mail_recipients r ON m.id = r.mail_id
                WHERE m.id = %s AND r.user_id = %s;
            """, (mail_id, user_id))
            mail = cur.fetchone()
            if not mail:
                raise HTTPException(status_code=404, detail="Mail not found")
            if mail["is_claimed"]:
                raise HTTPException(status_code=400, detail="Thư này đã được nhận phần thưởng trước đó")

            # Parse attached rewards and credit wallet
            rewards = mail["attached_rewards"]
            for r in rewards:
                if r.get("type") == "SILVER":
                    cur.execute("UPDATE user_wallets SET silver = silver + %s WHERE user_id = %s;", (r["val"], user_id))
                elif r.get("type") == "GOLD":
                    cur.execute("UPDATE user_wallets SET gold = gold + %s WHERE user_id = %s;", (r["val"], user_id))

            cur.execute("UPDATE mail_recipients SET is_claimed = true, is_read = true, claimed_at = NOW() WHERE mail_id = %s AND user_id = %s;", (mail_id, user_id))
            conn.commit()
            return {"status": "claimed", "rewards": rewards}

# ---------------------------------------------------------------------------
# DATABASE OVERVIEW / LIVE METRICS
# ---------------------------------------------------------------------------
@app.get("/api/db-overview")
def get_db_overview():
    counts = {}
    tables = [
        "users", "user_auth_providers", "user_profiles", "user_wallets",
        "user_heroes", "user_formations", "user_inventories", "user_equipments",
        "campaign_progress", "tower_progress", "daily_dungeon_records",
        "user_gacha_pity", "gacha_audit_logs", "iap_transactions",
        "arena_profiles", "arena_defense_snapshots", "battle_replays",
        "system_mails", "mail_recipients", "system_configs"
    ]
    with get_db() as conn:
        with conn.cursor() as cur:
            for t in tables:
                try:
                    cur.execute(f"SELECT COUNT(*) as count FROM {t};")
                    counts[t] = cur.fetchone()["count"]
                except Exception:
                    counts[t] = 0
    return {"table_counts": counts, "total_modules": 7}

# ---------------------------------------------------------------------------
# WEB GAME PORTAL & DASHBOARD (SERVED AT /)
# ---------------------------------------------------------------------------
@app.get("/", response_class=HTMLResponse)
def index_page():
    return """
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Tam Quốc 3D — Pixel Tactics Game Server Portal</title>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Press+Start+2P&family=Be+Vietnam+Pro:wght@400;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg: #0f0d14;
            --panel-bg: #181420;
            --panel-border: #d4af37;
            --panel-border-inner: #644b1e;
            --gold: #ffdf40;
            --crimson: #a02830;
            --jade: #32b474;
            --cyan: #3ac0f0;
            --text-main: #f0e6d2;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            background-color: var(--bg);
            background-image: radial-gradient(circle at 50% 20%, #25162a 0%, #0d0b12 80%);
            color: var(--text-main);
            font-family: 'Be Vietnam Pro', sans-serif;
            min-height: 100vh;
            padding: 18px 24px 60px;
        }
        header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 2px solid var(--panel-border);
            padding-bottom: 14px;
            margin-bottom: 22px;
        }
        .brand h1 {
            font-family: 'Cinzel', serif;
            font-size: 26px;
            color: var(--gold);
            letter-spacing: 1px;
            text-shadow: 0 0 12px rgba(212, 175, 55, 0.4);
        }
        .brand p { font-size: 13px; color: #a59e90; }
        .server-badges { display: flex; gap: 10px; align-items: center; }
        .badge {
            background: #251c30;
            border: 1px solid #785a25;
            padding: 5px 12px;
            border-radius: 4px;
            font-size: 12px;
            display: flex;
            align-items: center;
            gap: 6px;
        }
        .dot { width: 8px; height: 8px; border-radius: 50%; background: #32d74b; box-shadow: 0 0 6px #32d74b; }
        
        /* TopBar */
        .topbar {
            background: linear-gradient(180deg, #2a1f33 0%, #1a1424 100%);
            border: 2px solid var(--panel-border);
            border-radius: 6px;
            padding: 12px 18px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 22px;
            box-shadow: 0 4px 16px rgba(0,0,0,0.5);
        }
        .profile-area { display: flex; align-items: center; gap: 14px; }
        .avatar-box {
            width: 52px; height: 52px;
            border-radius: 50%;
            border: 2px solid var(--panel-border);
            background: #401018;
            display: flex; align-items: center; justify-content: center;
            font-size: 22px;
        }
        .player-info h3 { color: var(--gold); font-size: 16px; display: flex; gap: 8px; align-items: center; }
        .vip-tag {
            background: linear-gradient(135deg, #a02028, #701060);
            color: #fff;
            padding: 2px 8px;
            border-radius: 3px;
            font-size: 11px;
            border: 1px solid #ffd700;
        }
        .currencies { display: flex; gap: 20px; align-items: center; }
        .cur-item { display: flex; align-items: center; gap: 6px; font-weight: 600; font-size: 14px; }
        
        /* Layout Grid */
        .grid-main {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 22px;
        }
        .card {
            background: var(--panel-bg);
            border: 1px solid var(--panel-border);
            border-radius: 6px;
            padding: 16px 18px;
            box-shadow: 0 4px 14px rgba(0,0,0,0.4);
        }
        .card-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 1px solid var(--panel-border-inner);
            padding-bottom: 10px;
            margin-bottom: 14px;
        }
        .card-header h2 { font-size: 16px; color: var(--gold); display: flex; align-items: center; gap: 8px; }
        
        /* 3x3 Battle Grid */
        .battle-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 10px;
            background: #110e16;
            padding: 14px;
            border-radius: 6px;
            border: 1px solid #4a3820;
        }
        .cell {
            background: #201a2c;
            border: 1px dashed #785a25;
            min-height: 70px;
            border-radius: 4px;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            font-size: 12px;
            padding: 4px;
            text-align: center;
        }
        .cell.active {
            background: #361a22;
            border: 1px solid #e04040;
            box-shadow: inset 0 0 8px rgba(224, 64, 64, 0.4);
        }
        .hero-name { font-weight: 700; color: #ffd760; }
        .hero-role { font-size: 10px; color: #aaa; }

        /* Action Buttons */
        .btn {
            background: linear-gradient(180deg, #a02830 0%, #68151c 100%);
            color: #fff;
            border: 1px solid #ffd700;
            padding: 9px 18px;
            border-radius: 4px;
            font-size: 13px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.15s;
        }
        .btn:hover { background: #b8303a; transform: translateY(-1px); }
        .btn-gold {
            background: linear-gradient(180deg, #d4af37 0%, #997818 100%);
            color: #1a1202;
        }
        .btn-gold:hover { background: #e5bf45; }
        
        /* Log Box */
        .log-box {
            background: #0d0a12;
            border: 1px solid #4a3820;
            border-radius: 4px;
            padding: 12px;
            font-family: monospace;
            font-size: 12px;
            height: 180px;
            overflow-y: auto;
            line-height: 1.5;
            white-space: pre-wrap;
        }
        
        /* Database Overview */
        .db-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 12px;
        }
        .db-table th, .db-table td {
            border: 1px solid #332640;
            padding: 7px 10px;
            text-align: left;
        }
        .db-table th { background: #221a2c; color: var(--gold); }
        .db-table tr:nth-child(even) { background: #15111c; }
    </style>
</head>
<body>
    <header>
        <div class="brand">
            <h1>TAM QUỐC 3D — PROJECT PIXEL TACTICS</h1>
            <p>Authoritative Game Server & PostgreSQL Database Stack</p>
        </div>
        <div class="server-badges">
            <div class="badge"><div class="dot"></div> Postgres 16 (Online)</div>
            <div class="badge"><div class="dot"></div> Redis 7 (Cache Online)</div>
            <a href="/docs" target="_blank" class="btn btn-gold" style="text-decoration:none;">📖 Swagger API</a>
        </div>
    </header>

    <!-- TopBar -->
    <div class="topbar">
        <div class="profile-area">
            <div class="avatar-box">🥷</div>
            <div class="player-info">
                <h3><span id="player-name">Long Tướng Quân</span> <span class="vip-tag" id="vip-level">VIP 11</span></h3>
                <p style="font-size: 12px; color: #a59e90;">Quân hàm: Đại Tướng Quân • Lực chiến: <span style="color:#ff8040; font-weight:700;" id="combat-power">188,880</span></p>
            </div>
        </div>
        <div class="currencies">
            <div class="cur-item">🪙 Bạc: <span id="val-silver" style="color:#d0d0d0;">2,145,370</span></div>
            <div class="cur-item">✨ KNB: <span id="val-gold" style="color:var(--gold);">150,000</span></div>
            <div class="cur-item">⚡ Thể Lực: <span id="val-stamina" style="color:#ff4060;">100/145</span></div>
            <button class="btn btn-gold" onclick="claimStamina()" style="padding:4px 10px; font-size:11px;">+ Hồi Thể Lực</button>
        </div>
    </div>

    <!-- Main Content -->
    <div class="grid-main">
        <!-- 3x3 Formation Inspector -->
        <div class="card">
            <div class="card-header">
                <h2>📋 Trận Hình 3x3 (Module 2: Formations)</h2>
                <button class="btn" onclick="fetchFormation()">Tải Lại</button>
            </div>
            <div style="font-size: 12px; margin-bottom: 8px; color: #b0a898;">Bố cục 3 hàng (Tiền quân, Trung quân, Hậu quân) lưu trong <code>user_formations</code>:</div>
            <div class="battle-grid" id="grid-container">
                <!-- 3x3 populated dynamically -->
            </div>
        </div>

        <!-- Authoritative Combat Simulator -->
        <div class="card">
            <div class="card-header">
                <h2>⚔️ Máy Chủ Chiến Đấu (Module 6: Battles & Replay)</h2>
                <button class="btn" onclick="runBattleSim()">Giao Đấu Ngay</button>
            </div>
            <div style="font-size: 12px; margin-bottom: 8px; color: #b0a898;">Mô phỏng trọn vẹn trận đấu theo Seed RNG, lưu replay vào <code>battle_replays</code>:</div>
            <div class="log-box" id="battle-log">Bấm "Giao Đấu Ngay" để máy chủ thực thi tính toán sát thương turn-based...</div>
        </div>
    </div>

    <div class="grid-main">
        <!-- Gacha Simulator -->
        <div class="card">
            <div class="card-header">
                <h2>🎰 Đài Triệu Hồi (Module 5: Gacha & Pity)</h2>
                <div>
                    <button class="btn btn-gold" onclick="pullGacha(1)">Quay x1 (100 KNB)</button>
                    <button class="btn btn-gold" onclick="pullGacha(10)">Quay x10 (1000 KNB)</button>
                </div>
            </div>
            <div style="font-size: 12px; margin-bottom: 8px; color: #b0a898;">Bộ đếm Pity lưu trong <code>user_gacha_pity</code>, lịch sử phân vùng <code>gacha_audit_logs</code>:</div>
            <div class="log-box" id="gacha-log">Chưa có lượt quay nào trong phiên này...</div>
        </div>

        <!-- Database Overview -->
        <div class="card">
            <div class="card-header">
                <h2>🗄️ Tổng Quan 7 Modules PostgreSQL</h2>
                <button class="btn" onclick="fetchDbOverview()">Cập Nhật</button>
            </div>
            <div style="max-height: 180px; overflow-y: auto;">
                <table class="db-table" id="db-overview-table">
                    <thead><tr><th>Module</th><th>Bảng Dữ Liệu</th><th>Bản Ghi</th></tr></thead>
                    <tbody id="db-overview-body"><tr><td colspan="3">Đang nạp...</td></tr></tbody>
                </table>
            </div>
        </div>
    </div>

    <script>
        async function loadProfile() {
            try {
                const res = await fetch('/api/profile');
                const data = await res.json();
                document.getElementById('player-name').innerText = data.display_name;
                document.getElementById('vip-level').innerText = 'VIP ' + data.vip_level;
                document.getElementById('combat-power').innerText = Number(data.combat_power).toLocaleString();
            } catch(e) { console.error(e); }
        }

        async function loadWallet() {
            try {
                const res = await fetch('/api/wallet');
                const data = await res.json();
                document.getElementById('val-silver').innerText = Number(data.silver).toLocaleString();
                document.getElementById('val-gold').innerText = Number(data.gold).toLocaleString();
                document.getElementById('val-stamina').innerText = data.stamina + '/' + data.max_stamina;
            } catch(e) { console.error(e); }
        }

        async function claimStamina() {
            await fetch('/api/wallet/claim-stamina', {method: 'POST'});
            loadWallet();
        }

        async function fetchFormation() {
            const container = document.getElementById('grid-container');
            container.innerHTML = 'Đang tải...';
            try {
                const res = await fetch('/api/formations');
                const data = await res.json();
                const pve = data.find(f => f.formation_type === 'PVE_DEFAULT') || data[0];
                container.innerHTML = '';
                const rows = ['front', 'mid', 'back'];
                const rowNames = {'front': 'Tiền Quân', 'mid': 'Trung Quân', 'back': 'Hậu Quân'};
                rows.forEach(r => {
                    const rowArr = pve.grid_layout[r] || [null, null, null];
                    rowArr.forEach((h, idx) => {
                        const cell = document.createElement('div');
                        cell.className = 'cell' + (h ? ' active' : '');
                        cell.innerHTML = h 
                            ? `<div class="hero-name">⚔️ ${h.toUpperCase()}</div><div class="hero-role">${rowNames[r]} [${idx}]</div>` 
                            : `<div style="color:#555;">(Trống)</div><div class="hero-role">${rowNames[r]} [${idx}]</div>`;
                        container.appendChild(cell);
                    });
                });
            } catch(e) { container.innerHTML = 'Lỗi nạp trận hình: ' + e; }
        }

        async function runBattleSim() {
            const logBox = document.getElementById('battle-log');
            logBox.innerText = 'Đang gửi yêu cầu mô phỏng lên Server Authoritative Combat Engine...';
            try {
                const res = await fetch('/api/battle/simulate', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({battle_type: 'ASYNC_ARENA'})
                });
                const data = await res.json();
                logBox.innerText = `Replay ID: ${data.replay_id}\nDeterministic RNG Seed: ${data.seed}\nSố Hiệp: ${data.rounds}\nKết quả: ${data.attacker_win ? 'CHIẾN THẮNG' : 'THẤT BẠI'}\n\n` + data.log.join('\n');
            } catch(e) { logBox.innerText = 'Lỗi mô phỏng: ' + e; }
        }

        async function pullGacha(count) {
            const logBox = document.getElementById('gacha-log');
            logBox.innerText = 'Đang quay triệu hồi...';
            try {
                const res = await fetch('/api/gacha/pull', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({banner_id: 'banner_lu_bo_limited', pull_count: count})
                });
                const data = await res.json();
                loadWallet();
                let txt = `Đã quay ${count} lượt. Chi: ${data.gold_spent} KNB\nPity UR: ${data.pity_ur}/80 | Pity SSR: ${data.pity_ssr}/40\n\n`;
                data.results.forEach((r, idx) => {
                    const color = r.rarity === 'UR' ? '🔴' : (r.rarity === 'SSR' ? '🟡' : (r.rarity === 'SR' ? '🟣' : '⚪'));
                    txt += `${idx+1}. ${color} [${r.rarity}] ${r.hero_id.toUpperCase()}\n`;
                });
                logBox.innerText = txt;
            } catch(e) { logBox.innerText = 'Lỗi quay: ' + e; }
        }

        async function fetchDbOverview() {
            const body = document.getElementById('db-overview-body');
            try {
                const res = await fetch('/api/db-overview');
                const data = await res.json();
                body.innerHTML = '';
                const moduleMap = {
                    'users': '1. Auth & Wallets', 'user_auth_providers': '1. Auth & Wallets', 'user_profiles': '1. Auth & Wallets', 'user_wallets': '1. Auth & Wallets',
                    'user_heroes': '2. Heroes & Formations', 'user_formations': '2. Heroes & Formations',
                    'user_inventories': '3. Inventories & Gear', 'user_equipments': '3. Inventories & Gear',
                    'campaign_progress': '4. Progression', 'tower_progress': '4. Progression', 'daily_dungeon_records': '4. Progression',
                    'user_gacha_pity': '5. Gacha & Audit', 'gacha_audit_logs': '5. Gacha & Audit', 'iap_transactions': '5. Gacha & Audit',
                    'arena_profiles': '6. Arena & Replays', 'arena_defense_snapshots': '6. Arena & Replays', 'battle_replays': '6. Arena & Replays',
                    'system_mails': '7. Mails & Configs', 'mail_recipients': '7. Mails & Configs', 'system_configs': '7. Mails & Configs'
                };
                for (const [tbl, count] of Object.entries(data.table_counts)) {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `<td>${moduleMap[tbl] || 'System'}</td><td><code>${tbl}</code></td><td style="font-weight:700; color:#ffdf40;">${count}</td>`;
                    body.appendChild(tr);
                }
            } catch(e) { body.innerHTML = '<tr><td colspan="3">Lỗi tải dữ liệu</td></tr>'; }
        }

        // Initialize on load
        window.addEventListener('DOMContentLoaded', () => {
            loadProfile();
            loadWallet();
            fetchFormation();
            fetchDbOverview();
        });
    </script>
</body>
</html>
    """
