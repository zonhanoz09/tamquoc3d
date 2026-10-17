"""
Tam Quoc 3D - FastAPI Game Server Backend
Provides REST APIs for:
1. Auth & Wallets (Users, Profiles, Currencies)
2. Heroes & Formations
3. Inventory & Equipments
4. Campaign & Progression
5. Gacha & Audit Logs (with Pity counters)
6. Combat Engine & Deterministic Battle Simulation
7. Mails & Balance Configurations
"""

import os
import json
import time
import random
from typing import Optional, List, Dict, Any
from contextlib import contextmanager

from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import HTMLResponse
from pydantic import BaseModel
import psycopg2
from psycopg2.extras import RealDictCursor
import redis

# ---------------------------------------------------------------------------
# CONFIGURATION
# ---------------------------------------------------------------------------
DATABASE_URL = os.getenv("DATABASE_URL", "postgresql://tamquoc:tamquoc_secret_2026@tamquoc-postgres:5432/tamquoc_db")
REDIS_URL = os.getenv("REDIS_URL", "redis://tamquoc-redis:6379/0")
DEFAULT_USER_ID = "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11" # Chúa Công seeded

app = FastAPI(title="Tam Quoc 3D Game Server", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@contextmanager
def get_db():
    conn = psycopg2.connect(DATABASE_URL, cursor_factory=RealDictCursor)
    try:
        yield conn
    finally:
        conn.close()

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

class CampaignClearReq(BaseModel):
    stage_id: str = "1-1"
    stars: int = 3
    turns: int = 4
    gold_reward: int = 500
    diamond_reward: int = 50
    stamina_cost: int = 3

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
# MODULE 4: CAMPAIGN PROGRESSION
# ---------------------------------------------------------------------------
@app.get("/api/campaign/progress")
def get_campaign_progress(user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT stage_id, stars, first_clear_claimed, clear_count, best_record_turns, updated_at
                FROM campaign_progress
                WHERE user_id = %s
                ORDER BY stage_id ASC;
            """, (user_id,))
            return cur.fetchall()

@app.post("/api/campaign/clear")
def clear_stage(req: CampaignClearReq, user_id: str = DEFAULT_USER_ID):
    with get_db() as conn:
        with conn.cursor() as cur:
            # Check stamina
            cur.execute("SELECT stamina, silver, gold FROM user_wallets WHERE user_id = %s;", (user_id,))
            w = cur.fetchone()
            if not w or w["stamina"] < req.stamina_cost:
                raise HTTPException(status_code=400, detail="Không đủ Thể Lực để vượt ải!")

            # Deduct stamina, add silver & diamond
            cur.execute("""
                UPDATE user_wallets
                SET stamina = stamina - %s,
                    silver = silver + %s,
                    gold = gold + %s,
                    updated_at = NOW()
                WHERE user_id = %s;
            """, (req.stamina_cost, req.gold_reward, req.diamond_reward, user_id))

            # Upsert stage progress
            cur.execute("""
                INSERT INTO campaign_progress (user_id, stage_id, stars, first_clear_claimed, clear_count, best_record_turns)
                VALUES (%s, %s, %s, true, 1, %s)
                ON CONFLICT (user_id, stage_id) DO UPDATE
                SET stars = GREATEST(campaign_progress.stars, EXCLUDED.stars),
                    clear_count = campaign_progress.clear_count + 1,
                    best_record_turns = LEAST(COALESCE(campaign_progress.best_record_turns, 999), EXCLUDED.best_record_turns),
                    updated_at = NOW()
                RETURNING stage_id, stars, clear_count, best_record_turns;
            """, (user_id, req.stage_id, req.stars, req.turns))

            conn.commit()
            progress = cur.fetchone()
            return {
                "status": "cleared",
                "stage": progress,
                "reward_silver": req.gold_reward,
                "reward_gold": req.diamond_reward
            }

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
                SELECT id, title, body, attachments, is_read, is_claimed, expire_at, created_at
                FROM user_mails
                WHERE user_id = %s AND expire_at > NOW()
                ORDER BY created_at DESC;
            """, (user_id,))
            return cur.fetchall()

# ---------------------------------------------------------------------------
# SYSTEM STATS & HTML DASHBOARD
# ---------------------------------------------------------------------------
@app.get("/api/db-overview")
def db_overview():
    tables = [
        "users", "user_auth_providers", "user_profiles", "user_wallets",
        "user_heroes", "user_formations", "user_inventories", "user_equipments",
        "campaign_progress", "tower_progress", "daily_dungeon_records",
        "user_gacha_pity", "gacha_audit_logs", "iap_transactions",
        "arena_profiles", "battle_replays", "system_mail_templates", "user_mails"
    ]
    counts = {}
    with get_db() as conn:
        with conn.cursor() as cur:
            for t in tables:
                try:
                    cur.execute(f"SELECT COUNT(*) as count FROM {t};")
                    counts[t] = cur.fetchone()["count"]
                except Exception:
                    conn.rollback()
                    counts[t] = -1
    return counts

@app.get("/", response_class=HTMLResponse)
def index_portal():
    return """
    <!DOCTYPE html>
    <html lang="vi">
    <head>
        <meta charset="UTF-8">
        <title>Tam Quốc 3D - Game Backend Portal</title>
        <style>
            body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #0f172a; color: #f8fafc; margin: 0; padding: 24px; }
            .card { background: #1e293b; border: 1px solid #334155; border-radius: 12px; padding: 24px; margin-bottom: 20px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.3); }
            h1 { color: #f59e0b; margin-top: 0; display: flex; align-items: center; gap: 10px; }
            .badge { background: #10b981; color: white; padding: 4px 10px; border-radius: 9999px; font-size: 13px; font-weight: bold; }
            .btn { display: inline-block; background: #3b82f6; color: white; padding: 10px 16px; border-radius: 6px; text-decoration: none; font-weight: 500; }
            .btn:hover { background: #2563eb; }
            pre { background: #090d16; padding: 12px; border-radius: 6px; overflow-x: auto; color: #38bdf8; font-size: 13px; }
        </style>
    </head>
    <body>
        <div class="card">
            <h1>🏰 Tam Quốc 3D Game Server Backend <span class="badge">Running</span></h1>
            <p>Hệ thống máy chủ dịch vụ quản lý dữ liệu người chơi, trận hình 3x3, gacha, và mô phỏng trận đấu Server-Authoritative.</p>
            <p><strong>Cơ sở dữ liệu:</strong> PostgreSQL 16 & Redis 7</p>
            <a class="btn" href="/docs" target="_blank">📖 Mở OpenAPI Swagger UI (/docs)</a>
        </div>
    </body>
    </html>
    """
