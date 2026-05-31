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
  public const int Seed         = 42;

  // ── Data ──────────────────────────────────────────────────────────────
  public List<(ChunkMesh mesh, Chunk chunk, Vector3i chunkPos)> ChunkMeshes { get; } = [];
  public int ChunkCount => ChunkMeshes.Count;

  public WorldManager()
  {
    var generator = new NoiseWorldGenerator(seed: Seed);
    for (int cx = -RenderRadius; cx <= RenderRadius; cx++)
    for (int cz = -RenderRadius; cz <= RenderRadius; cz++)
    {
      var chunk = new Chunk();
      generator.Generate(chunk, cx, cz);
      var mesh = new ChunkMesh();
      mesh.Build(chunk);
      ChunkMeshes.Add((mesh, chunk, new Vector3i(cx, 0, cz)));
    }
  }

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

  public void Dispose()
  {
    foreach (var (mesh, _, _) in ChunkMeshes)
      mesh.Dispose();
  }
}