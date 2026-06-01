// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Benchmark;

public static class HardwareCollector
{
  public static HardwareInfo Collect()
  {
    return new HardwareInfo
    {
      Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
        ?? System.Runtime.InteropServices.RuntimeInformation.OSDescription,
      CpuCores = Environment.ProcessorCount,
      RamMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024,
      Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
      Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    };
  }
}