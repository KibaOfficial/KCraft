// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using KCraft.World.Generation;
using KCraft.World;

namespace KCraft.Benchmark;

public static class BenchmarkRunner
{
  private const int WarmupRuns = 10;
  private const int SampleRuns = 100;

  public static BenchmarkStats Run(string label, Action action)
  {
    Console.Write($"  {label,-30}");

    // Warmup
    for (int i = 0; i < WarmupRuns; i++) action();

    var times = new double[SampleRuns];
    var sw = new Stopwatch();

    for (int i = 0; i < SampleRuns; i++)
    {
      sw.Restart();
      action();
      sw.Stop();
      times[i] = sw.Elapsed.TotalMilliseconds;
    }

    Array.Sort(times);

    var stats = new BenchmarkStats
    {
      MinMs = times[0],
      MaxMs = times[^1],
      AvgMs = times.Average(),
      P95Ms = times[(int)(SampleRuns * 0.95)],
      Samples = SampleRuns,
    };

    Console.WriteLine($"avg={stats.AvgMs:F2}ms  min={stats.MinMs:F2}ms  max={stats.MaxMs:F2}ms  p95={stats.P95Ms:F2}ms");
    return stats;
  }
}