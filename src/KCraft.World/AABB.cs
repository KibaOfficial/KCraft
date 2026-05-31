// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.World;

public struct AABB
{
  public float MinX, MinY, MinZ;
  public float MaxX, MaxY, MaxZ;

  public AABB(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
  {
    MinX = minX; MinY = minY; MinZ = minZ;
    MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
  }

  public static AABB FromCenter(Vector3 pos, float width, float height)
  {
    float halfWidth = width / 2f;
    return new AABB(
      pos.X - halfWidth, pos.Y, pos.Z - halfWidth,
      pos.X + halfWidth, pos.Y + height, pos.Z + halfWidth);
  }

  public AABB Offset(float dx, float dy, float dz) => new(
    MinX + dx, MinY + dy, MinZ + dz,
    MaxX + dx, MaxY + dy, MaxZ + dz);

  public AABB Expand(float dx, float dy, float dz) => new(
    dx < 0 ? MinX + dx : MinX,
    dy < 0 ? MinY + dy : MinY,
    dz < 0 ? MinZ + dz : MinZ,
    dx > 0 ? MaxX + dx : MaxX,
    dy > 0 ? MaxY + dy : MaxY,
    dz > 0 ? MaxZ + dz : MaxZ);

  public bool Intersects(AABB other)
    => MaxX > other.MinX && MinX < other.MaxX
    && MaxY > other.MinY && MinY < other.MaxY
    && MaxZ > other.MinZ && MinZ < other.MaxZ;

  // Wie weit kann diese AABB in Y bewegen bis sie other trifft?
  public float ClipMoveY(AABB other, float dy)
  {
    if (other.MaxX <= MinX || other.MinX >= MaxX) return dy;
    if (other.MaxZ <= MinZ || other.MinZ >= MaxZ) return dy;
    if (dy > 0 && other.MinY >= MaxY)
    {
      float dist = other.MinY - MaxY;
      if (dist < dy) dy = dist;
    }
    if (dy < 0 && other.MaxY <= MinY)
    {
      float dist = other.MaxY - MinY;
      if (dist > dy) dy = dist;
    }
    return dy;
  }

  public float ClipMoveX(AABB other, float dx)
  {
    if (other.MaxY <= MinY || other.MinY >= MaxY) return dx;
    if (other.MaxZ <= MinZ || other.MinZ >= MaxZ) return dx;
    if (dx > 0 && other.MinX >= MaxX)
    {
      float dist = other.MinX - MaxX;
      if (dist < dx) dx = dist;
    }
    if (dx < 0 && other.MaxX <= MinX)
    {
      float dist = other.MaxX - MinX;
      if (dist > dx) dx = dist;
    }
    return dx;
  }

  public float ClipMoveZ(AABB other, float dz)
  {
    if (other.MaxX <= MinX || other.MinX >= MaxX) return dz;
    if (other.MaxY <= MinY || other.MinY >= MaxY) return dz;
    if (dz > 0 && other.MinZ >= MaxZ)
    {
      float dist = other.MinZ - MaxZ;
      if (dist < dz) dz = dist;
    }
    if (dz < 0 && other.MaxZ <= MinZ)
    {
      float dist = other.MaxZ - MinZ;
      if (dist > dz) dz = dist;
    }
    return dz;
  }
}