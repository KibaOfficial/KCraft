// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering;

public sealed class Camera
{
  public Vector3 Position { get; set; }
  public float Yaw { get; private set; } = -90f;
  public float Pitch { get; private set; } = 0f;
  public float Speed { get; set; } = 10f;
  public float Sensitivity { get; set; } = 0.1f;

  private Vector3 _front = -Vector3.UnitZ;
  public Vector3 Front => _front;
  private Vector3 _up = Vector3.UnitY;
  private Vector3 _right = Vector3.UnitX;
  public Vector3 Right => _right;
  public Vector3 Up => _up;

  public Camera(Vector3 position)
  {
    Position = position;
    UpdateVectors();
  }

  public Matrix4 GetViewMatrix()
    => Matrix4.LookAt(Position, Position + _front, _up);

  public void ProcessKeyboard(KeyboardState keyboard, float deltaTime)
  {
    float velocity = Speed * deltaTime;
    if (keyboard.IsKeyDown(Keys.W)) Position += _front * velocity;
    if (keyboard.IsKeyDown(Keys.S)) Position -= _front * velocity;
    if (keyboard.IsKeyDown(Keys.A)) Position -= _right * velocity;
    if (keyboard.IsKeyDown(Keys.D)) Position += _right * velocity;
    if (keyboard.IsKeyDown(Keys.Space)) Position += _up * velocity;
    if (keyboard.IsKeyDown(Keys.LeftShift)) Position -= _up * velocity;
  }

  public void ProcessMouse(float deltaX, float deltaY)
  {
    Yaw += deltaX * Sensitivity;
    Pitch -= deltaY * Sensitivity;
    Pitch = Math.Clamp(Pitch, -89f, 89f);
    UpdateVectors();
  }

  private void UpdateVectors()
  {
    var front = new Vector3(
      MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch)),
      MathF.Sin(MathHelper.DegreesToRadians(Pitch)),
      MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch))
    );
    _front = Vector3.Normalize(front);
    _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
    _up = Vector3.Normalize(Vector3.Cross(_right, _front));
  }

  public void SetRotation(float yaw, float pitch)
  {
    Yaw = yaw;
    Pitch = Math.Clamp(pitch, -89f, 89f);
    UpdateVectors();
  }
}