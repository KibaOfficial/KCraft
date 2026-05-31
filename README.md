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

| Main Menu | In-Game + Debug Overlay |
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
- `WorldTicker` — fixed **20 TPS** simulation loop (MC-accurate tick order)
- `WorldTime` — 24,000 ticks/day, SkyLight, TimeString, Day counter
- Block registry with per-block `BlockDefinition`
- DDA voxel raycasting (Amanatides & Woo) for targeted block detection

### 🎮 Gameplay
- **Player Entity** — AABB collision, gravity, jump, sprint, sneak
- **Edge snapping** — can't fall off edges while sneaking
- **Block Breaking** — left click, instant, chunk mesh rebuild
- **Block Placing** — right click, player collision check
- **Block Pick** — middle click copies targeted block to hotbar slot

### 🎨 Rendering
- Custom **GLSL shaders** (vertex + fragment)
- **Face culling** — only visible faces generate mesh geometry
- **Per-face textures** — grass uses separate top/side/bottom textures
- **Biome tint** — green tint on grass via shader uniform
- **Dynamic sky renderer** — day/night gradient, correct sun arc (east→west)
- **Sunset colours** — orange/pink dusk transition
- **Ambient light** — world darkens at night (~27% like MC Brightness 100)
- **Crosshair renderer** — lightweight 2D reticle
- **Block highlight renderer** — wireframe outline on targeted block
- **Block icon renderer** — isometric 2D block icons in hotbar (MC face brightness)
- **Bitmap font renderer** — Minecraft-style `ascii.png` atlas with proportional metrics
- **2D UI renderer** — `DrawRect` + `DrawText` for all overlays and menus

### 🖥️ UI System
- **Main Menu** — Singleplayer, Multiplayer *(disabled)*, Options, Quit
- **Pause Menu** — Back to Game, Options, Quit to Title
- **Options Menu** — GUI Scale 1×–4×
- `GameState` machine — `MainMenu` / `Playing` / `Paused` / `Options`
- MC-accurate button colours — Normal / Hover (yellow text) / Disabled
- `Screen` base class with `Layout` / `Draw` / `HandleClick`

### 🔧 Debug Overlay (F3)
- FPS + frame time, world time + day count
- XYZ, block coords, chunk coords, chunk-relative position
- Loaded chunks, facing direction (N/S/E/W + yaw/pitch)
- .NET version, memory, display resolution, OpenGL version, GPU
- Targeted block position + block ID
- **F3+G** — chunk border wireframe
- **F3+N** — toggle free cam / player cam
- **F3+B** — player AABB hitbox + eye-level line

### 📦 Packaging
- `build-release.ps1` — one-command Windows + Linux build with fancy output
- Windows installer via Inno Setup (`KCraft.iss`) with dynamic version injection
- Linux x64 `.tar.gz` archive

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
│   │   │                        # CrosshairRenderer, BlockHighlightRenderer,
│   │   │                        # BlockIconRenderer, HotbarRenderer,
│   │   │                        # HitboxRenderer, WorldManager, UiScale
│   │   └── Ui/                  # UiManager, Screen, Button,
│   │                            # MainMenuScreen, PauseMenuScreen, OptionsScreen
│   └── KCraft.World/            # Chunk, ChunkMath, ChunkPosition,
│       │                        # AABB, Entity, Player,
│       │                        # BlockRaycaster, WorldTime, WorldTicker,
│       │                        # FastNoiseLite
│       └── Generation/          # IWorldGenerator, FlatWorldGenerator,
│                                # NoiseWorldGenerator
├── docs/
│   └── screenshots/             # Day-by-day development screenshots
├── installer/                   # Generated release artifacts
├── tests/
│   └── KCraft.World.Tests/      # xUnit — 50 tests, all passing
└── assets/
    └── dev/                     # grass_block_top/side, dirt, stone, font_ascii.png
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
dotnet watch run --project src/KCraft.App
```

### Test

```bash
dotnet test
```

### Package

```bash
.\build-release.ps1 -Version "0.2.0"
```

Produces `installer/KCraft-v0.2.0-Setup.exe` and `installer/KCraft-v0.2.0-linux-x64.tar.gz`.

---

## Platform Support

KCraft provides builds for **Windows x64** and **Linux x64**.

macOS and iOS builds are intentionally not planned — KCraft is a learning-focused, from-scratch engine project built around developer freedom at the full stack level.

---

## Controls

| Input | Action |
|---|---|
| `W A S D` | Move |
| `Space` | Jump |
| `Left Shift` | Sneak |
| `Left Ctrl` | Sprint |
| `Left Click` | Break block |
| `Right Click` | Place block |
| `Middle Click` | Pick block |
| `Scroll / 1–9` | Select hotbar slot |
| `Mouse` | Look around |
| `Escape` | Pause / release cursor |
| `F3` | Toggle debug overlay |
| `F3 + G` | Toggle chunk borders |
| `F3 + N` | Toggle free cam |
| `F3 + B` | Toggle hitboxes |

---

## Roadmap

- [ ] Threaded chunk loading / unloading
- [ ] Frustum culling
- [ ] Inventory system
- [ ] Stars and richer celestial rendering
- [ ] Ambient occlusion
- [ ] Save / load world
- [ ] Multiplayer *(someday 👀)*
- [x] Sky rendering — sun arc, day/night, sunset colours
- [x] Ambient light — night darkness
- [x] Player entity — physics, gravity, jump, sprint, sneak
- [x] Block breaking + placing + pick
- [x] Hotbar with isometric block icons
- [x] Options menu — GUI Scale

---

## License

Copyright © 2026 KibaOfficial. All rights reserved.

---

<div align="center">
  <sub>Built with ❤️ and way too many hours of OpenGL debugging</sub><br>
  <sub><a href="https://github.com/KibaOfficial">github.com/KibaOfficial</a></sub>
</div>