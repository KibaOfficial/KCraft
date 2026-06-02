// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World.Generation;

public enum Biome { Ocean, Beach, Plains }

public sealed class NoiseWorldGenerator : IWorldGenerator
{
  private readonly FastNoiseLite _noise;
  private readonly int _seed;

  public const int SeaLevel = 64;
  public const int TerrainAmplitude = 25;

  // Biom-Schwellen
  private const int OceanMax = SeaLevel - 4;          // <= 60  → Ocean
  private const int BeachMax = SeaLevel + 2;      // 66-67  → Beach
                                                  // > 67 → Plains

  public NoiseWorldGenerator(int seed = 1337)
  {
    _seed = seed;
    _noise = new FastNoiseLite(seed);
    _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    _noise.SetFractalOctaves(4);
    _noise.SetFractalLacunarity(2.0f);
    _noise.SetFractalGain(0.5f);
    _noise.SetFrequency(0.005f);
  }

  private static Biome GetBiome(int surface) => surface switch
  {
    <= OceanMax => Biome.Ocean,
    <= BeachMax => Biome.Beach,
    _ => Biome.Plains,
  };

  public void Generate(Chunk chunk, int chunkX = 0, int chunkZ = 0)
  {
    // ── Terrain ───────────────────────────────────────────────────
    for (int x = 0; x < Chunk.Width; x++)
      for (int z = 0; z < Chunk.Depth; z++)
      {
        int worldX = chunkX * Chunk.Width + x;
        int worldZ = chunkZ * Chunk.Depth + z;

        float n = _noise.GetNoise(worldX, worldZ);
        int surface = SeaLevel + (int)(n * TerrainAmplitude);
        var biome = GetBiome(surface);

        for (int y = 0; y < Chunk.Height; y++)
        {
          Block block;

          if (y > surface && y <= SeaLevel)
          {
            chunk.SetBlock(x, y, z, Block.Water);
            chunk.SetFluidLevel(x, y, z, 0);
            continue;
          }
          else if (y > surface)
          {
            block = Block.Air;
          }
          else if (y == surface)
          {
            block = biome switch
            {
              Biome.Ocean => Block.Sand,   // Meeresboden
              Biome.Beach => Block.Sand,   // Strand
              Biome.Plains => Block.Grass,  // Wiese
              _ => Block.Grass,
            };
          }
          else if (y >= surface - 3)
          {
            block = biome switch
            {
              Biome.Ocean => Block.Sand,   // Meeresboden tiefer auch Sand
              Biome.Beach => Block.Sand,
              Biome.Plains => Block.Dirt,
              _ => Block.Dirt,
            };
          }
          else
          {
            block = Block.Stone;
          }

          chunk.SetBlock(x, y, z, block);
        }
      }

    // ── Bäume — nur Plains ─────────────────────────────────────
    var rng = new Random(_seed ^ (chunkX * 1234567) ^ (chunkZ * 7654321));

    for (int x = 2; x < Chunk.Width - 2; x++)
      for (int z = 2; z < Chunk.Depth - 2; z++)
      {
        if (rng.NextSingle() > 0.015f) continue;

        int surfaceY = 0;
        for (int y = Chunk.Height - 1; y >= 0; y--)
        {
          if (chunk.GetBlock(x, y, z) == Block.Grass)
          {
            surfaceY = y;
            break;
          }
        }
        if (surfaceY == 0) continue;
        if (GetBiome(surfaceY) != Biome.Plains) continue; // nur Plains

        PlaceTree(chunk, x, surfaceY, z, rng);
      }
  }

  private static void PlaceTree(Chunk chunk, int x, int surfaceY, int z, Random rng)
  {
    int trunkHeight = 4 + rng.Next(0, 3);

    for (int i = 1; i <= trunkHeight; i++)
    {
      int ty = surfaceY + i;
      if (chunk.IsInside(x, ty, z))
        chunk.SetBlock(x, ty, z, Block.OakLog);
    }

    int top = surfaceY + trunkHeight;
    var layers = new (int dy, int radius, float corner)[]
    {
            ( 2, 0, 1.0f),
            ( 1, 1, 1.0f),
            ( 0, 2, 0.5f),
            (-1, 2, 0.5f),
    };

    foreach (var (dy, radius, corner) in layers)
    {
      int ly = top + dy;
      for (int dx = -radius; dx <= radius; dx++)
        for (int dz = -radius; dz <= radius; dz++)
        {
          bool isCorner = radius > 0 && Math.Abs(dx) == radius && Math.Abs(dz) == radius;
          if (isCorner && rng.NextSingle() > corner) continue;
          int lx = x + dx;
          int lz = z + dz;
          if (!chunk.IsInside(lx, ly, lz)) continue;
          if (chunk.GetBlock(lx, ly, lz) != Block.Air) continue;
          chunk.SetBlock(lx, ly, lz, Block.OakLeaves);
        }
    }
  }

  void IWorldGenerator.Generate(Chunk chunk) => Generate(chunk);
}