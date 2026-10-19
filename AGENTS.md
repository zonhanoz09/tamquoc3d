# AI Agent Work Instructions

Welcome AI Agent. This repository contains the source code for **Tam Quoc 3D (Project Pixel Tactics)**, built on **Godot 4.3 Mono** with **C# (.NET 8)**.

Before modifying or generating code, please review the full architectural and directory reference at:
👉 **[docs/ARCHITECTURE_AND_DIRECTORIES.md](file:///home/annhan/Documents/src/tamquoc3d/docs/ARCHITECTURE_AND_DIRECTORIES.md)**

---

## 🛠️ AI Agent Skills Directory (`.agents/skills/` & `.gemini/skills/`)
The repository is equipped with specialized Agent Skills covering all technical domains of the project:

1. **[`godot-csharp-architecture`](file:///home/annhan/Documents/src/tamquoc3d/.agents/skills/godot-csharp-architecture/SKILL.md)**: Godot 4.3 C# (.NET 8) standards, thread safety (`Callable.From().CallDeferred()`), node lifecycle cleanup in `_ExitTree()`, Autoload rules.
2. **[`pixel-art-pipeline`](file:///home/annhan/Documents/src/tamquoc3d/.agents/skills/pixel-art-pipeline/SKILL.md)**: 16-bit Three Kingdoms aesthetic, color palettes, 9-slice NinePatchRect, TextureFilter Nearest, and asset generation tools.
3. **[`postgres-redis-backend`](file:///home/annhan/Documents/src/tamquoc3d/.agents/skills/postgres-redis-backend/SKILL.md)**: Docker Compose stack (PostgreSQL 16, Redis 7, ASP.NET Core 8), 7 core game modules schema, triggers, and partitioning rules.
4. **[`battle-combat-engine`](file:///home/annhan/Documents/src/tamquoc3d/.agents/skills/battle-combat-engine/SKILL.md)**: 3x3 tactical grid, speed initiative, rage/morale bars, skill targeting logic, and deterministic PRNG battle replays.
5. **[`network-sync-protocol`](file:///home/annhan/Documents/src/tamquoc3d/.agents/skills/network-sync-protocol/SKILL.md)**: Client-Server communication contracts, `NetworkManager` Autoload, async REST endpoints, and offline fallback.
6. **[`game-testing-qa`](file:///home/annhan/Documents/src/tamquoc3d/.agents/skills/game-testing-qa/SKILL.md)**: Headless testing commands (`godot --headless res://tests/...`), exit codes, integration test procedures.

---

## Essential Guidelines for AI Agents:
1. **Language:** Strictly **C# (.NET 8)**. Never create `.gd` (GDScript) files.
2. **Godot Class Declaration:**
   - Every Godot node or resource must include `[GlobalClass]` and `public partial class`.
   - Never omit the `partial` keyword.
3. **Architecture Layers:**
   - Put core systems & autoloads in `core/autoload/`, interfaces in `core/interfaces/`, constants in `core/constants/`.
   - Put data models & resource schemas in `data/resources/`, `.tres` files in `data/database/`.
   - Put gameplay feature controllers and nodes in `features/` (e.g. `features/battle/`, `features/deck_builder/`, `features/campaign/`).
4. **Validation:**
   - Always run `dotnet build` to ensure 0 errors and 0 warnings.
   - Run `godot --headless res://tests/TestClientServerConnection.tscn` or specific test scenes to verify runtime execution.
