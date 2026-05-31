// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
namespace KCraft.Blocks;

public static class BlockRegistry
{
  public static readonly Dictionary<Block, BlockDefinition> Definitions = new()
  {
    [Block.Grass] = new() {
      TextureTop    = "grass_block_top",
      TextureSide   = "grass_block_side",
      TextureBottom = "dirt",
    },
    [Block.Dirt]  = new() { TextureTop = "dirt", TextureSide = "dirt", TextureBottom = "dirt" },
    [Block.Stone] = new() { TextureTop = "stone", TextureSide = "stone", TextureBottom = "stone" },
  };
  public static BlockDefinition Get(Block block) => Definitions[block]; 
}