// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World;

public static class FaceVisibility
{
  // Blöcke die transparent sind — Faces dahinter werden gerendert
  private static bool IsTransparent(Block block) => block switch
  {
    Block.Air => true,
    Block.OakLeaves => true,
    _ => false,
  };

  public static bool IsVisible(Chunk chunk, int x, int y, int z, FaceDirection face)
  {
    var (nx, ny, nz) = face switch
    {
      FaceDirection.North => (x, y, z - 1),
      FaceDirection.South => (x, y, z + 1),
      FaceDirection.East => (x + 1, y, z),
      FaceDirection.West => (x - 1, y, z),
      FaceDirection.Up => (x, y + 1, z),
      FaceDirection.Down => (x, y - 1, z),
      _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
    };

    if (!chunk.IsInside(nx, ny, nz)) return true;
    return IsTransparent(chunk.GetBlock(nx, ny, nz));
  }
}