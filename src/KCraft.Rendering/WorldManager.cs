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
  public const int Seed = 42;

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

      // Mesh neu bauen
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
    int centerX = (int)MathF.Floor(worldPos.X / (float)Chunk.Width);
    int centerZ = (int)MathF.Floor(worldPos.Z / (float)Chunk.Depth);

    for (int i = 0; i < ChunkMeshes.Count; i++)
    {
      var (mesh, chunk, chunkPos) = ChunkMeshes[i];
        if (chunkPos.X != centerX || chunkPos.Z != centerZ) continue;

        int lx = worldPos.X - centerX * Chunk.Width;
        int ly = worldPos.Y;
        int lz = worldPos.Z - centerZ * Chunk.Depth;

        if (!chunk.IsInside(lx, ly, lz)) return false;
        if (chunk.GetBlock(lx, ly, lz) != Block.Air) return false; // schon belegt

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
  }
}