// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World.Tests;

public sealed class ChunkTests
{
  [Fact]
  public void NewChunk_ShouldContainAirByDefault()
  {
    var chunk = new Chunk();
    var block = chunk.GetBlock(0, 0, 0);
    Assert.Equal(Block.Air, block);
  }

  [Fact]
  public void SetBlock_ShouldStoreBlock()
  {
    var chunk = new Chunk();
    chunk.SetBlock(1, 2, 3, Block.Dirt);
    var block = chunk.GetBlock(1, 2, 3);
    Assert.Equal(Block.Dirt, block);
  }

  [Fact]
  public void GetBlock_WithInvalidX_ShouldThrow()
  {
    var chunk = new Chunk();

    Assert.Throws<ArgumentOutOfRangeException>(() => 
      chunk.GetBlock(-1, 0, 0)
    );
  }

  // IsInside - valid
  [Fact]
  public void IsInside_WithValidCoords_ShouldReturnTrue()
  {
    var chunk = new Chunk();
    Assert.True(chunk.IsInside(0, 0, 0));
    Assert.True(chunk.IsInside(15, 255, 15));
    Assert.True(chunk.IsInside(8, 128, 8));
  }

  // IsInside - Grenzen
  [Theory]
  [InlineData(-1,  0,  0)]
  [InlineData(16,  0,  0)]
  [InlineData( 0, -1,  0)]
  [InlineData( 0, 256,  0)]
  [InlineData( 0,  0, -1)]
  [InlineData( 0,  0, 16)]
  public void IsInside_WithInvalidCoords_ShouldReturnFalse(int x, int y, int z)
  {
    var chunk = new Chunk();
    Assert.False(chunk.IsInside(x, y, z));
  }

  // ValidateCoordinates baut auf IsInside auf
  [Theory]
  [InlineData(-1,  0,  0, "x")]
  [InlineData(16,  0,  0, "x")]
  [InlineData( 0, -1,  0, "y")]
  [InlineData( 0, 256,  0, "y")]
  [InlineData( 0,  0, -1, "z")]
  [InlineData( 0,  0, 16, "z")]
  public void GetBlock_WithInvalidCoords_ShouldThrowWithParamName(int x, int y, int z, string param)
  {
    var chunk = new Chunk();
    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(x, y, z));
    Assert.Equal(param, ex.ParamName);
  }

}