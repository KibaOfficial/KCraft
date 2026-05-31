// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World.Generation;

public sealed class NoiseWorldGenerator : IWorldGenerator
{
  private readonly FastNoiseLite _noise;

  public const int SeaLevel = 64;
  public const int TerrainAmplitude = 20;

  public NoiseWorldGenerator(int seed = 1337)
  {
    _noise = new FastNoiseLite(seed);
    _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    _noise.SetFractalOctaves(4);
    _noise.SetFractalLacunarity(0.005f);
  }

  public void Generate(Chunk chunk, int chunkX = 0, int chunkZ = 0)
  {
    for (int x = 0; x < Chunk.Width;  x++)
    for (int z = 0; z < Chunk.Depth;  z++)
    {
      int worldX = chunkX * Chunk.Width + x;
      int worldZ = chunkZ * Chunk.Depth + z;

      float n      = _noise.GetNoise(worldX, worldZ); // -1..1
      int surface  = SeaLevel + (int)(n * TerrainAmplitude); // 44..84

      for (int y = 0; y < Chunk.Height; y++)
      {
        Block block;
        if (y > surface)
          block = Block.Air;
        else if (y == surface)
          block = Block.Grass;
        else if (y >= surface - 3)
          block = Block.Dirt;
        else
          block = Block.Stone;

        chunk.SetBlock(x, y, z, block);
      }
    }
  }

  void IWorldGenerator.Generate(Chunk chunk) => Generate(chunk);
}