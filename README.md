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
[![Version](https://img.shields.io/badge/Version-v0.5.0-orange?style=flat-square)]()
[![Author](https://img.shields.io/badge/Author-KibaOfficial-blueviolet?style=flat-square&logo=github)](https://github.com/KibaOfficial)

</div>

---

## What is KCraft?

KCraft is a Minecraft clone written **completely from scratch** — no Unity, no Godot, no engine abstractions. Just raw C#, OpenGL shaders, and a lot of debugging. It started as a learning project to understand 3D rendering, voxel world generation, and game architecture at a fundamental level.

---

## Screenshots

| Main Menu | In-Game + Debug Overlay |
| --------- | ----------------------- |
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
- World save / load system (`world.json` + chunk data)
- `WorldTicker` — fixed **20 TPS** simulation loop (MC-accurate tick order)
- `WorldTime` — 24,000 ticks/day, SkyLight, TimeString, Day counter
- Block registry with per-block `BlockDefinition`
- DDA voxel raycasting (Amanatides & Woo) for targeted block detection
- **Biome system** — Plains, Beach, Ocean
- **Water physics simulation** — Level 0-7 (source + flowing), scheduled ticks, decay
- **Corner height interpolation** — fließendes Wasser mit schräger Oberfläche wie MC

### 🎮 Gameplay

- **Player Entity** — AABB collision, gravity, jump, sprint, sneak
- **Swimming** — reduzierte Gravity + Speed im Wasser, Auftrieb mit Space
- **Gamemodes** — Survival, Creative, Spectator
- **Fly Mode** — Creative-style free flight
- **Edge snapping** — can't fall off edges while sneaking
- **Block Breaking** — left click, instant, chunk mesh rebuild
- **Block Placing** — right click, player collision check
- **Block Pick** — middle click copies targeted block to hotbar slot
- **World Selection** — choose existing saves
- **World Creation** — custom world names and seeds

### 🎨 Rendering

- Custom **GLSL shaders** (vertex + fragment)
- **Frustum Culling** — nur sichtbare Chunks werden gerendert (~80% weniger Draw Calls)
- **Face culling** — only visible faces generate mesh geometry
- **Cross-chunk face visibility** — Wasser-Culling über Chunk-Grenzen
- **Per-face textures** — grass uses separate top/side/bottom textures
- Transparent block rendering (Oak Leaves, Water)
- Faithful 64x development textures
- **Biome tint** — green tint on grass via shader uniform
- **Dynamic sky renderer** — day/night gradient, correct sun arc (east→west)
- **Sunset colours** — orange/pink dusk transition
- **Ambient light** — world darkens at night (~27% like MC Brightness 100)
- **Water rendering** — translucent two-pass alpha blending, corner height interpolation
- **Spritesheet frame extraction** — animated texture support (water_still.png)
- **Crosshair renderer** — lightweight 2D reticle
- **Block highlight renderer** — wireframe outline on targeted block
- **Block icon renderer** — isometric 2D block icons in hotbar (MC face brightness)
- **Bitmap font renderer** — Minecraft-style `ascii.png` atlas with proportional metrics
- **2D UI renderer** — `DrawRect` + `DrawText` for all overlays and menus

### 🖥️ UI System

- **Main Menu** — Singleplayer, Multiplayer _(disabled)_, Benchmark, Options, Quit
- **Pause Menu** — Back to Game, Options, Quit to Title
- **Options Menu** — GUI Scale 1×–4×
- **Loading Screen** — Chunk Colormap (17×17 Pixel-Grid) + Progress Bar wie MC Java Edition
- **World Selection Screen**
- **Create World Screen**
- **Benchmark Result Screen** — Score, Phase Stats, Hardware Info, KCraft Version
- **Text Input System** — world names and custom seeds
- `GameState` machine — MainMenu / Playing / Paused / Options / Loading / Benchmark / BenchmarkResult
- MC-accurate button colours — Normal / Hover (yellow text) / Disabled
- `Screen` base class with `Layout` / `Draw` / `HandleClick`

### 📊 Benchmark Suite

- Multi-phase benchmark (R4 / R8 / R12, 8s each, 2s warmup + 3s chunk wait)
- Score-based recommended render distance
- Hardware information collection (CPU, GPU, RAM, OS, OpenGL)
- JSON benchmark export mit KCraft Version
- Automatic benchmark IDs
- In-game benchmark HUD
- Benchmark result screen mit Phase Stats
- Performance comparison across hardware

### 🔧 Debug Overlay (F3)

- FPS + frame time, world time + day count
- XYZ, block coords, chunk coords, chunk-relative position
- Loaded chunks / visible chunks (Frustum Culling Status)
- .NET version, memory, display resolution, OpenGL version, GPU
- Targeted block position + block ID
- **F3+G** — chunk border wireframe
- **F3+N** — toggle free cam / player cam
- **F3+B** — player AABB hitbox + eye-level line
- Current gamemode
- Dynamic chunk statistics

### ⚡ Performance

- **Frustum Culling** — 6-plane AABB test, ~6x weniger Draw Calls
- **Dictionary-based O(1) Chunk Lookup**
- **Spiral Chunk Loading** — von innen nach außen, 1 Chunk pro Frame
- **Dirty Chunk Budget** — max 2 Mesh-Rebuilds pro Frame
- **Active Water HashSet** — kein Vollscan für Water Simulation
- **RebuildNeighborsIfNeeded** — Nachbar-Chunks nur bei Wasser-Grenze neu bauen
- **GPU Preference Selector** — Registry-basiert für NVIDIA Optimus / AMD PowerXpress

### 📦 Packaging

- `build.ps1` — one-command Windows + Linux build pipeline
- Windows installer via Inno Setup (`KCraft.iss`)
- Linux x64 `.tar.gz` archive
- Self-contained .NET 10 builds with all dependencies included

---

## Benchmark Results

| Hardware | Version | Score | Avg FPS | Recommended Radius |
| -------- | ------- | ----- | ------- | ------------------ |
| RTX 3060 + Ryzen 3800X | v0.3.0 | 5696 | 58 | 12 |
| RTX 3060 + Ryzen 3800X | v0.5.0 | 4650 | 51 | 8 |
| GTX 1660 Ti + i7-8750H | v0.5.0 | 4954 | 55 | 12 |
| GTX 1060 + Intel i7 | v0.5.0 | 4403 | 45 | 12 |

*v0.5.0 Score-Differenz zu v0.3.0 durch Wasser-Rendering + Physik-Simulation.*

---

## Tech Stack

| Layer | Technology |
| ----- | ---------- |
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
│ ├── KCraft.App/ # Entry point, GpuSelector
│ ├── KCraft.Assets/ # Texture2D, TextureManager
│ ├── KCraft.Blocks/ # Block enum, BlockDefinition, BlockRegistry
│ ├── KCraft.Core/ # KCraftVersion, shared types
│ ├── KCraft.Rendering/ # KCraftWindow, Camera, ChunkMesh,
│ │ │ # FrustumCuller, TextRenderer, DebugOverlay,
│ │ │ # ChunkBorderRenderer, SkyRenderer,
│ │ │ # CrosshairRenderer, BlockHighlightRenderer,
│ │ │ # BlockIconRenderer, HotbarRenderer,
│ │ │ # HitboxRenderer, WorldManager, UiScale
│ │ ├── Benchmark/
│ │ │ ├── BenchmarkData
│ │ │ ├── BenchmarkSession
│ │ │ ├── FrameSample
│ │ │ └── PhaseStats
│ │ └── Ui/
│ │ ├── UiManager
│ │ ├── Screen
│ │ ├── Button
│ │ ├── TextInput
│ │ ├── LoadingScreen
│ │ ├── MainMenuScreen
│ │ ├── PauseMenuScreen
│ │ ├── OptionsScreen
│ │ ├── SelectWorldScreen
│ │ ├── NewWorldScreen
│ │ ├── BenchmarkHudScreen
│ │ └── BenchmarkResultScreen
│ └── KCraft.World/ # Chunk, ChunkMath, ChunkPosition,
│ │ # AABB, Entity, Player,
│ │ # BlockRaycaster, WorldTime, WorldTicker,
│ │ # FaceVisibility, WaterSimulator, FastNoiseLite
│ └── Generation/ # IWorldGenerator, FlatWorldGenerator,
│ # NoiseWorldGenerator (Biome enum)
├── docs/
│ └── screenshots/ # Day-by-day development screenshots
├── installer/ # Generated release artifacts
├── tests/
│ └── KCraft.World.Tests/ # xUnit — 50 tests, all passing
└── assets/
└── dev/
├── faithful/ # Faithful 64x textures (dev placeholders)
│ └── FAITHFUL_LICENSE.txt
└── font_ascii.png # Minecraft-style bitmap font atlas

````

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
````

### Test

```bash
dotnet test
```

### Package

```bash
.\build.ps1 -Version "0.5.0"
```

Produces `installer/KCraft-v0.5.0-Setup.exe` and `installer/KCraft-v0.5.0-linux-x64.tar.gz`.

---

## Platform Support

KCraft provides builds for **Windows x64** and **Linux x64**.

macOS and iOS builds are intentionally not planned — KCraft is a learning-focused, from-scratch engine project built around developer freedom at the full stack level.

---

## Controls

| Input          | Action                 |
| -------------- | ---------------------- |
| `W A S D`      | Move                   |
| `Space`        | Jump / Swim up         |
| `Left Shift`   | Sneak / Swim down      |
| `Left Ctrl`    | Sprint                 |
| `Left Click`   | Break block            |
| `Right Click`  | Place block            |
| `Middle Click` | Pick block             |
| `Scroll / 1–9` | Select hotbar slot     |
| `Mouse`        | Look around            |
| `Escape`       | Pause / release cursor |
| `F3`           | Toggle debug overlay   |
| `F3 + G`       | Toggle chunk borders   |
| `F3 + N`       | Toggle free cam        |
| `F3 + B`       | Toggle hitboxes        |

---

## Roadmap

### 🌍 World

- [x] Water generation + physics
- [x] Beaches
- [x] Biomes (Plains, Beach, Ocean)
- [ ] Water decay (flowing water disappears without source)
- [ ] Better structure generation
- [ ] More tree variants

### 🎮 Gameplay

- [x] Swimming physics
- [ ] Underwater effect (blue overlay)
- [ ] Inventory system
- [ ] Item system
- [ ] Crafting system
- [ ] Survival progression

### 🎨 Rendering

- [x] Frustum culling
- [x] Loading screen with chunk colormap
- [ ] Ambient occlusion
- [ ] Clouds
- [ ] Stars and moon

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

---

<div align="center">
  <sub>Built with ❤️ and way too many hours of OpenGL debugging</sub><br>
  <sub><a href="https://github.com/KibaOfficial">github.com/KibaOfficial</a></sub>
</div>
```
