// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Blocks;

public enum BlockFacing : byte
{
  North = 0,
  South = 1,
  East = 2,
  West = 3,
}

public static class BlockFacingHelper
{
  // Aus Camera Yaw die Facing-Richtung bestimmen
  public static BlockFacing FromYaw(float yaw)
  {
    float normalized = (yaw % 360 + 360) % 360;
    return normalized switch
    {
      >= 45 and < 135 => BlockFacing.South,
      >= 135 and < 225 => BlockFacing.West,
      >= 225 and < 315 => BlockFacing.North,
      _ => BlockFacing.East,
    };
  }
}