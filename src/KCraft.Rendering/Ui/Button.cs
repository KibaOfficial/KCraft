// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class Button
{
  public string Text     { get; set; }
  public float  X        { get; set; }
  public float  Y        { get; set; }
  public float  Width    { get; set; }
  public float  Height   { get; set; }
  public bool   Disabled { get; set; } = false;

  public event Action? OnClick;

  private static readonly Vector4 ColorNormal   = new(0.25f, 0.25f, 0.25f, 0.85f);
  private static readonly Vector4 ColorHover    = new(0.40f, 0.40f, 0.40f, 0.90f);
  private static readonly Vector4 ColorDisabled = new(0.15f, 0.15f, 0.15f, 0.70f);
  private static readonly Vector4 ColorBorder   = new(0.55f, 0.55f, 0.55f, 1.00f);
  private static readonly Vector4 TextColor     = new(1.0f,  1.0f,  1.0f,  1.0f);
  private static readonly Vector4 TextDisabled  = new(0.45f, 0.45f, 0.45f, 1.0f);

  public Button(string text, float x, float y, float width, float height)
  {
    Text = text; X = x; Y = y; Width = width; Height = height;
  }

  public bool OnMouseClick(float mouseX, float mouseY)
    => !Disabled && mouseX >= X && mouseX <= X + Width && mouseY >= Y && mouseY <= Y + Height;

  public void Draw(TextRenderer text, Vector2 screen, bool isHover)
  {
    var bg     = Disabled ? ColorDisabled : isHover ? ColorHover : ColorNormal;
    var textC  = Disabled ? TextDisabled : TextColor;

    // Border (1px größer)
    text.DrawRect(X - 1, Y - 1, Width + 2, Height + 2, screen, ColorBorder);
    // Background
    text.DrawRect(X, Y, Width, Height, screen, bg);

    // Text zentriert
    float tw = text.MeasureTextWidth(Text);
    float tx = X + (Width  - tw) / 2f;
    float ty = Y + (Height - 16f) / 2f; // 16 = Glyph-Höhe bei scale=2
    text.DrawText(Text, tx, ty, screen, color: textC);
  }

  public void Click() => OnClick?.Invoke();
}
