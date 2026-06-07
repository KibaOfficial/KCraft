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
[![Version](https://img.shields.io/badge/Version-v1.0.0-brightgreen?style=flat-square)]()
[![Author](https://img.shields.io/badge/Author-KibaOfficial-blueviolet?style=flat-square&logo=github)](https://github.com/KibaOfficial)

</div>

---

## What is KCraft?

KCraft is a Minecraft clone written **completely from scratch** — no Unity, no Godot, no engine abstractions. Just raw C#, OpenGL shaders, and a lot of debugging. It started as a learning project to understand 3D rendering, voxel world generation, and game architecture at a fundamental level.

---

## Screenshots

| Main Menu                                                                | In-Game + Debug Overlay                                                           |
| ------------------------------------------------------------------------ | --------------------------------------------------------------------------------- |
| ![KCraft menu](<docs/screenshots/day2/Screenshot 2026-05-31 161338.png>) | ![KCraft debug overlay](<docs/screenshots/day2/Screenshot 2026-05-31 161216.png>) |

More development screenshots live in [`docs/screenshots`](./docs/screenshots/).

---

## Features

### 🌍 World & Generation

- Chunk-based world — **16×256×16** blocks per chunk
- Spiral dynamic chunk loading / unloading around the player
- `NoiseWorldGenerator` — FastNoiseLite (OpenSimplex2, FBm, 4 octaves) terrain generation
- Procedural **oak tree generation**
- Procedural **house generation**
- World save / load system (`world.json` + chunk data + block metadata)
- `WorldTicker` — fixed **20 TPS** simulation loop (MC-accurate tick order)
- `WorldTime` — 24,000 ticks/day, SkyLight, TimeString, Day counter
- Block registry with per-block `BlockDefinition`
- DDA voxel raycasting (Amanatides & Woo) for targeted block detection
- **Biome system** — Plains, Beach, Ocean
- **Water physics simulation** — Level 0-7 (source + flowing), scheduled ticks, decay
- **Corner height interpolation** — smooth water surface like MC Java Edition

### 🎮 Gameplay

- **Player Entity** — AABB collision, gravity, jump, sprint, sneak
- **Swimming** — reduced gravity + speed in water, swim up with Space
- **Gamemodes** — Survival, Creative, Spectator
- **Fly Mode** — Creative-style free flight with proper collision
- **Edge snapping** — can't fall off edges while sneaking
- **Block Breaking** — left click, instant, chunk mesh rebuild
- **Block Placing** — right click with player collision check
- **Block Pick** — middle click copies targeted block to hotbar slot
- **Block Facing** — directional blocks (stairs, slopes) face toward the player on placement
- **Stair Collision** — dual-AABB stair geometry with correct step-up (0.5f lower half)
- **Slope Collision** — heightfield-based smooth slope traversal
- **World Selection** — choose existing saves
- **World Creation** — custom world names and seeds
- **Inventory persistence** — hotbar and inventory saved with world

### 🎨 Rendering

- Custom **GLSL shaders** (vertex + fragment)
- **Frustum Culling** — only visible chunks rendered (~80% fewer draw calls)
- **Face culling** — only visible faces generate mesh geometry
- **Cross-chunk face visibility** — water culling across chunk borders
- **Connected Textures (CTM)** — Glass uses a 16-tile spritesheet for seamless joins on all 6 faces
- **Per-face textures** — grass uses separate top/side/bottom textures
- **Face brightness** — directional lighting per face (Top 1.0, N/S 0.8, E/W 0.6, Bottom 0.5)
- Transparent block rendering (Oak Leaves, Water, Glass)
- Faithful 64x development textures
- **Biome tint** — green tint on grass via shader uniform
- **Dynamic sky renderer** — reworked with inverse view matrix for correct world-space directions
- **Sun arc** — correct east→noon→west trajectory based on WorldTime
- **Moon** — rendered opposite the sun, fades in at night
- **Stars** — ~780 geometry-based stars (fixed seed 10842 like MC), additive blending, night fade
- **Clouds** — procedural greedy-meshed cloud layer (OpenSimplex2 noise, Y=192, 3×3 tile rendering)
- **Sunset colours** — orange/pink dusk transition
- **Ambient light** — world darkens at night (~27% like MC Brightness 100)
- **Water rendering** — translucent two-pass alpha blending, corner height interpolation
- **Underwater effect** — blue fullscreen overlay when eyes are submerged
- **Spritesheet frame extraction** — animated texture support (water_still.png)
- **Grid tile extraction** — CTM spritesheet tile sampling (Texture2D)
- **Crosshair renderer** — lightweight 2D reticle
- **Block highlight renderer** — wireframe on targeted solid blocks (no highlight for fluids)
- **Block icon renderer** — real 3D mini-renderer per slot (Orthographic, correct L/wedge shape for Stairs/Slopes)
- **Bitmap font renderer** — Minecraft-style `ascii.png` atlas with proportional metrics
- **2D UI renderer** — `DrawRect` + `DrawText` for all overlays and menus

### 🧱 Blocks

- **Glass** — transparent, connected textures, Glass-to-Glass face culling
- **Cobblestone** — standard solid block
- **Gravel** — standard solid block
- **Oak Stairs** — directional L-shaped geometry, facing saved per block
- **Stone Stairs** — directional L-shaped geometry, facing saved per block
- **Oak Slope** — directional 45° wedge geometry (triangular prism), facing saved per block
- **Stone Slope** — directional 45° wedge geometry (triangular prism), facing saved per block
- **Block Metadata system** — per-block state stored in `Chunk._metadata`, persisted as `.kmeta`
- **IsFullCube** — face visibility system respects non-full-cube blocks (stairs, slopes)

### 🎒 Inventory & UI

- **Player Inventory** — 36 slots (0–8 hotbar, 9–35 main), saved with world
- **Survival Inventory Screen** — drag & drop swap, 3×9 main grid + hotbar row
- **Creative Inventory Screen** — tabbed (Blocks, Wood, Natural, Building, Inv), scrollable item grid, click-to-hotbar
- **HotbarRenderer** — driven by PlayerInventory
- **Options Screen rework** — page-based navigation (Main, Video)
- **Video Settings** — FOV slider (30°–120°), Render Distance slider (2–32)
- **Slider component** — MC-style draggable slider with live value display
- **GameSettings** — global FOV + RenderDistance, applied live

### 🖥️ UI System

- **Main Menu** — Singleplayer, Multiplayer _(disabled)_, Benchmark, Options, Quit
- **Pause Menu** — Back to Game, Options, Quit to Title
- **Options Menu** — page-based: Main → Video Settings
- **Loading Screen** — Chunk Colormap (17×17 pixel grid) + progress bar like MC Java Edition
- **World Selection Screen**
- **Create World Screen**
- **Benchmark Result Screen** — Score, Phase Stats, Hardware Info, KCraft Version
- **Text Input System** — world names and custom seeds
- `GameState` machine — MainMenu / Playing / Paused / Inventory / CreativeInventory / Options / Loading / Benchmark / BenchmarkResult
- MC-accurate button colours — Normal / Hover (yellow text) / Disabled
- `Screen` base class with `Layout` / `Draw` / `HandleClick`
- UI scale system (2×–4×)

### 🎮 Discord Integration

- **Discord Rich Presence** — custom Named Pipe IPC implementation (no SDK dependency)
- Presence updates for: Main Menu, Loading, Playing, Paused, Inventory, Creative Inventory, Options, Benchmark, Benchmark Results

### 📊 Benchmark Suite

- Multi-phase benchmark (R4 / R8 / R12, 8s each, 2s warmup + 3s chunk wait)
- Score-based recommended render distance
- Hardware information collection (CPU, GPU, RAM, OS, OpenGL)
- JSON benchmark export with KCraft version
- Automatic benchmark IDs
- In-game benchmark HUD
- Benchmark result screen with phase stats

### 🔧 Debug Overlay (F3)

- FPS + frame time, world time + day count
- XYZ, block coords, chunk coords, chunk-relative position
- Loaded chunks / visible chunks (Frustum Culling status)
- .NET version, memory, display resolution, OpenGL version, GPU
- Targeted block position + block ID
- **F3+G** — chunk border wireframe
- **F3+N** — toggle free cam / player cam
- **F3+B** — player AABB hitbox + eye-level line
- Current gamemode

### ⚡ Performance

- **Frustum Culling** — 6-plane AABB test, ~6x fewer draw calls
- **Dictionary-based O(1) Chunk Lookup**
- **Spiral Chunk Loading** — inside-out, 1 chunk per frame
- **Dirty Chunk Budget** — max 2 mesh rebuilds per frame
- **Active Water HashSet** — no full-scan for water simulation
- **RebuildNeighborsIfNeeded** — neighbor chunks only rebuilt at water borders
- **GPU Preference Selector** — registry-based for NVIDIA Optimus / AMD PowerXpress
- **WinExe conditional** — Debug builds keep console, Release builds suppress it
- **Star Geometry** — static VAO, generated once at startup (fixed seed, ~780 quads)
- **Cloud Greedy Mesh** — noise map → merged quads, 3×3 tiled around player

### 📦 Packaging

- `build.ps1` — one-command Windows + Linux build pipeline
- Windows installer via Inno Setup (`KCraft.iss`)
- Linux x64 `.tar.gz` archive
- Self-contained .NET 10 builds with all dependencies included

---

## Benchmark Results

| Hardware               | Version | Score | Avg FPS | Recommended Radius |
| ---------------------- | ------- | ----- | ------- | ------------------ |
| RTX 3060 + Ryzen 3800X | v0.3.0  | 5696  | 58      | 12                 |
| RTX 3060 + Ryzen 3800X | v0.5.0  | 4650  | 51      | 8                  |
| GTX 1660 Ti + i7-8750H | v0.5.0  | 4954  | 55      | 12                 |
| GTX 1060 + Intel i7    | v0.5.0  | 4403  | 45      | 12                 |
| RTX 3060 + Ryzen 3800X | v0.6.0  | 5986  | 65      | 12                 |
| RTX 3060 + Ryzen 3800X | v0.7.0  | 5949  | 60      | 4                  |
| RTX 3060 + Ryzen 3800X | v1.0.0  | 5824  | 60      | 4                  |

---

## Tech Stack

| Layer         | Technology                    |
| ------------- | ----------------------------- |
| Language      | C# 13 / .NET 10               |
| Windowing     | OpenTK 4.9.4 (GLFW)           |
| Graphics      | OpenGL 4.6                    |
| Image Loading | StbImageSharp                 |
| Noise         | FastNoiseLite (MIT, embedded) |
| Testing       | xUnit (50 tests, all passing) |

---

## Project Structure

```
kcraft/
├── src/
│   ├── KCraft.App/              # Entry point, GpuSelector
│   ├── KCraft.Assets/           # Texture2D (grid + frame extraction), TextureManager
│   ├── KCraft.Blocks/           # Block enum, BlockDefinition, BlockRegistry,
│   │                            # BlockFacing, BlockFacingHelper
│   ├── KCraft.Core/             # KCraftVersion, GameSettings, DiscordRpc, shared types
│   ├── KCraft.Rendering/        # KCraftWindow, Camera, ChunkMesh,
│   │   │                        # FrustumCuller, TextRenderer, DebugOverlay,
│   │   │                        # ChunkBorderRenderer, SkyRenderer, StarRenderer,
│   │   │                        # CloudRenderer, CrosshairRenderer,
│   │   │                        # BlockHighlightRenderer, BlockIconRenderer,
│   │   │                        # HotbarRenderer, HitboxRenderer, UiScale
│   │   ├── Benchmark/
│   │   │    ├── BenchmarkData
│   │   │    ├── BenchmarkSession
│   │   │    ├── FrameSample
│   │   │    └── PhaseStats
│   │   └── Ui/
│   │        ├── UiManager
│   │        ├── Screen
│   │        ├── Button
│   │        ├── Slider
│   │        ├── TextInput
│   │        ├── LoadingScreen
│   │        ├── MainMenuScreen
│   │        ├── PauseMenuScreen
│   │        ├── OptionsScreen
│   │        ├── InventoryScreen
│   │        ├── CreativeInventoryScreen
│   │        ├── SelectWorldScreen
│   │        ├── NewWorldScreen
│   │        ├── BenchmarkHudScreen
│   │        └── BenchmarkResultScreen
│   └── KCraft.World/            # Chunk (blocks + fluidLevels + metadata),
│       │                        # AABB, Entity, Player, PlayerInventory,
│       │                        # BlockRaycaster, WorldTime, WorldTicker,
│       │                        # FaceVisibility, WaterSimulator, FastNoiseLite,
│       │                        # WorldSaveManager (.kchunk + .kmeta)
│       └── Generation/          # IWorldGenerator, FlatWorldGenerator,
│                                # NoiseWorldGenerator (Biome enum)
├── docs/
│   └── screenshots/             # Day-by-day development screenshots
├── installer/                   # Generated release artifacts
├── tests/
│   └── KCraft.World.Tests/      # xUnit — 50 tests, all passing
└── assets/
    └── dev/
        ├── faithful/            # Faithful 64x textures (dev placeholders)
        │   └── FAITHFUL_LICENSE.txt
        └── font_ascii.png       # Minecraft-style bitmap font atlas
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- GPU with **OpenGL 4.1+** support
- _(Optional)_ Minecraft Java Edition JAR for full block texture set

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
.\build.ps1 -Version "1.0.0"
```

Produces `installer/KCraft-v1.0.0-Setup.exe` and `installer/KCraft-v1.0.0-linux-x64.tar.gz`.

---

## Platform Support

KCraft provides builds for **Windows x64** and **Linux x64**.

macOS and iOS builds are intentionally not planned — KCraft is a learning-focused, from-scratch engine project built around developer freedom at the full stack level.

---

## Controls

| Input          | Action                                   |
| -------------- | ---------------------------------------- |
| `W A S D`      | Move                                     |
| `Space`        | Jump / Swim up                           |
| `Left Shift`   | Sneak / Swim down                        |
| `Left Ctrl`    | Sprint                                   |
| `Left Click`   | Break block                              |
| `Right Click`  | Place block                              |
| `Middle Click` | Pick block                               |
| `Scroll / 1–9` | Select hotbar slot                       |
| `E`            | Open Inventory / Creative Inventory      |
| `Mouse`        | Look around                              |
| `Escape`       | Pause / Close inventory / release cursor |
| `F3`           | Toggle debug overlay                     |
| `F3 + G`       | Toggle chunk borders                     |
| `F3 + N`       | Toggle free cam                          |
| `F3 + B`       | Toggle hitboxes                          |
| `F3 + F4`      | Cycle gamemode                           |

---

## Roadmap

### 🌍 World

- [x] Water generation + physics
- [x] Beaches
- [x] Biomes (Plains, Beach, Ocean)
- [x] Water decay (flowing water disappears without source)
- [ ] Better structure generation
- [ ] More tree variants

### 🎮 Gameplay

- [x] Swimming physics
- [x] Underwater effect (blue overlay)
- [x] Block facing system
- [x] Stair collision matching geometry
- [x] Slope collision (heightfield)
- [x] Inventory system
- [x] Inventory persistence
- [ ] Item system
- [ ] Crafting system
- [ ] Survival progression

### 🎨 Rendering

- [x] Frustum culling
- [x] Loading screen with chunk colormap
- [x] Connected textures (CTM) for glass
- [x] Face brightness (directional lighting)
- [x] Clouds
- [x] Stars and moon
- [x] 3D block icons in inventory
- [ ] Ambient occlusion
- [ ] Smooth lighting

### 🌐 Multiplayer

- [ ] Networking layer
- [ ] P2P multiplayer
- [ ] Dedicated server support

### ⚡ Performance

- [x] Dynamic chunk loading
- [x] Frustum culling
- [x] Benchmark suite
- [x] GPU preference selector
- [x] Dirty chunk budget
- [ ] Threaded mesh generation
- [ ] Async chunk loading

---

## License

Copyright © 2026 KibaOfficial. All rights reserved.

---

## Credits & Third-Party Assets

**Textures**: [Faithful 64x](https://faithfulpack.net/) by the Faithful Resource Pack team — used as development placeholders under the [Faithful License](./assets/dev/faithful/FAITHFUL_LICENSE.txt). KCraft is non-commercial and open source. Faithful 64x is not affiliated with or endorsed by Mojang Studios.

**Noise**: [FastNoiseLite](https://github.com/Auburn/FastNoiseLite) by Auburn — MIT License.

<!-- **Code Signing**: Certificate provided by [SignPath Foundation](https://signpath.org/) —
free code signing for open source projects. -->

---

<div align="center">
  <sub>Built with ❤️ and way too many hours of OpenGL debugging</sub><br>
  <sub><a href="https://github.com/KibaOfficial">github.com/KibaOfficial</a></sub>
</div>
