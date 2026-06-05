// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Blocks;

public static class BlockRegistry
{
  public static readonly Dictionary<Block, BlockDefinition> Definitions = new()
  {
    [Block.Grass] = new()
    {
      TextureTop = "grass_block_top",
      TextureSide = "grass_block_side",
      TextureBottom = "dirt",
    },
    [Block.OakLog] = new()
    {
      TextureTop = "oak_log_top",
      TextureSide = "oak_log",
      TextureBottom = "oak_log_top",
    },
    [Block.OakLeaves] = new()
    {
      TextureTop = "oak_leaves",
      TextureSide = "oak_leaves",
      TextureBottom = "oak_leaves",
      IsTransparent = true,
    },
    [Block.OakPlanks] = new()
    {
      TextureTop = "oak_planks",
      TextureSide = "oak_planks",
      TextureBottom = "oak_planks",
    },
    [Block.Sand] = new()
    {
      TextureTop = "sand",
      TextureSide = "sand",
      TextureBottom = "sand",
    },
    [Block.Dirt] = new() { TextureTop = "dirt", TextureSide = "dirt", TextureBottom = "dirt" },
    [Block.Stone] = new() { TextureTop = "stone", TextureSide = "stone", TextureBottom = "stone" },
    [Block.Water] = new()
    {
      TextureTop = "water_still",
      TextureSide = "water_still",
      TextureBottom = "water_still",
      IsSolid = false,
      IsTransparent = true,
      IsFluid = true,
      IsSwimmable = true,
    },
    [Block.Glass] = new()
    {
      TextureTop = "glass",
      TextureSide = "glass",
      TextureBottom = "glass",
      IsTransparent = true,
      UsesCTM = true,
      CTMTexture = "glass_ctm", // 4x4 sprite sheet for connected textures (top, right, bottom, left)
    },
    [Block.Cobblestone] = new()
    {
      TextureTop = "cobblestone",
      TextureSide = "cobblestone",
      TextureBottom = "cobblestone",
    },
    [Block.Gravel] = new()
    {
      TextureTop = "gravel",
      TextureSide = "gravel",
      TextureBottom = "gravel",
    },
    [Block.OakStairs] = new()
    {
      TextureTop = "oak_planks",
      TextureSide = "oak_planks",
      TextureBottom = "oak_planks",
      IsFullCube = false,
      IsStairs = true,
      IsTransparent = true, // Allow light to pass through stairs
    },
    [Block.StoneStairs] = new()
    {
      TextureTop = "stone",
      TextureSide = "stone",
      TextureBottom = "stone",
      IsFullCube = false,
      IsStairs = true,
      IsTransparent = true, // Allow light to pass through stairs
    },
    [Block.OakSlope] = new()
    {
      TextureTop = "oak_planks",
      TextureSide = "oak_planks",
      TextureBottom = "oak_planks",
      IsFullCube = false,
      IsSlope = true,
      IsTransparent = true,
    },
    [Block.StoneSlope] = new()
    {
      TextureTop = "stone",
      TextureSide = "stone",
      TextureBottom = "stone",
      IsFullCube = false,
      IsSlope = true,
      IsTransparent = true,
    },
  };
  public static BlockDefinition Get(Block block) => Definitions[block];
}
