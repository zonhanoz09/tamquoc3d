import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { spawn } from "child_process";
import path from "path";
import fs from "fs";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const PROJECT_ROOT = path.resolve(__dirname, "../..");

const server = new McpServer({
  name: "pixelart-mcp",
  version: "1.0.0"
});

// Tool 1: Generate Hero HD-2D Spritesheet & Godot SpriteFrames
server.tool(
  "generate_hero_spritesheet",
  {
    hero_id: z.string().describe("Hero identifier (e.g., 'trieu_van', 'lu_bo', 'quan_vu')"),
    frame_size: z.number().default(64).describe("Size of each square frame in pixels (default: 64)")
  },
  async ({ hero_id, frame_size }) => {
    return new Promise((resolve) => {
      const scriptPath = path.join(__dirname, "pixel_core.py");
      const outImg = path.join(PROJECT_ROOT, `assets/heroes/${hero_id}/spritesheet_hd.png`);
      const outTres = path.join(PROJECT_ROOT, `assets/heroes/${hero_id}/sprite_frames.tres`);

      const py = spawn("python3", [scriptPath, hero_id, outImg, outTres]);
      let stdout = "";
      let stderr = "";

      py.stdout.on("data", (data) => { stdout += data.toString(); });
      py.stderr.on("data", (data) => { stderr += data.toString(); });

      py.on("close", (code) => {
        if (code === 0) {
          resolve({
            content: [
              {
                type: "text",
                text: `✅ Generated HD-2D spritesheet for ${hero_id} successfully!\nOutput Image: ${outImg}\nSpriteFrames .tres: ${outTres}\n${stdout}`
              }
            ]
          });
        } else {
          resolve({
            isError: true,
            content: [{ type: "text", text: `Error generating sprite for ${hero_id}: ${stderr}` }]
          });
        }
      });
    });
  }
);

// Tool 2: List supported palettes and hero art presets
server.tool(
  "list_palettes",
  {},
  async () => {
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({
            heroes: ["trieu_van", "lu_bo", "quan_vu", "ma_sieu", "tieu_kieu", "gia_cat_luong"],
            styles: ["arcade_16bit", "neo_geo", "cps2_hd2d"],
            frame_grid: "6 columns x 4 rows (Idle: 6f, Attack: 6f, Skill: 6f, Hurt: 3f, Death: 3f)"
          }, null, 2)
        }
      ]
    };
  }
);

// Connect via standard stdio transport
const transport = new StdioServerTransport();
await server.connect(transport);
