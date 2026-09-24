// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using KCraft.World;

namespace KCraft.Rendering;

public static class ConnectedTextureResolver
{
  public static int GetTileIndex(
    Chunk chunk,
    Func<int, int, int, Block?>? getWorldBlock,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z,
    FaceDirection face,
    Block block)
  {
    bool left, right, up, down;

    switch (face)
    {
      case FaceDirection.North:
        left = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        right = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        up = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;

      case FaceDirection.South:
        left = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        right = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        up = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;

      case FaceDirection.East:
        left = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        right = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        up = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;

      case FaceDirection.West:
        left = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        right = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        up = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;

      case FaceDirection.Up:
        left = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        right = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        up = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        down = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        break;

      default: // Down
        left = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        right = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        up = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        down = IsSameBlock(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        break;
    }

    int index = 0;

    if (left) index |= 1;
    if (right) index |= 2;
    if (up) index |= 4;
    if (down) index |= 8;

    return index;
  }

  private static bool IsSameBlock(
    Chunk chunk,
    Func<int, int, int, Block?>? getWorldBlock,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z,
    Block block)
  {
    if (chunk.IsInside(x, y, z))
      return chunk.GetBlock(x, y, z) == block;

    if (getWorldBlock == null)
      return false;

    int wx = chunkX * Chunk.Width + x;
    int wz = chunkZ * Chunk.Depth + z;

    return getWorldBlock(wx, y, wz) == block;
  }
}