// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class CrosshairRenderer : IDisposable
{
  private readonly TextRenderer _text;

  private static readonly Vector4 Color = new(1f, 1f, 1f, 0.85f);

  public CrosshairRenderer(string fontPath)
  {
    _text = new TextRenderer(fontPath);
  }

  public void Draw(Vector2 screen)
  {
    float cx = screen.X / 2f;
    float cy = screen.Y / 2f;
    const float len = 8f;
    const float thick = 2f;

    // Horizontale Linie
    _text.DrawRect(cx - len, cy - thick / 2f, len * 2f, thick, screen, Color);
    // Vertikale Linie
    _text.DrawRect(cx - thick / 2f, cy - len, thick, len * 2f, screen, Color);
  }

  public void Dispose() => _text.Dispose();
}