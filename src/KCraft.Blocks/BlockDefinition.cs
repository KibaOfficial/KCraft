// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Blocks;

public sealed class BlockDefinition
{
  public string TextureTop { get; set; } = "dirt";
  public string TextureSide { get; set; } = "dirt";
  public string TextureBottom { get; set; } = "dirt";
  public bool IsTransparent { get; set; } = false;
  public bool IsFluid { get; set; } = false;
}