// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World;

public static class FaceVisibility
{
  private static bool IsTransparent(Block block) => block switch
  {
    Block.Air => true,
    Block.OakLeaves => true,
    Block.Water => true,
    _ => false,
  };

  public static bool IsVisible(Chunk chunk, int x, int y, int z, FaceDirection face,
      Func<int, int, int, Block?>? getWorldBlock = null, int chunkX = 0, int chunkZ = 0)
  {
    var (nx, ny, nz) = face switch
    {
      FaceDirection.North => (x, y, z - 1),
      FaceDirection.South => (x, y, z + 1),
      FaceDirection.East => (x + 1, y, z),
      FaceDirection.West => (x - 1, y, z),
      FaceDirection.Up => (x, y + 1, z),
      FaceDirection.Down => (x, y - 1, z),
      _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    var current = chunk.GetBlock(x, y, z);

    // Innerhalb des Chunks
    if (chunk.IsInside(nx, ny, nz))
    {
      var neighbor = chunk.GetBlock(nx, ny, nz);
      if (current == Block.Water && neighbor == Block.Water) return false;
      return IsTransparent(neighbor);
    }

    // Chunk-Grenze — getWorldBlock nutzen
    if (getWorldBlock != null)
    {
      int wx = chunkX * Chunk.Width + nx;
      int wy = ny;
      int wz = chunkZ * Chunk.Depth + nz;
      var worldNeighbor = getWorldBlock(wx, wy, wz);

      if (worldNeighbor == null) return true; // Chunk nicht geladen
      if (current == Block.Water && worldNeighbor == Block.Water) return false;
      return IsTransparent(worldNeighbor.Value);
    }

    return true;
  }
}