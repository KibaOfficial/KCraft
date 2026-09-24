// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public readonly record struct FramebufferRect(
  int X,
  int Y,
  int Width,
  int Height);

public static class UiFramebuffer
{
  public static int[] GetViewport()
  {
    int[] viewport = new int[4];
    GL.GetInteger(GetPName.Viewport, viewport);
    return viewport;
  }

  public static FramebufferRect ToFramebufferRect(
    Vector2 screen,
    float x,
    float y,
    float width,
    float height,
    int[] viewport)
  {
    float scaleX = viewport[2] / screen.X;
    float scaleY = viewport[3] / screen.Y;

    return new FramebufferRect(
      viewport[0] + (int)MathF.Round(x * scaleX),
      viewport[1] + (int)MathF.Round((screen.Y - y - height) * scaleY),
      (int)MathF.Round(width * scaleX),
      (int)MathF.Round(height * scaleY));
  }

  public static void RestoreViewport(int[] viewport)
  {
    GL.Viewport(
      viewport[0],
      viewport[1],
      viewport[2],
      viewport[3]);
  }
}