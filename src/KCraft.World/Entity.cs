// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using OpenTK.Mathematics;

namespace KCraft.World;

public abstract class Entity
{
  // ── Dimensionen (wie MC) ──────────────────────────────────────────────
  public virtual float Width { get; } = 0.6f;
  public virtual float Height { get; } = 1.8f;

  // ── State ─────────────────────────────────────────────────────────────
  public Vector3 Position { get; set; }
  public Vector3 Velocity { get; set; }
  public bool OnGround { get; protected set; }

  public AABB BoundingBox => AABB.FromCenter(Position, Width, Height);

  protected Entity(Vector3 spawnPos)
  {
    Position = spawnPos;
  }

  // ── Physik (MC moveEntity Prinzip) ────────────────────────────────────
  public void Move(float dx, float dy, float dz, Func<int, int, int, Block?> getBlock)
  {
    var aabb = BoundingBox;

    // Alle Block-AABBs im Weg sammeln
    var blocks = GetBlockAABBs(aabb, dx, dy, dz, getBlock);

    // Y zuerst (Gravity wichtigste Achse)
    float originalDy = dy;
    foreach (var b in blocks) dy = aabb.ClipMoveY(b, dy);
    aabb = aabb.Offset(0, dy, 0);

    // X
    float originalDx = dx;
    foreach (var b in blocks) dx = aabb.ClipMoveX(b, dx);
    aabb = aabb.Offset(dx, 0, 0);

    // Z
    float originalDz = dz;
    foreach (var b in blocks) dz = aabb.ClipMoveZ(b, dz);
    aabb = aabb.Offset(0, 0, dz);

    // OnGround: wenn dy < 0 und Y geclipt wurde → auf Boden
    OnGround = originalDy < 0 && Math.Abs(dy - originalDy) > 0.001f;

    // Velocity auf 0 wenn geclipt
    var vel = Velocity;
    if (Math.Abs(dx - originalDx) > 0.001f) vel.X = 0;
    if (Math.Abs(dy - originalDy) > 0.001f) vel.Y = 0;
    if (Math.Abs(dz - originalDz) > 0.001f) vel.Z = 0;
    Velocity = vel;

    // Position aktualisieren — aus AABB MinX/MinZ, MinY = Feet
    Position = new Vector3(
      (aabb.MinX + aabb.MaxX) / 2f,
      aabb.MinY,
      (aabb.MinZ + aabb.MaxZ) / 2f);
  }

  private static List<AABB> GetBlockAABBs(AABB aabb, float dx, float dy, float dz,
    Func<int, int, int, Block?> getBlock)
  {
    var expanded = aabb.Expand(dx, dy, dz);
    var result = new List<AABB>();

    int x0 = (int)MathF.Floor(expanded.MinX) - 1;
    int x1 = (int)MathF.Ceiling(expanded.MaxX) + 1;
    int y0 = (int)MathF.Floor(expanded.MinY) - 1;
    int y1 = (int)MathF.Ceiling(expanded.MaxY) + 1;
    int z0 = (int)MathF.Floor(expanded.MinZ) - 1;
    int z1 = (int)MathF.Ceiling(expanded.MaxZ) + 1;

    for (int x = x0; x <= x1; x++)
      for (int y = y0; y <= y1; y++)
        for (int z = z0; z <= z1; z++)
        {
          var block = getBlock(x, y, z);
          if (block.HasValue)
            result.Add(new AABB(x, y, z, x + 1, y + 1, z + 1));
        }

    return result;
  }

  public abstract void Tick(Func<int, int, int, Block?> getBlock);
}