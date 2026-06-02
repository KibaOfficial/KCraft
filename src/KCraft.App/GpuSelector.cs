// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using System.Runtime.InteropServices;

namespace KCraft.App;

/// <summary>
/// Hints an OS/Driver dass wir die dedizierte GPU bevorzugen.
/// Funktioniert für NVIDIA Optimus und AMD PowerXpress Laptops.
/// </summary>
public static class GpuSelector
{
  // NVIDIA Optimus — zwingt dGPU statt iGPU
  [DllImport("nvapi64.dll", EntryPoint = "NvAPI_Initialize")]
  private static extern int NvAPI_Initialize();

  // Exports die dem Treiber signalisieren: "Nimm die High-Performance GPU"
  // Diese müssen als native Exports existieren — geht in C# über eine native DLL
  // Stattdessen nutzen wir den einfacheren Weg über Registry/Environment

  public static void PreferHighPerformanceGpu()
  {
    // Setze Environment Variable die OpenTK/GLFW nutzt
    Environment.SetEnvironmentVariable("MESA_GL_VERSION_OVERRIDE", "4.6");

    // Windows GPU Preference via Registry für diesen Prozess
    // 0 = Default, 1 = Power Saving (iGPU), 2 = High Performance (dGPU)
    TrySetWindowsGpuPreference();
  }

  private static void TrySetWindowsGpuPreference()
  {
    if (!OperatingSystem.IsWindows()) return;

    try
    {
      // GPU Preference für aktuellen Prozess setzen
      var exePath = Environment.ProcessPath ?? "";
      if (string.IsNullOrEmpty(exePath)) return;

      using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
          @"SOFTWARE\Microsoft\DirectX\UserGpuPreferences", writable: true)
          ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
          @"SOFTWARE\Microsoft\DirectX\UserGpuPreferences");

      // GpuPreference=2 = High Performance
      key?.SetValue(exePath, "GpuPreference=2;");
      Console.WriteLine($"[GPU] Set high-performance preference for {Path.GetFileName(exePath)}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[GPU] Could not set GPU preference: {ex.Message}");
    }
  }

  public static void Cleanup()
  {
    if (!OperatingSystem.IsWindows()) return;
    try
    {
      var exePath = Environment.ProcessPath ?? "";
      using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
          @"SOFTWARE\Microsoft\DirectX\UserGpuPreferences", writable: true);
      key?.DeleteValue(exePath, throwOnMissingValue: false);
    }
    catch { }
  }
}