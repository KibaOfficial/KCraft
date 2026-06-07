// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class TextInput
{
  // ── Layout ────────────────────────────────────────────────────────────
  public float X { get; set; }
  public float Y { get; set; }
  public float Width { get; set; }
  public float Height { get; set; }

  // ── State ─────────────────────────────────────────────────────────────
  public string Value { get; private set; } = "";
  public string Placeholder { get; set; } = "";
  public int MaxLength { get; set; } = 32;
  public bool IsFocused { get; private set; } = false;
  public bool IsDisabled { get; set; } = false;

  // ── Cursor ────────────────────────────────────────────────────────────
  private float _cursorTimer = 0f;
  private bool _cursorVisible = true;

  // ── Farben (MC-style) ─────────────────────────────────────────────────
  private static readonly Vector4 BgNormal = new(0.10f, 0.10f, 0.10f, 0.90f);
  private static readonly Vector4 BgFocused = new(0.15f, 0.15f, 0.15f, 0.95f);
  private static readonly Vector4 BgDisabled = new(0.07f, 0.07f, 0.07f, 0.80f);
  private static readonly Vector4 BorderNormal = new(0.50f, 0.50f, 0.50f, 1.00f);
  private static readonly Vector4 BorderFocused = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 TextColor = new(0.88f, 0.88f, 0.88f, 1.00f);
  private static readonly Vector4 PlaceholderColor = new(0.45f, 0.45f, 0.45f, 1.00f);
  private static readonly Vector4 CursorColor = new(1.00f, 1.00f, 1.00f, 1.00f);

  private const float BorderWidth = 1f;
  private const float TextPadding = 6f;

  public TextInput(float x, float y, float width, float height,
      string placeholder = "", int maxLength = 32)
  {
    X = x; Y = y; Width = width; Height = height;
    Placeholder = placeholder;
    MaxLength = maxLength;
  }

  // ── Hit Test ──────────────────────────────────────────────────────────
  public bool Contains(float mx, float my)
  {
    float s = UiScale.Scale;
    float sx = X * s;
    float sy = Y * s;
    float sw = Width * s;
    float sh = Height * s;

    return mx >= sx && mx <= sx + sw
        && my >= sy && my <= sy + sh;
  }

  public void HandleClick(float mx, float my)
  {
    if (IsDisabled) return;
    IsFocused = Contains(mx, my);
  }

  public void Blur() => IsFocused = false;

  // ── Keyboard Input ────────────────────────────────────────────────────
  public void HandleKeyDown(Keys key, bool shift)
  {
    if (!IsFocused || IsDisabled) return;

    switch (key)
    {
      case Keys.Backspace:
        if (Value.Length > 0)
          Value = Value[..^1];
        break;

      case Keys.Escape:
        IsFocused = false;
        break;
    }
  }

  public void HandleTextInput(char c)
  {
    if (!IsFocused || IsDisabled) return;
    if (Value.Length >= MaxLength) return;
    if (c < 32 || c > 126) return; // nur druckbare ASCII
    Value += c;
  }

  public void SetValue(string value) => Value = value[..Math.Min(value.Length, MaxLength)];

  // ── Update ────────────────────────────────────────────────────────────
  public void Update(float deltaTime)
  {
    if (!IsFocused) { _cursorVisible = false; return; }
    _cursorTimer += deltaTime;
    if (_cursorTimer >= 0.53f) // MC Cursor blink speed
    {
      _cursorTimer = 0f;
      _cursorVisible = !_cursorVisible;
    }
  }

  // ── Draw ──────────────────────────────────────────────────────────────
  public void Draw(TextRenderer text, Vector2 screen)
  {
    float scale = UiScale.Scale;
    float border = BorderWidth * scale;
    float pad = TextPadding * scale;

    var bg = IsDisabled ? BgDisabled : IsFocused ? BgFocused : BgNormal;
    var border2 = IsFocused ? BorderFocused : BorderNormal;

    float sx = X * scale;
    float sy = Y * scale;
    float sw = Width * scale;
    float sh = Height * scale;

    // Border
    text.DrawRect(sx - border, sy - border,
        sw + border * 2, sh + border * 2, screen, border2);

    // Background
    text.DrawRect(sx, sy, sw, sh, screen, bg);

    // Text oder Placeholder
    bool hasValue = Value.Length > 0;
    float availableWidth = Math.Max(0f, sw - pad * 2f - 4f);

    string display = hasValue
      ? FitTextFromEnd(text, Value, scale, availableWidth)
      : FitTextFromStart(text, Placeholder, scale, availableWidth);

    var col = hasValue ? TextColor : PlaceholderColor;

    float textY = sy + (sh - 8f * scale) / 2f;
    text.DrawText(display, sx + pad, textY, screen, scale: scale, color: col);

    // Cursor
    if (IsFocused && _cursorVisible)
    {
      float cursorX = sx + pad + (hasValue
          ? text.MeasureTextWidth(display, scale)
          : 0f);

      float cursorH = 8f * scale;
      text.DrawRect(cursorX, textY, border, cursorH, screen, CursorColor);
    }
  }

  private static string FitTextFromStart(TextRenderer text, string value, float scale, float maxWidth)
  {
    while (value.Length > 0 && text.MeasureTextWidth(value, scale) > maxWidth)
      value = value[..^1];

    return value;
  }

  private static string FitTextFromEnd(TextRenderer text, string value, float scale, float maxWidth)
  {
    while (value.Length > 0 && text.MeasureTextWidth(value, scale) > maxWidth)
      value = value[1..];

    return value;
  }
}
