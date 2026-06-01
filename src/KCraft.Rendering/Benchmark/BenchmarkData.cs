// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Rendering.Benchmark;

public sealed class FrameSample
{
  public double FrameTimeMs { get; set; }
  public int ChunkCount { get; set; }
}

public sealed class PhaseStats
{
  public int RenderRadius { get; set; }
  public double AvgFps { get; set; }
  public double P1LowFps { get; set; }
}

public sealed class BenchmarkData
{
  public string Id { get; set; } = "";
  public DateTime Timestamp { get; set; }
  public string Cpu { get; set; } = "";
  public int CpuCores { get; set; }
  public long RamMb { get; set; }
  public string Os { get; set; } = "";
  public string GpuRenderer { get; set; } = "";
  public string GlVersion { get; set; } = "";

  // Frame Metrics
  public double AvgFps { get; set; }
  public double MinFps { get; set; }
  public double MaxFps { get; set; }
  public double AvgFrameMs { get; set; }
  public double P1LowFps { get; set; } // 1% low
  public double P01LowFps { get; set; } // 0.1% low

  // Chunk Metrics
  public double ChunkGenAvgMs { get; set; }
  public int TotalChunks { get; set; }

  // Score
  public int Score { get; set; }

  // Per-Phase Stats
  public PhaseStats[] PhaseStats { get; set; } = [];
  public int RecommendedRadius { get; set; } = 8;
}