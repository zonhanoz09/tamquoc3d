---
name: pixel-art-pipeline
description: Standards and generation pipelines for Pixel Art assets, UI NinePatchRect, and SpriteFrames in Tam Quoc 3D
triggers:
  - pixelart
  - sprite
  - vfx
  - ui
  - texture
  - ninepatch
---

# Pixel Art & UI Asset Pipeline Skill

This skill defines the visual rules, asset specifications, and automated generation pipelines for 2D pixel art sprites, UI components, and VFX in **Tam Quoc 3D**.

## 1. Visual Style & Color Palettes
The game adopts a **16-bit / 32-bit Tactical JRPG aesthetic** (inspired by Final Fantasy Tactics & Tactics Ogre).

### Faction Color Coding:
- **Thục (Shu)**: Emerald Green (`#2E7D32`), Forest (`#1B5E20`), Gold accents (`#FFD700`).
- **Ngụy (Wei)**: Imperial Navy Blue (`#1565C0`), Royal Indigo (`#283593`), Silver accents (`#CFD8DC`).
- **Ngô (Wu)**: Crimson Red (`#C62828`), Flame Orange (`#EF6C00`), Bronze accents (`#D7CCC8`).
- **Quần Hùng (Others)**: Deep Violet (`#6A1B9A`), Obsidian Charcoal (`#212121`), Amber (`#FFB300`).

## 2. Sprite Dimensions & Grid Standards
- **Heroes / Units**:
  - Combat Chibi Sprites: 48x48 or 64x64 pixels.
  - Hub Avatars / Portraits: 64x64 or 128x128 pixels.
- **UI Icons**:
  - Currency / TopBar icons: 24x24 or 32x32 pixels.
  - Shortcut / Navigation icons: 48x48 pixels.
- **Skill VFX**:
  - Slash / Impact / Spell animations: 64x64 to 96x96 pixels per frame.
- **UI Panels & Frames**:
  - NinePatchRect borders: 3 to 6 pixels border margin, repeating or stretching center.

## 3. Godot Texture Settings (CRITICAL)
Pixel art must remain crisp and never be blurred by bilinear/trilinear interpolation:
1. In Godot `.tscn` nodes: Set `TextureFilter = TextureFilterEnum.Nearest` (Texture Filter 1).
2. For NinePatchRect UI:
   ```csharp
   ninePatch.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
   ninePatch.PatchMarginLeft = 4;
   ninePatch.PatchMarginTop = 4;
   ninePatch.PatchMarginRight = 4;
   ninePatch.PatchMarginBottom = 4;
   ```

## 4. Asset Generation Tools
- **Hub UI & Icon Generator**:
  Run the automated Python PIL generator located at [`tools/generate_hub_assets.py`](file:///home/annhan/Documents/src/tamquoc3d/tools/generate_hub_assets.py):
  ```bash
  python3 tools/generate_hub_assets.py
  ```
  Generates 45+ UI pixel icons (buttons, banners, frames, avatars, currencies) in `assets/ui/hub/`.

- **Pixelart MCP Tools**:
  When using MCP tools:
  - `call_mcp_tool(ServerName="pixelart", ToolName="generate_hero_spritesheet", ...)`
  - `call_mcp_tool(ServerName="pixelart", ToolName="generate_skill_vfx", ...)`
  - `call_mcp_tool(ServerName="pixelart", ToolName="slice_spritesheet", ...)`
  - `call_mcp_tool(ServerName="pixelart", ToolName="generate_sprite_frames_tres", ...)`

## 5. File Formats & Paths
- Raw PNG textures: `assets/ui/`, `assets/sprites/heroes/`, `assets/sprites/vfx/`.
- Resource definitions: `data/resources/`.
- Animated SpriteFrames: `data/database/animations/*.tres`.
