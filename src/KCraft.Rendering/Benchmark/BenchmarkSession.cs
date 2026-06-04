// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using System.Diagnostics;
using KCraft.World;
using KCraft.World.Generation;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering.Benchmark;

public sealed class BenchmarkSession
{
  // ── Config ────────────────────────────────────────────────────────────
  public const float Duration = 15f; // Sekunden
  public const float CameraSpeed = 8f;
  public const int ChunkGenSamples = 50;

  // ── State ─────────────────────────────────────────────────────────────
  public bool IsRunning { get; private set; }
  public bool IsFinished { get; private set; }
  public string PhaseLabel { get; private set; } = "";
  public BenchmarkData? Result { get; private set; }
  // ── Phasen ────────────────────────────────────────────────────────────
  private static readonly (string label, int renderRadius, float duration)[] Phases =
  [
    ("Phase 1: Low Distance (R4)",    4,  8f),
    ("Phase 2: Medium Distance (R8)", 8,  8f),
    ("Phase 3: High Distance (R12)",  12, 8f),
  ];

  private int _phase = 0;
  private float _phaseElapsed = 0f;
  private float _phaseWarmup = 2f;
  private readonly List<double>[] _phaseFrameTimes = [[], [], []];

  public int CurrentRenderRadius => _phase < Phases.Length ? Phases[_phase].renderRadius : 8;

  // Camera path — fliegt einen Bogen
  private float _elapsed;
  private readonly List<double> _frameTimes = [];
  private readonly List<double> _chunkGenTimes = [];

  // Camera Movement
  public Vector3 CameraPos { get; private set; } = new(0, 80, 0);
  public float CameraYaw { get; private set; } = 0f;
  public float CameraPitch { get; private set; } = -15f;

  private readonly string _gpuRenderer;
  private readonly string _glVersion;
  private bool _waitingForChunks = true;
  private float _chunkWaitTimer = 0f;
  private const float ChunkWaitTime = 3f;

  public BenchmarkSession(string gpuRenderer, string glVersion)
  {
    _gpuRenderer = gpuRenderer;
    _glVersion = glVersion;
  }

  public void Start()
  {
    IsRunning = true;
    IsFinished = false;
    _elapsed = 0f;
    _frameTimes.Clear();
    _chunkGenTimes.Clear();
    PhaseLabel = "Warming up...";

    // Chunk Gen messen
    var gen = new NoiseWorldGenerator(seed: 42);
    var sw = new Stopwatch();
    for (int i = 0; i < ChunkGenSamples; i++)
    {
      sw.Restart();
      var c = new Chunk();
      gen.Generate(c, i % 20, i / 20);
      sw.Stop();
      _chunkGenTimes.Add(sw.Elapsed.TotalMilliseconds);
    }
  }

  public void Update(float deltaTime, int chunkCount)
  {
    if (!IsRunning || IsFinished) return;

    // Warten bis Chunks geladen sind
    if (_waitingForChunks)
    {
      _chunkWaitTimer += deltaTime;
      PhaseLabel = $"Loading chunks... ({chunkCount} loaded)";
      if (_chunkWaitTimer >= ChunkWaitTime)
        _waitingForChunks = false;
      return; // noch nicht messen
    }

    _elapsed += deltaTime;
    _phaseElapsed += deltaTime;

    // Phase wechseln
    if (_phase < Phases.Length && _phaseElapsed >= Phases[_phase].duration)
    {
      _phase++;
      _phaseElapsed = 0f;
      _phaseWarmup = 2f;
    }

    if (_phase >= Phases.Length)
    {
      Finish(chunkCount);
      return;
    }

    // Camera
    CameraYaw = _elapsed * 20f;
    CameraPos = new Vector3(
        MathF.Sin(MathHelper.DegreesToRadians(CameraYaw)) * 40f,
        70f + MathF.Sin(_elapsed * 0.5f) * 10f,
        MathF.Cos(MathHelper.DegreesToRadians(CameraYaw)) * 40f);

    PhaseLabel = _phaseElapsed < _phaseWarmup
        ? $"{Phases[_phase].label}  (warming up...)"
        : Phases[_phase].label;

    // Nur nach Warmup messen
    if (_phaseElapsed >= _phaseWarmup)
      _phaseFrameTimes[_phase].Add(deltaTime * 1000.0);
  }

  public float Progress => _elapsed / Phases.Sum(p => p.duration);

  private void Finish(int chunkCount)
  {
    IsRunning = false;
    IsFinished = true;
    PhaseLabel = "Done!";

    var phaseStats = new PhaseStats[Phases.Length];
    for (int i = 0; i < Phases.Length; i++)
    {
      var times = _phaseFrameTimes[i].OrderBy(x => x).ToList();
      if (times.Count == 0)
      {
        // Fallback damit kein null in phaseStats landet
        phaseStats[i] = new PhaseStats
        {
          RenderRadius = Phases[i].renderRadius,
          AvgFps = 0,
          P1LowFps = 0,
        };
        continue;
      }
      double avg = times.Average();
      phaseStats[i] = new PhaseStats
      {
        RenderRadius = Phases[i].renderRadius,
        AvgFps = 1000.0 / avg,
        P1LowFps = 1000.0 / times[^Math.Max(1, (int)(times.Count * 0.01))],
      };
    }

    // Recommended — höchster Radius wo 1% Low > 50 FPS
    var best = phaseStats
      .Where(ps => ps.RenderRadius > 0)
      .OrderByDescending(ps => ps.AvgFps * 0.7 + ps.P1LowFps * 0.3)
      .FirstOrDefault();
    int recommended = best?.RenderRadius ?? 8;

    // Alle Frame Times zusammen für Gesamt-Score
    var allTimes = _phaseFrameTimes.SelectMany(x => x).OrderBy(x => x).ToList();
    double avgMs = allTimes.Average();
    double avgFps = 1000.0 / avgMs;
    int p1Idx = Math.Max(1, (int)(allTimes.Count * 0.01));
    double p1Low = 1000.0 / allTimes[^p1Idx];
    int score = (int)(avgFps * 80 + p1Low * 20);

    Result = new BenchmarkData
    {
      Id = $"BENCH-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
      KCraftVersion = Core.KCraftVersion.Version,
      Timestamp = DateTime.Now,
      Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown",
      CpuCores = Environment.ProcessorCount,
      RamMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024,
      Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
      GpuRenderer = _gpuRenderer,
      GlVersion = _glVersion,
      AvgFps = avgFps,
      MinFps = 1000.0 / allTimes.Last(),
      MaxFps = 1000.0 / allTimes.First(),
      AvgFrameMs = avgMs,
      P1LowFps = p1Low,
      P01LowFps = 1000.0 / allTimes[^Math.Max(1, (int)(allTimes.Count * 0.001))],
      ChunkGenAvgMs = _chunkGenTimes.Count > 0 ? _chunkGenTimes.Average() : 0,
      TotalChunks = chunkCount,
      Score = score,
      PhaseStats = phaseStats,
      RecommendedRadius = recommended,
    };

    ExportJson();
  }

  private void ExportJson()
  {
    if (Result == null) return;
    var dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KCraft", "benchmarks");
    Directory.CreateDirectory(dir);
    var path = Path.Combine(dir, $"{Result.Id}.json");
    var json = System.Text.Json.JsonSerializer.Serialize(Result,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
    Console.WriteLine($"[Benchmark] Saved: {path}");
  }
}