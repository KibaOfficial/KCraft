// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.World;

public sealed class Player : Entity
{
  // ── Konstanten (wie MC) ───────────────────────────────────────────────
  public const float EyeHeight = 1.62f;
  public const float EyeHeightSneak = 1.27f;
  public const float HeightNormal = 1.8f;
  public const float HeightSneak = 1.5f;
  public const float Gravity = -32f;
  public const float JumpVelocity = 10f;
  public const float WalkSpeed = 0.65f;
  public const float SprintSpeed = 1.1f;
  public const float SneakSpeed = 0.22f;
  public const float Drag = 0.6f;
  private bool _pendingSneaking;
  // ── State ─────────────────────────────────────────────────────────────
  public bool IsSneaking { get; private set; }
  public bool IsSprinting { get; private set; }
  // ── Override Height dynamisch ─────────────────────────────────────────
  public override float Height => IsSneaking ? HeightSneak : HeightNormal;
  // ── Eye-Position ──────────────────────────────────────────────────────
  public Vector3 EyePosition => Position + new Vector3(0,
      IsSneaking ? EyeHeightSneak : EyeHeight, 0);

  public Player(Vector3 spawnPos) : base(spawnPos) { }

  public override void Tick(Func<int, int, int, Block?> getBlock)
  {
    IsSneaking = _pendingSneaking;

    var vel = Velocity;
    vel.Y += Gravity * (1f / 20f);
    Velocity = vel;

    bool wasOnGround = OnGround;
    float dx = Velocity.X * (1f / 20f);
    float dz = Velocity.Z * (1f / 20f);

    // Edge snapping beim Sneaken — VOR dem Move
    if (IsSneaking && wasOnGround)
    {
      // Teste ob wir nach dem Move noch auf dem Boden wären
      // Wenn nicht → einzelne Achsen testen
      float stepDown = -0.6f; // max fall check

      bool xSafe = HasGroundAt(Position.X + dx, Position.Y, Position.Z, stepDown, getBlock);
      bool zSafe = HasGroundAt(Position.X, Position.Y, Position.Z + dz, stepDown, getBlock);
      bool bothSafe = HasGroundAt(Position.X + dx, Position.Y, Position.Z + dz, stepDown, getBlock);

      if (!bothSafe)
      {
        if (!xSafe) dx = 0;      // X nicht sicher → X blockieren
        if (!zSafe) dz = 0;      // Z nicht sicher → Z blockieren
      }
    }

    Move(dx, Velocity.Y * (1f / 20f), dz, getBlock);

    if (IsSneaking && wasOnGround && !OnGround)
    {
      var snappedPos = Position;

      // Versuche zurück zur letzten sicheren Y-Position
      // und prüfe ob wir dort stehen können
      float checkY = snappedPos.Y - 0.01f;

      // Snapped X: teste nur X zurücknehmen
      var testX = new Vector3(snappedPos.X - dx, checkY, snappedPos.Z);
      if (IsOnGround(testX, getBlock)) { Position = testX; OnGround = true; goto done; }

      // Snapped Z: teste nur Z zurücknehmen  
      var testZ = new Vector3(snappedPos.X, checkY, snappedPos.Z - dz);
      if (IsOnGround(testZ, getBlock)) { Position = testZ; OnGround = true; goto done; }

      // Beide zurück
      var testXZ = new Vector3(snappedPos.X - dx, checkY, snappedPos.Z - dz);
      if (IsOnGround(testXZ, getBlock)) { Position = testXZ; OnGround = true; }

    done:;
    }

    vel = Velocity;
    vel.X *= OnGround ? Drag : 0.91f;
    vel.Z *= OnGround ? Drag : 0.91f;
    Velocity = vel;
  }

  public void ProcessInput(KeyboardState keyboard, float yaw)
  {
    _pendingSneaking = keyboard.IsKeyDown(Keys.LeftShift);
    IsSprinting = keyboard.IsKeyDown(Keys.LeftControl) && !IsSneaking;

    float rad = MathHelper.DegreesToRadians(yaw + 90f);
    float sinY = MathF.Sin(rad);
    float cosY = MathF.Cos(rad);

    float fx = sinY, fz = -cosY;
    float rx = cosY, rz = sinY;

    float mx = 0, mz = 0;
    if (keyboard.IsKeyDown(Keys.W)) { mx += fx; mz += fz; }
    if (keyboard.IsKeyDown(Keys.S)) { mx -= fx; mz -= fz; }
    if (keyboard.IsKeyDown(Keys.A)) { mx -= rx; mz -= rz; }
    if (keyboard.IsKeyDown(Keys.D)) { mx += rx; mz += rz; }

    float len = MathF.Sqrt(mx * mx + mz * mz);
    if (len > 0.01f) { mx /= len; mz /= len; }

    float speed = IsSneaking ? SneakSpeed
                : IsSprinting ? SprintSpeed
                : WalkSpeed;

    var vel = Velocity;
    vel.X += mx * speed * (OnGround ? 1f : 0.3f);
    vel.Z += mz * speed * (OnGround ? 1f : 0.3f);
    Velocity = vel;
  }

  public void Jump()
  {
    if (!OnGround) return; // kein Springen beim Sneaken
    var vel = Velocity;
    vel.Y = JumpVelocity;
    Velocity = vel;
  }

  private bool HasGroundAt(float x, float y, float z, float stepDown,
    Func<int, int, int, Block?> getBlock)
  {
    float hw = Width / 2f - 0.01f; // leicht kleiner als AABB
    int by = (int)MathF.Floor(y + stepDown);
    return getBlock((int)MathF.Floor(x - hw), by, (int)MathF.Floor(z - hw)).HasValue
        || getBlock((int)MathF.Floor(x + hw), by, (int)MathF.Floor(z - hw)).HasValue
        || getBlock((int)MathF.Floor(x - hw), by, (int)MathF.Floor(z + hw)).HasValue
        || getBlock((int)MathF.Floor(x + hw), by, (int)MathF.Floor(z + hw)).HasValue;
  }

  private bool IsOnGround(Vector3 pos, Func<int, int, int, Block?> getBlock)
  {
    // Prüfe ob unter der AABB ein Block ist
    float hw = Width / 2f;
    int y = (int)MathF.Floor(pos.Y - 0.01f);

    // 4 Ecken der Hitbox prüfen
    return getBlock((int)MathF.Floor(pos.X - hw), y, (int)MathF.Floor(pos.Z - hw)).HasValue
        || getBlock((int)MathF.Floor(pos.X + hw), y, (int)MathF.Floor(pos.Z - hw)).HasValue
        || getBlock((int)MathF.Floor(pos.X - hw), y, (int)MathF.Floor(pos.Z + hw)).HasValue
        || getBlock((int)MathF.Floor(pos.X + hw), y, (int)MathF.Floor(pos.Z + hw)).HasValue;
  }
}