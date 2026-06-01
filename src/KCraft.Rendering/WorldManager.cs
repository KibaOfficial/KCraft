// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using KCraft.World;
using KCraft.World.Generation;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class WorldManager : IDisposable
{
  // ── Config ────────────────────────────────────────────────────────────
  public const int RenderRadius = 8;
  public const int LoadRadius = 10;
  public const int UnloadRadius = 13;
  public int Seed { get; private set; } = 42;

  // ── Data ──────────────────────────────────────────────────────────────
  public List<(ChunkMesh mesh, Chunk chunk, Vector3i chunkPos)> ChunkMeshes { get; } = [];
  public int ChunkCount => ChunkMeshes.Count;

  private readonly NoiseWorldGenerator _generator;

  // ── Init ──────────────────────────────────────────────────────────────
  public WorldManager(int seed = 42)
  {
    Seed = seed;
    _generator = new NoiseWorldGenerator(seed: seed);

    // Initial load um Spawn (0,0)
    for (int cx = -RenderRadius; cx <= RenderRadius; cx++)
      for (int cz = -RenderRadius; cz <= RenderRadius; cz++)
        LoadChunk(cx, cz);
  }

  // ── Dynamic Chunk Loading ─────────────────────────────────────────────
  public void UpdateChunks(Vector3 playerPos)
  {
    int pcx = (int)MathF.Floor(playerPos.X / Chunk.Width);
    int pcz = (int)MathF.Floor(playerPos.Z / Chunk.Depth);

    // Unload — Chunks die zu weit weg sind
    for (int i = ChunkMeshes.Count - 1; i >= 0; i--)
    {
      var (mesh, chunk, pos) = ChunkMeshes[i];
      int dx = Math.Abs(pos.X - pcx);
      int dz = Math.Abs(pos.Z - pcz);
      if (dx > UnloadRadius || dz > UnloadRadius)
      {
        mesh.Dispose();
        ChunkMeshes.RemoveAt(i);
      }
    }

    // Load — neue Chunks im Load Radius
    for (int cx = pcx - LoadRadius; cx <= pcx + LoadRadius; cx++)
      for (int cz = pcz - LoadRadius; cz <= pcz + LoadRadius; cz++)
      {
        int dx = Math.Abs(cx - pcx);
        int dz = Math.Abs(cz - pcz);
        if (dx > LoadRadius || dz > LoadRadius) continue;
        if (IsLoaded(cx, cz)) continue;
        LoadChunk(cx, cz);
      }
  }

  private bool IsLoaded(int cx, int cz)
  {
    foreach (var (_, _, pos) in ChunkMeshes)
      if (pos.X == cx && pos.Z == cz) return true;
    return false;
  }

  private void LoadChunk(int cx, int cz)
  {
    var chunk = new Chunk();
    _generator.Generate(chunk, cx, cz);
    var mesh = new ChunkMesh();
    mesh.Build(chunk);
    ChunkMeshes.Add((mesh, chunk, new Vector3i(cx, 0, cz)));
  }

  // ── Block Access ──────────────────────────────────────────────────────
  public Block? GetBlock(int wx, int wy, int wz)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);

    foreach (var (_, chunk, chunkPos) in ChunkMeshes)
    {
      if (chunkPos.X != cx || chunkPos.Z != cz) continue;
      int lx = wx - cx * Chunk.Width;
      int ly = wy;
      int lz = wz - cz * Chunk.Depth;
      if (!chunk.IsInside(lx, ly, lz)) return null;
      var block = chunk.GetBlock(lx, ly, lz);
      return block == Block.Air ? null : block;
    }
    return null;
  }

  public bool BreakBlock(Vector3i worldPos)
  {
    int cx = (int)MathF.Floor(worldPos.X / (float)Chunk.Width);
    int cz = (int)MathF.Floor(worldPos.Z / (float)Chunk.Depth);

    for (int i = 0; i < ChunkMeshes.Count; i++)
    {
      var (mesh, chunk, chunkPos) = ChunkMeshes[i];
      if (chunkPos.X != cx || chunkPos.Z != cz) continue;
      int lx = worldPos.X - cx * Chunk.Width;
      int ly = worldPos.Y;
      int lz = worldPos.Z - cz * Chunk.Depth;
      if (!chunk.IsInside(lx, ly, lz)) return false;
      chunk.SetBlock(lx, ly, lz, Block.Air);
      var newMesh = new ChunkMesh();
      newMesh.Build(chunk);
      mesh.Dispose();
      ChunkMeshes[i] = (newMesh, chunk, chunkPos);
      return true;
    }
    return false;
  }

  public bool PlaceBlock(Vector3i worldPos, Block block)
  {
    int cx = (int)MathF.Floor(worldPos.X / (float)Chunk.Width);
    int cz = (int)MathF.Floor(worldPos.Z / (float)Chunk.Depth);

    for (int i = 0; i < ChunkMeshes.Count; i++)
    {
      var (mesh, chunk, chunkPos) = ChunkMeshes[i];
      if (chunkPos.X != cx || chunkPos.Z != cz) continue;
      int lx = worldPos.X - cx * Chunk.Width;
      int ly = worldPos.Y;
      int lz = worldPos.Z - cz * Chunk.Depth;
      if (!chunk.IsInside(lx, ly, lz)) return false;
      if (chunk.GetBlock(lx, ly, lz) != Block.Air) return false;
      chunk.SetBlock(lx, ly, lz, block);
      var newMesh = new ChunkMesh();
      newMesh.Build(chunk);
      mesh.Dispose();
      ChunkMeshes[i] = (newMesh, chunk, chunkPos);
      return true;
    }
    return false;
  }

  public void Dispose()
  {
    foreach (var (mesh, _, _) in ChunkMeshes)
      mesh.Dispose();
    ChunkMeshes.Clear();
  }
}