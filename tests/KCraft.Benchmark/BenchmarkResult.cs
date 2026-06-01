// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Benchmark;

public sealed class BenchmarkStats
{
  public double MinMs { get; set; }
  public double MaxMs { get; set; }
  public double AvgMs { get; set; }
  public double P95Ms { get; set; }
  public int Samples { get; set; }
}

public sealed class HardwareInfo
{
  public string Cpu { get; set; } = "";
  public int CpuCores { get; set; }
  public long RamMb { get; set; }
  public string Os { get; set; } = "";
  public string Runtime { get; set; } = "";
}

public sealed class BenchmarkResult
{
  public string Id { get; set; } = "";
  public DateTime Timestamp { get; set; }
  public HardwareInfo Hardware { get; set; } = new();
  public BenchmarkStats ChunkGen { get; set; } = new();
  public BenchmarkStats MeshBuild { get; set; } = new();
  public BenchmarkStats ChunkGenMT { get; set; } = new();
}