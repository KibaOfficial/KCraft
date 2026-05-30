// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World.Generation;

public sealed class FlatWorldGenerator : IWorldGenerator
{
  public const int StoneMaxY  = 59;
  public const int DirtY      = 60;
  public const int GrassY     = 61;

  public void Generate(Chunk chunk)
  {
    for (int x = 0; x < Chunk.Width; x++)
    for (int z = 0; z < Chunk.Depth; z++)
      {
        for (int y = 0; y <= StoneMaxY; y++)
          chunk.SetBlock(x, y, z, Block.Stone);

        chunk.SetBlock(x, DirtY, z, Block.Dirt);
        chunk.SetBlock(x, GrassY, z, Block.Grass);
      }
  }
}