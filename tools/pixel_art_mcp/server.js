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
  name: "pixelart",
  version: "1.1.0"
});

function runPython(scriptPath, args) {
  return new Promise((resolve) => {
    const py = spawn("python3", [scriptPath, ...args], {
      cwd: PROJECT_ROOT,
      env: { ...process.env, PYTHONPATH: PROJECT_ROOT }
    });
    let stdout = "";
    let stderr = "";

    py.stdout.on("data", (data) => { stdout += data.toString(); });
    py.stderr.on("data", (data) => { stderr += data.toString(); });

    py.on("close", (code) => {
      if (code === 0) {
        resolve({ success: true, output: stdout });
      } else {
        resolve({ success: false, error: stderr || stdout });
      }
    });
  });
}

// Tool 1: Generate Hero HD-2D Spritesheet & Godot SpriteFrames
server.tool(
  "generate_hero_spritesheet",
  {
    hero_id: z.string().describe("Hero identifier (e.g., 'trieu_van', 'lu_bo', 'quan_vu', or 'all')"),
    frame_size: z.number().default(64).describe("Size of each square frame in pixels (default: 64)")
  },
  async ({ hero_id, frame_size }) => {
    const scriptPath = path.join(__dirname, "pixel_core.py");
    const res = await runPython(scriptPath, [hero_id]);
    if (res.success) {
      return {
        content: [{ type: "text", text: `✅ Generated HD-2D spritesheet for ${hero_id} successfully!\n${res.output}` }]
      };
    } else {
      return {
        isError: true,
        content: [{ type: "text", text: `Error generating sprite for ${hero_id}: ${res.error}` }]
      };
    }
  }
);

// Tool 2: Generate Multi-frame Skill VFX Strips
server.tool(
  "generate_skill_vfx",
  {
    vfx_name: z.string().optional().describe("Specific VFX name (e.g. 'vfx_dragon_white') or omit to generate all")
  },
  async ({ vfx_name }) => {
    const scriptPath = path.join(__dirname, "vfx_core.py");
    const args = [PROJECT_ROOT];
    const res = await runPython(scriptPath, args);
    if (res.success) {
      return {
        content: [{ type: "text", text: `✨ Generated Skill VFX strips successfully!\n${res.output}` }]
      };
    } else {
      return {
        isError: true,
        content: [{ type: "text", text: `Error generating VFX: ${res.error}` }]
      };
    }
  }
);

// Tool 3: Slice Spritesheet into individual frames
server.tool(
  "slice_spritesheet",
  {
    input_path: z.string().describe("Path to input spritesheet image"),
    output_dir: z.string().describe("Directory where sliced frames will be saved"),
    frame_width: z.number().default(64).describe("Width of each frame in pixels"),
    frame_height: z.number().default(64).describe("Height of each frame in pixels")
  },
  async ({ input_path, output_dir, frame_width, frame_height }) => {
    const scriptPath = path.join(PROJECT_ROOT, "tools/pixel_art/slicer.py");
    const res = await runPython(scriptPath, [
      input_path,
      "-o", output_dir,
      "-w", frame_width.toString(),
      "-H", frame_height.toString()
    ]);
    if (res.success) {
      return { content: [{ type: "text", text: `🔪 Sliced successfully:\n${res.output}` }] };
    } else {
      return { isError: true, content: [{ type: "text", text: `Error slicing spritesheet: ${res.error}` }] };
    }
  }
);

// Tool 4: Stitch individual frames into a spritesheet
server.tool(
  "stitch_spritesheet",
  {
    input_dir: z.string().describe("Directory containing image frames to stitch"),
    output_path: z.string().describe("Output spritesheet image path (.png)"),
    cols: z.number().optional().describe("Number of columns for grid (omit for horizontal strip)"),
    padding: z.number().default(0).describe("Padding between frames in pixels")
  },
  async ({ input_dir, output_path, cols, padding }) => {
    const scriptPath = path.join(PROJECT_ROOT, "tools/pixel_art/stitcher.py");
    const args = [input_dir, "-o", output_path, "-p", padding.toString()];
    if (cols) {
      args.push("-c", cols.toString());
    }
    const res = await runPython(scriptPath, args);
    if (res.success) {
      return { content: [{ type: "text", text: `🧵 Stitched successfully:\n${res.output}` }] };
    } else {
      return { isError: true, content: [{ type: "text", text: `Error stitching frames: ${res.error}` }] };
    }
  }
);

