// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class Button
{
  public string Text { get; set; }
  public float X { get; set; }
  public float Y { get; set; }
  public float Width { get; set; }
  public float Height { get; set; }
  public bool Disabled { get; set; } = false;

  public event Action? OnClick;

  private static readonly Vector4 ColorNormal = new(0x70 / 255f, 0x70 / 255f, 0x70 / 255f, 0.85f);
  private static readonly Vector4 ColorHover = new(0x8B / 255f, 0x8B / 255f, 0x8B / 255f, 0.90f);
  private static readonly Vector4 ColorDisabled = new(0x4A / 255f, 0x4A / 255f, 0x4A / 255f, 0.70f);
  private static readonly Vector4 ColorBorder = new(0xA0 / 255f, 0xA0 / 255f, 0xA0 / 255f, 1.00f);
  private static readonly Vector4 TextColor = new(0xE0 / 255f, 0xE0 / 255f, 0xE0 / 255f, 1.0f);
  private static readonly Vector4 TextHover = new(1.0f, 1.0f, 0x55 / 255f, 1.0f); // Gelb!
  private static readonly Vector4 TextDisabled = new(0xA0 / 255f, 0xA0 / 255f, 0xA0 / 255f, 1.0f);

  public Button(string text, float x, float y, float width, float height)
  {
    Text = text; X = x; Y = y; Width = width; Height = height;
  }

  public bool OnMouseClick(float mouseX, float mouseY)
  {
    if (Disabled) return false;

    float s = UiScale.Scale;
    float sx = X * s;
    float sy = Y * s;
    float sw = Width * s;
    float sh = Height * s;

    return mouseX >= sx && mouseX <= sx + sw
        && mouseY >= sy && mouseY <= sy + sh;
  }

  public void Draw(TextRenderer text, Vector2 screen, bool isHover)
  {
    float s = UiScale.Scale;

    float sx = X * s;
    float sy = Y * s;
    float sw = Width * s;
    float sh = Height * s;

    float textScale = s;
    var bg = Disabled ? ColorDisabled : isHover ? ColorHover : ColorNormal;
    var textC = Disabled ? TextDisabled : isHover ? TextHover : TextColor;

    text.DrawRect(sx - s, sy - s, sw + 2 * s, sh + 2 * s, screen, ColorBorder);
    text.DrawRect(sx, sy, sw, sh, screen, bg);

    float tw = text.MeasureTextWidth(Text, textScale);
    float tx = sx + (sw - tw) / 2f;
    float ty = sy + (sh - 8f * textScale) / 2f;

    text.DrawText(Text, tx, ty, screen, scale: textScale, color: textC);
  }

  public void Click() => OnClick?.Invoke();
}
