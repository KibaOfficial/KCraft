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

}