// Tool 5: Normalize sprite size (64x64, 96x96, 128x128) with Nearest-Neighbor
server.tool(
  "normalize_sprite_size",
  {
    input_path: z.string().describe("Path to input sprite image"),
    output_path: z.string().describe("Path to save normalized sprite image"),
    target_size: z.number().default(64).describe("Target square pixel dimension (e.g. 64, 96, 128)"),
    mode: z.enum(["scale", "pad", "fit"]).default("scale").describe("Resize mode: 'scale' (nearest-neighbor), 'pad' (center with transparent padding), 'fit' (aspect scale + pad)")
  },
  async ({ input_path, output_path, target_size, mode }) => {
    const scriptPath = path.join(PROJECT_ROOT, "tools/pixel_art/resizer.py");
    const res = await runPython(scriptPath, [
      input_path,
      "-o", output_path,
      "-s", target_size.toString(),
      "-m", mode
    ]);
    if (res.success) {
      return { content: [{ type: "text", text: `📐 Normalized sprite successfully:\n${res.output}` }] };
    } else {
      return { isError: true, content: [{ type: "text", text: `Error normalizing sprite: ${res.error}` }] };
    }
  }
);

// Tool 6: Generate Godot SpriteFrames .tres Resource
server.tool(
  "generate_sprite_frames_tres",
  {
    res_texture_path: z.string().describe("Godot texture resource path (e.g. res://assets/heroes/quan_vu/spritesheet_hd.png)"),
    output_tres_path: z.string().describe("Output filesystem path for the .tres file"),
    preset: z.enum(["hero", "strip"]).default("hero").describe("Preset type: 'hero' (24f idle/attack/skill/hurt/death) or 'strip' (single row strip)"),
    frame_size: z.number().default(64).describe("Frame width/height in pixels"),
    fps: z.number().default(16).describe("Animation playback speed in FPS (for strip preset)"),
    loop: z.boolean().default(false).describe("Whether the animation loops (for strip preset)")
  },
  async ({ res_texture_path, output_tres_path, preset, frame_size, fps, loop }) => {
    const scriptPath = path.join(PROJECT_ROOT, "tools/pixel_art/tres_generator.py");
    const args = [
      res_texture_path,
      "-o", output_tres_path,
      "-p", preset,
      "-s", frame_size.toString(),
      "-f", fps.toString()
    ];
    if (loop) args.push("-l");

    const res = await runPython(scriptPath, args);
    if (res.success) {
      return { content: [{ type: "text", text: `🎬 Generated SpriteFrames .tres successfully:\n${res.output}` }] };
    } else {
      return { isError: true, content: [{ type: "text", text: `Error generating SpriteFrames: ${res.error}` }] };
    }
  }
);

// Tool 7: List supported palettes and hero art presets
server.tool(
  "list_palettes",
  {},
  async () => {
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({
            heroes: [
              "trieu_van", "dien_vi", "truong_phi", "tao_nhan", "hoang_cai",
              "quan_vu", "lu_bo", "chu_du", "hoang_trung", "ma_sieu", "dieu_thuyen",
              "gia_cat_luong", "bang_thong", "tieu_kieu", "sai_van_co"
            ],
            standard_sizes: {
              hero_frame: "64x64",
              hero_spritesheet: "384x256 (6 cols x 4 rows = 24 frames)",
              vfx_frame: "96x96",
              vfx_strip: "576x96 (6 frames horizontal strip)",
              boss_frame: "128x128"
            },
            pipeline_tools: [
              "slice_spritesheet",
              "stitch_spritesheet",
              "normalize_sprite_size",
              "generate_sprite_frames_tres",
              "generate_hero_spritesheet",
              "generate_skill_vfx"
            ]
          }, null, 2)
        }
      ]
    };
  }
);

// Connect via standard stdio transport
const transport = new StdioServerTransport();
await server.connect(transport);
