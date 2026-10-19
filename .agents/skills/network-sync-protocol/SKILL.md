---
name: network-sync-protocol
description: Client-Server communication protocols, REST API contracts, and offline fallback strategies in Tam Quoc 3D
triggers:
  - network
  - http
  - api
  - sync
  - client-server
  - endpoints
---

# Network Sync & Client-Server Protocol Skill

This skill defines the networking patterns, API contracts, and client-server synchronization protocols between Godot 4.3 C# and the ASP.NET Core 8 game backend.

## 1. NetworkManager Architecture
- **Location**: [`core/network/NetworkManager.cs`](file:///home/annhan/Documents/src/tamquoc3d/core/network/NetworkManager.cs)
- **Autoload Path**: `/root/NetworkManager`
- **Default Base URL**: `http://localhost:8000`
- **Threading Model**: Asynchronous I/O via `System.Net.Http.HttpClient`, dispatched back to the Godot Main Thread via `Callable.From().CallDeferred()`.

## 2. Standard REST API Contracts

### Health & Status:
- `GET /health`:
  - Response: `{"status": "healthy", "database_connected": true, "redis_connected": true, "game_version": "1.0.0", "timestamp": 0}`

### Player Profile:
- `GET /api/profile`:
  - Response: `{"id": "...", "display_name": "...", "level": 80, "vip_level": 11, "combat_power": 125000}`
  - Client Event: Emits `EventBus.ProfileUpdated(profileData)`.

### Currency & Wallets:
- `GET /api/wallet`:
  - Response: `{"silver": 2145370, "gold": 149300, "stamina": 100, "max_stamina": 145}`
  - Client Event: Emits `EventBus.CurrencyChanged("silver", ...)`, `EventBus.CurrencyChanged("gold", ...)`.

### 3x3 Formations:
- `GET /api/formations?type=PVE_DEFAULT`:
  - Response: `{"formation_type": "PVE_DEFAULT", "grid_layout": {"front": [...], "mid": [...], "back": [...]}}`
- `POST /api/formations`:
  - Payload: `{"formation_type": "PVE_DEFAULT", "grid_layout": {"front": [...], "mid": [...], "back": [...]}}`
  - Response: `{"status": "saved", "updated_at": "..."}`

### Gacha Pulls:
- `POST /api/gacha/pull`:
  - Payload: `{"banner_id": "standard_hero_banner", "pull_count": 10}`
  - Response: `{"results": [{"hero_id": "...", "rarity": "SSR", "is_new": true}], "pity_ur": 35}`

### Battle Simulation:
- `POST /api/battle/simulate`:
  - Payload: `{"attacker_formation": {...}, "defender_formation": {...}, "seed": 964658}`
  - Response: `{"replay_id": "...", "winner": "attacker", "rounds_count": 5, "log": [...]}`

## 3. Resilience & Offline Fallback Strategy
1. **Connection Failure Resilience**:
   - If server is unreachable, `NetworkManager.IsServerOnline` is set to `false`.
   - The game falls back to local data resources in `data/database/` without crashing.
   - UI reflects offline status (`● Offline` badge in `TopBar`).
2. **Timeout Settings**:
   - HTTP Client timeout is set to 5 seconds (`Timeout = TimeSpan.FromSeconds(5)`).
3. **Data Deserialization Safety**:
   - Always wrap JSON parsing in try-catch and validate nullability before passing to game state.
