// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using OpenTK.Mathematics;

namespace KCraft.World;

public readonly struct RaycastHit
{
  public bool     Hit        { get; init; }
  public Vector3i BlockPos   { get; init; }
  public Vector3i FaceNormal { get; init; }
  public float    Distance   { get; init; }
  public Block    Block      { get; init; }
}

public static class BlockRaycaster
{
  // DDA Voxel Traversal — Amanatides & Woo
  public static RaycastHit Cast(
    Vector3 origin,
    Vector3 direction,
    float maxDistance,
    Func<int, int, int, Block?> getBlock)
  {
    var dir = Vector3.Normalize(direction);

    int x = (int)MathF.Floor(origin.X);
    int y = (int)MathF.Floor(origin.Y);
    int z = (int)MathF.Floor(origin.Z);

    int stepX = dir.X > 0 ? 1 : dir.X < 0 ? -1 : 0;
    int stepY = dir.Y > 0 ? 1 : dir.Y < 0 ? -1 : 0;
    int stepZ = dir.Z > 0 ? 1 : dir.Z < 0 ? -1 : 0;

    float tDeltaX = stepX != 0 ? MathF.Abs(1f / dir.X) : float.MaxValue;
    float tDeltaY = stepY != 0 ? MathF.Abs(1f / dir.Y) : float.MaxValue;
    float tDeltaZ = stepZ != 0 ? MathF.Abs(1f / dir.Z) : float.MaxValue;

    float tMaxX = stepX > 0
        ? (MathF.Ceiling(origin.X) - origin.X) * tDeltaX
        : (origin.X - MathF.Floor(origin.X)) * tDeltaX;
    float tMaxY = stepY > 0
        ? (MathF.Ceiling(origin.Y) - origin.Y) * tDeltaY
        : (origin.Y - MathF.Floor(origin.Y)) * tDeltaY;
    float tMaxZ = stepZ > 0
        ? (MathF.Ceiling(origin.Z) - origin.Z) * tDeltaZ
        : (origin.Z - MathF.Floor(origin.Z)) * tDeltaZ;

    var face = Vector3i.Zero;
    float dist = 0f;

    while (dist < maxDistance)
    {
      if (tMaxX < tMaxY && tMaxX < tMaxZ)
      {
        x    += stepX;
        dist  = tMaxX;
        tMaxX += tDeltaX;
        face   = new Vector3i(-stepX, 0, 0);
      }
      else if (tMaxY < tMaxZ)
      {
        y    += stepY;
        dist  = tMaxY;
        tMaxY += tDeltaY;
        face   = new Vector3i(0, -stepY, 0);
      }
      else
      {
        z    += stepZ;
        dist  = tMaxZ;
        tMaxZ += tDeltaZ;
        face   = new Vector3i(0, 0, -stepZ);
      }

      if (dist > maxDistance) break;

      var block = getBlock(x, y, z);
      if (block.HasValue)
        return new RaycastHit
        {
          Hit        = true,
          BlockPos   = new Vector3i(x, y, z),
          FaceNormal = face,
          Distance   = dist,
          Block      = block.Value,
        };
    }

    return new RaycastHit { Hit = false };
  }
}