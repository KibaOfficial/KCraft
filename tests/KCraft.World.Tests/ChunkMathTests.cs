// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;

namespace KCraft.World.Tests;

public sealed class ChunkMathTests
{
  // FromWorld
  [Theory]
  [InlineData(  0,   0,   0,  0)]
  [InlineData( 15,  15,   0,  0)]
  [InlineData( 16,  16,   1,  1)]
  [InlineData( 17,  17,   1,  1)]
  [InlineData( -1,  -1,  -1, -1)]
  [InlineData(-16, -16,  -1, -1)]
  [InlineData(-17, -17,  -2, -2)]
  public void FromWorld_ShouldReturnCorrectChunkPosition(
    int worldX, int worldZ, int expectedX, int expectedZ)
  {
    var pos = ChunkMath.FromWorld(worldX, worldZ);
    Assert.Equal(expectedX, pos.X);
    Assert.Equal(expectedZ, pos.Z);
  }

  // LocalX
  [Theory]
  [InlineData(  0,  0)]
  [InlineData( 15, 15)]
  [InlineData( 16,  0)]
  [InlineData( 17,  1)]
  [InlineData( -1, 15)]
  [InlineData(-16,  0)]
  [InlineData(-17, 15)]
  public void LocalX_ShouldReturnCorrectLocalCoordinate(int worldX, int expected)
  {
    Assert.Equal(expected, ChunkMath.LocalX(worldX));
  }

  // LocalZ
  [Theory]
  [InlineData(  0,  0)]
  [InlineData( 15, 15)]
  [InlineData( 16,  0)]
  [InlineData( 17,  1)]
  [InlineData( -1, 15)]
  [InlineData(-16,  0)]
  [InlineData(-17, 15)]
  public void LocalZ_ShouldReturnCorrectLocalCoordinate(int worldZ, int expected)
  {
    Assert.Equal(expected, ChunkMath.LocalZ(worldZ));
  }

  // WorldX / WorldZ
  [Theory]
  [InlineData( 0,   0,   0)]
  [InlineData( 1,  16,  16)]
  [InlineData( 2,  32,  32)]
  [InlineData(-1, -16, -16)]
  [InlineData(-2, -32, -32)]
  public void WorldXZ_ShouldReturnChunkOrigin(int chunk, int expectedWorldX, int expectedWorldZ)
  {
    var pos = new ChunkPosition(chunk, chunk);
    Assert.Equal(expectedWorldX, ChunkMath.WorldX(pos));
    Assert.Equal(expectedWorldZ, ChunkMath.WorldZ(pos));
  }
}