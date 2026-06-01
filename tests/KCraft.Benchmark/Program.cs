// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using System.Text.Json;
using KCraft.World;
using KCraft.World.Generation;
using KCraft.Benchmark;

Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║         KCraft Benchmark Suite           ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.WriteLine();

// Hardware
Console.WriteLine("── Hardware ─────────────────────────────────");
var hw = HardwareCollector.Collect();
Console.WriteLine($"  CPU:     {hw.Cpu}");
Console.WriteLine($"  Cores:   {hw.CpuCores}");
Console.WriteLine($"  RAM:     {hw.RamMb} MB");
Console.WriteLine($"  OS:      {hw.Os}");
Console.WriteLine($"  Runtime: {hw.Runtime}");
Console.WriteLine();

// Benchmarks
Console.WriteLine("── Benchmarks ───────────────────────────────");

var generator = new NoiseWorldGenerator(seed: 42);
var chunk = new Chunk();

// Chunk Generation
var chunkGen = BenchmarkRunner.Run("Chunk Generation (single)", () =>
{
  var c = new Chunk();
  generator.Generate(c, 0, 0);
});

// Mesh Build (wir können kein OpenGL aufrufen, nur BuildData)
// Chunk vorbereiten
generator.Generate(chunk, 0, 0);

// Multi-threaded Chunk Gen
var tasks = new List<Task<double>>();
var sw2 = System.Diagnostics.Stopwatch.StartNew();
int mtCount = 100;

var chunkGenMT = BenchmarkRunner.Run("Chunk Generation (parallel)", () =>
{
  var chunks = new Chunk[4];
  var t = System.Threading.Tasks.Parallel.For(0, 4, i =>
  {
    chunks[i] = new Chunk();
    generator.Generate(chunks[i], i, i);
  });
});

Console.WriteLine();

// Result zusammenbauen
var id = $"BENCH-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

var result = new BenchmarkResult
{
  Id = id,
  Timestamp = DateTime.Now,
  Hardware = hw,
  ChunkGen = chunkGen,
  ChunkGenMT = chunkGenMT,
};

// JSON Export
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
var outDir = Path.Combine(AppContext.BaseDirectory, "benchmarks");
Directory.CreateDirectory(outDir);
var outPath = Path.Combine(outDir, $"{id}.json");
File.WriteAllText(outPath, json);

Console.WriteLine("── Result ───────────────────────────────────");
Console.WriteLine($"  ID:     {id}");
Console.WriteLine($"  Saved:  {outPath}");
Console.WriteLine();
Console.WriteLine(json);