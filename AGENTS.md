# AI Agent Work Instructions

Welcome AI Agent. This repository contains the source code for **Tam Quoc 3D (Project Pixel Tactics)**, built on **Godot 4.3 Mono** with **C# (.NET 8)**.

Before modifying or generating code, please review the full architectural and directory reference at:
👉 **[docs/ARCHITECTURE_AND_DIRECTORIES.md](file:///home/annhan/Documents/src/tamquoc3d/docs/ARCHITECTURE_AND_DIRECTORIES.md)**

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
   - Run `godot --quit-after 20` to verify runtime execution.
