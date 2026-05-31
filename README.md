<div align="center">

```
 _  ______            __ _
| |/ / ___|_ __ __ _ / _| |_
| ' / |   | '__/ _` | |_| __|
| . \ |___| | | (_| |  _| |_
|_|\_\____|_|  \__,_|_|  \__|
```

### A Minecraft-inspired voxel engine — built from scratch in C# and OpenGL.

[![License](https://img.shields.io/badge/License-Proprietary-red?style=flat-square)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![OpenGL](https://img.shields.io/badge/OpenGL-4.6-5586A4?style=flat-square&logo=opengl&logoColor=white)](https://www.opengl.org/)
[![OpenTK](https://img.shields.io/badge/OpenTK-4.9.4-1E88E5?style=flat-square)](https://opentk.net/)
[![Tests](https://img.shields.io/badge/Tests-50%20passing-brightgreen?style=flat-square)](./tests/)
[![Version](https://img.shields.io/badge/Version-v0.2.0-orange?style=flat-square)]()
[![Author](https://img.shields.io/badge/Author-KibaOfficial-blueviolet?style=flat-square&logo=github)](https://github.com/KibaOfficial)

</div>

---

## What is KCraft?

KCraft is a Minecraft clone written **completely from scratch** — no Unity, no Godot, no engine abstractions. Just raw C#, OpenGL shaders, and a lot of debugging. It started as a learning project to understand 3D rendering, voxel world generation, and game architecture at a fundamental level.

---

## Screenshots

| Main Menu / UI | Debug Overlay / Targeting |
|---|---|
| ![KCraft menu](<docs/screenshots/day2/Screenshot 2026-05-31 161338.png>) | ![KCraft debug overlay](<docs/screenshots/day2/Screenshot 2026-05-31 161216.png>) |

More development screenshots live in [`docs/screenshots`](./docs/screenshots/).

---

## Features

### 🌍 World & Generation
- Chunk-based world — **16×256×16** blocks per chunk
- **289 chunks** rendered simultaneously (render radius 8)
- `FlatWorldGenerator` — stone → dirt → grass layering
- `NoiseWorldGenerator` — FastNoiseLite (OpenSimplex2, FBm, 4 octaves) for realistic terrain
- `WorldTicker` — fixed 20 TPS simulation loop
- `WorldTime` — Minecraft-style day time, day count, sky light, and clock string
- Block registry with per-block `BlockDefinition`
- DDA voxel raycasting for targeted block detection

### 🎨 Rendering
- Custom **GLSL shaders** (vertex + fragment)
- **Face culling** — only visible faces generate mesh geometry
- **Per-face textures** — grass uses separate top/side/bottom textures
- **Biome tint** — green tint on grass via shader uniform
- **Dynamic sky renderer** — day/night gradient plus sun/moon direction from world time
- **Crosshair renderer** — lightweight 2D reticle drawn over the world
- **Block highlight renderer** — outlines the currently targeted block
- **Bitmap font renderer** — Minecraft-style `ascii.png` atlas
- **2D UI renderer** — `DrawRect` + `DrawText` for overlays and menus

### 🖥️ UI System
- **Main Menu** — Singleplayer, Multiplayer *(disabled)*, Options *(disabled)*, Quit
- **Pause Menu** — Back to Game, Options *(disabled)*, Quit to Title
- `Button` — hover/disabled states, border, centered text
- `GameState` machine — `MainMenu` / `Playing` / `Paused`
- `Screen` base class with `Layout` / `Draw` / `HandleClick`

### 🔧 Debug Overlay (F3)
- FPS + frame time
- World time + day count
- XYZ position, block coords, chunk coords, chunk-relative position
- Loaded chunks, facing direction (N/S/E/W + yaw/pitch)
- .NET version, memory usage, display resolution
- OpenGL version + GPU name
- Targeted block position + block ID
- Targeted fluid *(placeholder)*

### 🟨 Chunk Borders (F3+G)
- Yellow 3D line box around your current chunk
- 4 vertical corner lines + top/bottom horizontal edges
- Follows the player dynamically

### 📦 Packaging
- Windows installer script via Inno Setup (`KCraft.iss`)
- Linux x64 archive artifact under `installer/`
- Assets are bundled into the installed/published app directory

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# 13 / .NET 10 |
| Windowing | OpenTK 4.9.4 (GLFW) |
| Graphics | OpenGL 4.6 |
| Image Loading | StbImageSharp |
| Noise | FastNoiseLite (MIT, embedded) |
| Testing | xUnit (50 tests, all passing) |

---

## Project Structure

```
kcraft/
├── src/
│   ├── KCraft.App/              # Entry point
│   ├── KCraft.Assets/           # Texture2D, TextureManager
│   ├── KCraft.Blocks/           # Block enum, BlockDefinition, BlockRegistry
│   ├── KCraft.Core/             # Shared types
│   ├── KCraft.Rendering/        # KCraftWindow, Camera, ChunkMesh,
│   │   │                        # TextRenderer, DebugOverlay,
│   │   │                        # ChunkBorderRenderer, SkyRenderer,
│   │   │                        # CrosshairRenderer,
│   │   │                        # BlockHighlightRenderer
│   │   └── Ui/                  # UiManager, Screen, Button,
│   │                            # MainMenuScreen, PauseMenuScreen
│   └── KCraft.World/            # Chunk, ChunkMath, ChunkPosition,
│       │                        # FaceDirection, FaceVisibility,
│       │                        # BlockRaycaster, WorldTime,
│       │                        # WorldTicker, FastNoiseLite
│       └── Generation/          # IWorldGenerator, FlatWorldGenerator,
│                                # NoiseWorldGenerator
├── docs/
│   └── screenshots/             # Day-by-day development screenshots
├── installer/                   # Generated release artifacts
├── tests/
│   └── KCraft.World.Tests/      # xUnit — 50 tests, all passing
└── assets/
    └── dev/                     # grass_block_top/side, dirt, stone,
                                 # font_ascii.png
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- GPU with **OpenGL 4.1+** support
- *(Optional)* Minecraft Java Edition JAR for full block texture set

### Run

```bash
git clone https://github.com/KibaOfficial/KCraft.git
cd kcraft
dotnet run --project src/KCraft.App
```

### Test

```bash
dotnet test
```

### Package

Windows publish output is expected under `publish/win-x64` before compiling the Inno Setup script:

```bash
dotnet publish src/KCraft.App -c Release -r win-x64 --self-contained true -o publish/win-x64
```

Then compile [`KCraft.iss`](./KCraft.iss) with Inno Setup to create the Windows installer in `installer/`.

## Platform Support

KCraft currently provides builds for:

- Windows x64
- Linux x64

macOS and iOS builds are intentionally not planned.

This is a deliberate platform policy. I do not want to support platforms whose vendor direction increasingly restricts developer freedom, especially around low-level experimentation, runtime code generation, JIT access, and open development workflows.

KCraft is a learning-focused, from-scratch engine project. It exists because I enjoy understanding and controlling the full stack — from world generation to rendering. Platforms that move further away from that spirit are not a target for this project.

---

## Controls

| Input | Action |
|---|---|
| `W A S D` | Move |
| `Space` | Move up |
| `Left Shift` | Move down |
| `Mouse` | Look around |
| `Escape` | Pause / release cursor |
| `F3` | Toggle debug overlay |
| `F3 + G` | Toggle chunk borders |
| Look at block | Target block raycast + highlight |

---

## Roadmap

- [ ] Threaded chunk loading / unloading
- [ ] Frustum culling
- [ ] Block placement & breaking
- [ ] Inventory system
- [x] Sky rendering (sun/moon direction + day/night gradient)
- [ ] Stars and richer celestial rendering
- [ ] Ambient occlusion
- [ ] Save / load world
- [ ] Multiplayer *(someday 👀)*

---

## License

Copyright © 2026 KibaOfficial. All rights reserved.

---

<div align="center">
  <sub>Built with ❤️ and way too many hours of OpenGL debugging</sub><br>
  <sub><a href="https://github.com/KibaOfficial">github.com/KibaOfficial</a></sub>
</div>
