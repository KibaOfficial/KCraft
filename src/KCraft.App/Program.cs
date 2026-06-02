// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.App;
using KCraft.Rendering;

// GPU Preference setzen BEVOR OpenGL/OpenTK initialisiert wird
GpuSelector.PreferHighPerformanceGpu();

using var window = new KCraftWindow();
window.Run();

// Registry aufräumen
GpuSelector.Cleanup();