// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering;

public sealed class GameplayController
{
  private float _jumpPressTimer;
  private bool _jumpPressedLastFrame;

  public bool FreeCam { get; private set; }

  public void Update(
    float deltaTime,
    KeyboardState keyboard,
    WorldTicker ticker,
    Camera camera)
  {
    HandleJumpAndFlight(
      deltaTime,
      keyboard,
      ticker.Player);

    if (FreeCam)
    {
      camera.ProcessKeyboard(
        keyboard,
        deltaTime);
    }
    else
    {
      ticker.Player?.ProcessInput(
        keyboard,
        camera.Yaw);
    }
  }

  public void ToggleFreeCam(
    Camera camera,
    Player? player)
  {
    FreeCam = !FreeCam;

    if (!FreeCam && player != null)
      camera.Position = player.EyePosition;
  }

  private void HandleJumpAndFlight(
    float deltaTime,
    KeyboardState keyboard,
    Player? player)
  {
    bool jumpNow = keyboard.IsKeyDown(Keys.Space);

    if (jumpNow && !_jumpPressedLastFrame)
    {
      if (player is { IsInWater: true })
      {
        _jumpPressTimer = 0f;
        player.Jump();
      }
      else if (_jumpPressTimer > 0f)
      {
        player?.ToggleFly();
        _jumpPressTimer = 0f;
      }
      else
      {
        _jumpPressTimer = 0.3f;
        player?.Jump();
      }
    }

    if (_jumpPressTimer > 0f)
      _jumpPressTimer -= deltaTime;

    _jumpPressedLastFrame = jumpNow;
  }
}