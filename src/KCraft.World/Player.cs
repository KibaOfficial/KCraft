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
  public const float FlySpeed = 1.2f;
  public const float FlySprintSpeed = 2.4f;
  public const float Drag = 0.6f;

  private bool _pendingSneaking;

  // ── State ─────────────────────────────────────────────────────────────
  public bool IsSneaking { get; private set; }
  public bool IsSprinting { get; private set; }
  public bool IsFlying { get; private set; }
  public bool IsCreative { get; set; }
  public bool IsSpectator { get; set; }

  // ── Override Height dynamisch ─────────────────────────────────────────
  public override float Height => IsSneaking ? HeightSneak : HeightNormal;

  // ── Eye-Position ──────────────────────────────────────────────────────
  public Vector3 EyePosition => Position + new Vector3(0,
      IsSneaking ? EyeHeightSneak : EyeHeight, 0);

  public Player(Vector3 spawnPos) : base(spawnPos) { }

  public override void Tick(Func<int, int, int, Block?> getBlock)
  {
    IsSneaking = _pendingSneaking;

    // Spectator — noclip, kein Gravity
    if (IsSpectator)
    {
      var vel = Velocity;
      vel.X *= 0.5f;
      vel.Y *= 0.5f;
      vel.Z *= 0.5f;
      Velocity = vel;
      Position += Velocity * (1f / 20f);
      return;
    }

    // Creative Flying
    if (IsCreative && IsFlying)
    {
      var vel = Velocity;
      vel.X *= 0.8f;
      vel.Y *= 0.8f;
      vel.Z *= 0.8f;
      Velocity = vel;
      Position += Velocity * (1f / 20f);
      return;
    }

    // Survival / Creative on ground
    var v = Velocity;
    v.Y += Gravity * (1f / 20f);
    Velocity = v;

    bool wasOnGround = OnGround;
    float dx = Velocity.X * (1f / 20f);
    float dz = Velocity.Z * (1f / 20f);

    if (IsSneaking && wasOnGround)
    {
      float stepDown = -0.6f;
      bool xSafe = HasGroundAt(Position.X + dx, Position.Y, Position.Z, stepDown, getBlock);
      bool zSafe = HasGroundAt(Position.X, Position.Y, Position.Z + dz, stepDown, getBlock);
      bool bothSafe = HasGroundAt(Position.X + dx, Position.Y, Position.Z + dz, stepDown, getBlock);

      if (!bothSafe)
      {
        if (!xSafe) dx = 0;
        if (!zSafe) dz = 0;
      }
    }

    Move(dx, Velocity.Y * (1f / 20f), dz, getBlock);

    if (IsSneaking && wasOnGround && !OnGround)
    {
      var snappedPos = Position;
      float checkY = snappedPos.Y - 0.01f;

      var testX = new Vector3(snappedPos.X - dx, checkY, snappedPos.Z);
      if (IsOnGround(testX, getBlock)) { Position = testX; OnGround = true; goto done; }

      var testZ = new Vector3(snappedPos.X, checkY, snappedPos.Z - dz);
      if (IsOnGround(testZ, getBlock)) { Position = testZ; OnGround = true; goto done; }

      var testXZ = new Vector3(snappedPos.X - dx, checkY, snappedPos.Z - dz);
      if (IsOnGround(testXZ, getBlock)) { Position = testXZ; OnGround = true; }

    done:;
    }

    v = Velocity;
    v.X *= OnGround ? Drag : 0.91f;
    v.Z *= OnGround ? Drag : 0.91f;
    Velocity = v;

    // Landing cancelt Fly im Creative
    if (IsCreative && IsFlying && OnGround)
      IsFlying = false;
  }

  public void ProcessInput(KeyboardState keyboard, float yaw)
  {
    _pendingSneaking = keyboard.IsKeyDown(Keys.LeftShift) && !IsFlying && !IsSpectator;
    IsSprinting = keyboard.IsKeyDown(Keys.LeftControl) && !IsSneaking;

    float rad = MathHelper.DegreesToRadians(yaw + 90f);
    float sinY = MathF.Sin(rad);
    float cosY = MathF.Cos(rad);

    float fx = sinY, fz = -cosY;
    float rx = cosY, rz = sinY;

    float mx = 0, mz = 0, my = 0;
    if (keyboard.IsKeyDown(Keys.W)) { mx += fx; mz += fz; }
    if (keyboard.IsKeyDown(Keys.S)) { mx -= fx; mz -= fz; }
    if (keyboard.IsKeyDown(Keys.A)) { mx -= rx; mz -= rz; }
    if (keyboard.IsKeyDown(Keys.D)) { mx += rx; mz += rz; }

    // Fly / Spectator vertikale Bewegung
    if ((IsCreative && IsFlying) || IsSpectator)
    {
      if (keyboard.IsKeyDown(Keys.Space)) my += 1f;
      if (keyboard.IsKeyDown(Keys.LeftShift)) my -= 1f;
    }

    float len = MathF.Sqrt(mx * mx + mz * mz);
    if (len > 0.01f) { mx /= len; mz /= len; }

    float speed = IsSpectator || (IsCreative && IsFlying)
        ? (IsSprinting ? FlySprintSpeed : FlySpeed)
        : IsSneaking ? SneakSpeed
        : IsSprinting ? SprintSpeed
        : WalkSpeed;

    var vel = Velocity;
    if (IsSpectator || (IsCreative && IsFlying))
    {
      vel.X += mx * speed;
      vel.Y += my * speed;
      vel.Z += mz * speed;
    }
    else
    {
      vel.X += mx * speed * (OnGround ? 1f : 0.3f);
      vel.Z += mz * speed * (OnGround ? 1f : 0.3f);
    }
    Velocity = vel;
  }

  public void Jump()
  {
    if (IsFlying) return;        // Fliegt schon — Space macht nichts (Aufsteigen via ProcessInput)
    if (!OnGround) return;
    var vel = Velocity;
    vel.Y = JumpVelocity;
    Velocity = vel;
  }

  public void ToggleFly()
  {
    if (!IsCreative) return;
    IsFlying = !IsFlying;
    if (IsFlying)
    {
      var vel = Velocity;
      vel.Y = 0;
      Velocity = vel;
    }
  }

  private bool HasGroundAt(float x, float y, float z, float stepDown,
      Func<int, int, int, Block?> getBlock)
  {
    float hw = Width / 2f - 0.01f;
    int by = (int)MathF.Floor(y + stepDown);
    return getBlock((int)MathF.Floor(x - hw), by, (int)MathF.Floor(z - hw)).HasValue
        || getBlock((int)MathF.Floor(x + hw), by, (int)MathF.Floor(z - hw)).HasValue
        || getBlock((int)MathF.Floor(x - hw), by, (int)MathF.Floor(z + hw)).HasValue
        || getBlock((int)MathF.Floor(x + hw), by, (int)MathF.Floor(z + hw)).HasValue;
  }

  private bool IsOnGround(Vector3 pos, Func<int, int, int, Block?> getBlock)
  {
    float hw = Width / 2f;
    int y = (int)MathF.Floor(pos.Y - 0.01f);
    return getBlock((int)MathF.Floor(pos.X - hw), y, (int)MathF.Floor(pos.Z - hw)).HasValue
        || getBlock((int)MathF.Floor(pos.X + hw), y, (int)MathF.Floor(pos.Z - hw)).HasValue
        || getBlock((int)MathF.Floor(pos.X - hw), y, (int)MathF.Floor(pos.Z + hw)).HasValue
        || getBlock((int)MathF.Floor(pos.X + hw), y, (int)MathF.Floor(pos.Z + hw)).HasValue;
  }
}