// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using KCraft.World;
using KCraft.World.Generation;

namespace KCraft.World.Tests;

public sealed class FlatWorldGeneratorTests
{
  private readonly Chunk _chunk;
  private readonly FlatWorldGenerator _gen;

  public FlatWorldGeneratorTests()
  {
    _chunk = new Chunk();
    _gen   = new FlatWorldGenerator();
    _gen.Generate(_chunk);
  }

  [Fact]
  public void Generate_ShouldFillStoneBelow60()
  {
    for (int y = 0; y <= FlatWorldGenerator.StoneMaxY; y++)
      Assert.Equal(Block.Stone, _chunk.GetBlock(0, y, 0));
  }

  [Fact]
  public void Generate_ShouldPlaceDirtAtY60()
  {
    Assert.Equal(Block.Dirt, _chunk.GetBlock(0, FlatWorldGenerator.DirtY, 0));
  }

  [Fact]
  public void Generate_ShouldPlaceGrassAtY61()
  {
    Assert.Equal(Block.Grass, _chunk.GetBlock(0, FlatWorldGenerator.GrassY, 0));
  }

  [Fact]
  public void Generate_ShouldLeaveAirAboveGrass()
  {
    for (int y = FlatWorldGenerator.GrassY + 1; y < Chunk.Height; y++)
      Assert.Equal(Block.Air, _chunk.GetBlock(0, y, 0));
  }

  [Theory]
  [InlineData(0,  0)]
  [InlineData(15, 0)]
  [InlineData(0,  15)]
  [InlineData(15, 15)]
  public void Generate_ShouldFillAllColumns(int x, int z)
  {
    Assert.Equal(Block.Grass, _chunk.GetBlock(x, FlatWorldGenerator.GrassY, z));
    Assert.Equal(Block.Stone, _chunk.GetBlock(x, 0, z));
  }
}