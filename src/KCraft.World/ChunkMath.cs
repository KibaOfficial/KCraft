// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.World;

public static class ChunkMath
{
  public static ChunkPosition FromWorld(int worldX, int worldZ)
    => new(worldX >> 4, worldZ >> 4);

  public static int LocalX(int worldX) => ((worldX % Chunk.Width)  + Chunk.Width)  % Chunk.Width;
  public static int LocalZ(int worldZ) => ((worldZ % Chunk.Depth)  + Chunk.Depth)  % Chunk.Depth;

  public static int WorldX(ChunkPosition pos) => pos.X * Chunk.Width;
  public static int WorldZ(ChunkPosition pos) => pos.Z * Chunk.Depth;
}