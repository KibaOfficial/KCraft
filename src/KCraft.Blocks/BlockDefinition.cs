// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Blocks;

public sealed class BlockDefinition
{
  public string TextureTop { get; set; } = "dirt";
  public string TextureSide { get; set; } = "dirt";
  public string TextureBottom { get; set; } = "dirt";
  public bool IsSolid { get; set; } = true;
  public bool IsTransparent { get; set; } = false;
  public bool IsFluid { get; set; } = false;
  public bool IsSwimmable { get; set; } = false;
  public bool IsFullCube { get; set; } = true;
  public bool UsesCTM { get; set; } = false; // Connected Texture Mod (CTM) support just in vanilla bc why need a mod when you can just do it yourself
  public string CTMTexture { get; set; } = "";
  public bool IsStairs { get; set; } = false;
  public bool IsSlope { get; init; } = false;
}
