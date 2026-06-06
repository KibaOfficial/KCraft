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
  public void Move(float dx, float dy, float dz,
    Func<int, int, int, Block?> getBlock,
    Func<int, int, int, byte>? getMetadata = null)
  {
    var aabb = BoundingBox;

    // Alle Block-AABBs im Weg sammeln
    var blocks = GetBlockAABBs(aabb, dx, dy, dz, getBlock, getMetadata);

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

    // ── Step-Up ───────────────────────────────────────────────────────
    bool steppedUp = false;
    bool xBlocked = Math.Abs(dx - originalDx) > 0.001f;
    bool zBlocked = Math.Abs(dz - originalDz) > 0.001f;

    if (OnGround && (xBlocked || zBlocked))
    {
      const float stepHeight = 0.6f;
      var preStepAabb = BoundingBox; // original AABB vor dem Move

      // Hochheben
      var steppedAabb = preStepAabb.Offset(0, stepHeight, 0);
      var stepBlocks = GetBlockAABBs(steppedAabb, originalDx, 0, originalDz, getBlock, getMetadata);

      float stepDx = originalDx;
      float stepDz = originalDz;
      foreach (var b in stepBlocks) stepDx = steppedAabb.ClipMoveX(b, stepDx);
      steppedAabb = steppedAabb.Offset(stepDx, 0, 0);
      foreach (var b in stepBlocks) stepDz = steppedAabb.ClipMoveZ(b, stepDz);
      steppedAabb = steppedAabb.Offset(0, 0, stepDz);

      bool movedX = Math.Abs(stepDx - originalDx) < 0.001f;
      bool movedZ = Math.Abs(stepDz - originalDz) < 0.001f;

      if ((xBlocked && movedX) || (zBlocked && movedZ))
      {
        // Nach unten fallen bis Boden
        float stepDy = -stepHeight;
        var downBlocks = GetBlockAABBs(steppedAabb, 0, stepDy, 0, getBlock, getMetadata);
        foreach (var b in downBlocks) stepDy = steppedAabb.ClipMoveY(b, stepDy);
        steppedAabb = steppedAabb.Offset(0, stepDy, 0);

        aabb = steppedAabb;
        steppedUp = true;

        var vel2 = Velocity;
        vel2.X = 0;
        vel2.Z = 0;
        Velocity = vel2;
      }
    }
    // ── Ende Step-Up ──────────────────────────────────────────────────

    // ── Slope Heightfield ─────────────────────────────────────────────
    bool onSlope = false;
    if (getMetadata != null)
      onSlope = ResolveSlope(ref aabb, getBlock, getMetadata, Width);
    // ── Ende Slope Heightfield ────────────────────────────────────────

    // OnGround — Slope ODER Step-Up ODER normaler Boden-Clip
    OnGround = onSlope || steppedUp || (originalDy < 0 && Math.Abs(dy - originalDy) > 0.001f);

    // Velocity auf 0 wenn geclipt
    var vel = Velocity;
    if (Math.Abs(dx - originalDx) > 0.001f) vel.X = 0;
    if (Math.Abs(dy - originalDy) > 0.001f) vel.Y = 0;
    if (Math.Abs(dz - originalDz) > 0.001f) vel.Z = 0;
    Velocity = vel;

    // Position aktualisieren — aus AABB
    Position = new Vector3(
      (aabb.MinX + aabb.MaxX) / 2f,
      aabb.MinY,
      (aabb.MinZ + aabb.MaxZ) / 2f);
  }

  private static List<AABB> GetBlockAABBs(AABB aabb, float dx, float dy, float dz,
    Func<int, int, int, Block?> getBlock,
    Func<int, int, int, byte>? getMetadata = null)
  {
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
            if (!block.HasValue) continue;

            var def = BlockRegistry.Definitions.TryGetValue(block.Value, out var d) ? d : null;

            if (def?.IsStairs == true && getMetadata != null)
            {
              var facing = (BlockFacing)getMetadata(x, y, z);
              // Untere Hälfte — immer
              result.Add(new AABB(x, y, z, x + 1, y + 0.5f, z + 1));
              // Obere Hälfte — je nach Facing
              switch (facing)
              {
                case BlockFacing.North:
                  result.Add(new AABB(x, y + 0.5f, z, x + 1, y + 1f, z + 0.5f));
                  break;
                case BlockFacing.South:
                  result.Add(new AABB(x, y + 0.5f, z + 0.5f, x + 1, y + 1f, z + 1));
                  break;
                case BlockFacing.West:
                  result.Add(new AABB(x, y + 0.5f, z, x + 0.5f, y + 1f, z + 1));
                  break;
                case BlockFacing.East:
                  result.Add(new AABB(x + 0.5f, y + 0.5f, z, x + 1, y + 1f, z + 1));
                  break;
              }
            }
            else if (def?.IsSlope == true)
            {
              continue; // kein AABB für Slopes — Heightfield übernimmt
            }
            else
            {
              result.Add(new AABB(x, y, z, x + 1, y + 1, z + 1));
            }
          }

      return result;
    }
  }

  private static bool ResolveSlope(
      ref AABB aabb,
      Func<int, int, int, Block?> getBlock,
      Func<int, int, int, byte> getMetadata,
      float width)
  {
    float cx = (aabb.MinX + aabb.MaxX) / 2f;
    float cz = (aabb.MinZ + aabb.MaxZ) / 2f;

    var points = new (float x, float z)[]
    {
    (cx, cz),
    };

    float bestY = float.NegativeInfinity;

    foreach (var (fx, fz) in points)
    {
      int bx = (int)MathF.Floor(fx);
      int bz = (int)MathF.Floor(fz);
      int footY = (int)MathF.Floor(aabb.MinY + 0.05f);

      for (int by = footY + 1; by >= footY - 2; by--)
      {
        var block = getBlock(bx, by, bz);
        if (!block.HasValue) continue;

        var def = BlockRegistry.Definitions.TryGetValue(block.Value, out var d) ? d : null;
        if (def?.IsSlope != true) continue;

        var facing = (BlockFacing)getMetadata(bx, by, bz);
        float lx = Math.Clamp(fx - bx, 0f, 1f);
        float lz = Math.Clamp(fz - bz, 0f, 1f);

        float h = facing switch
        {
          BlockFacing.North => 1f - lz,
          BlockFacing.South => lz,
          BlockFacing.West => 1f - lx,
          BlockFacing.East => lx,
          _ => 0f
        };

        float targetY = by + h;

        if (aabb.MinY >= targetY - 0.75f && aabb.MinY <= targetY + 0.45f)
          bestY = MathF.Max(bestY, targetY);
      }
    }

    if (float.IsNegativeInfinity(bestY))
    {
      int bx = (int)MathF.Floor(cx);
      int bz = (int)MathF.Floor(cz);
      int by = (int)MathF.Floor(aabb.MinY - 0.05f);

      var block = getBlock(bx, by, bz);
      if (block.HasValue)
      {
        var def = BlockRegistry.Definitions.TryGetValue(block.Value, out var d) ? d : null;

        if (def?.IsSlope != true)
        {
          float groundY = by + 1f;
          float deltaGround = groundY - aabb.MinY;

          if (deltaGround <= 0.25f && deltaGround >= -0.8f)
          {
            aabb = aabb.Offset(0, deltaGround, 0);
            return true;
          }
        }
      }

      return false;
    }

    float delta = bestY - aabb.MinY;

    if (delta > 0.45f) return false;
    if (delta < -0.8f) return false;

    aabb = aabb.Offset(0, delta, 0);
    return true;
  }
  public abstract void Tick(Func<int, int, int, Block?> getBlock);
}