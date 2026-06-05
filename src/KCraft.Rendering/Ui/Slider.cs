// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class Slider
{
  public float X { get; set; }
  public float Y { get; set; }
  public float Width { get; set; }
  public float Height { get; set; }

  public float Min { get; }
  public float Max { get; }
  public float Step { get; }
  public float Value { get; private set; }
  public string Label { get; set; }
  public bool Disabled { get; set; } = false;

  public event Action<float>? OnValueChanged;

  private bool _dragging = false;

  private static readonly Vector4 BgNormal = new(0x8B / 255f, 0x8B / 255f, 0x8B / 255f, 1f);
  private static readonly Vector4 BgDisabled = new(0x55 / 255f, 0x55 / 255f, 0x55 / 255f, 1f);
  private static readonly Vector4 BgHover = new(0xA0 / 255f, 0xA0 / 255f, 0xA0 / 255f, 1f);
  private static readonly Vector4 HandleCol = new(1f, 1f, 1f, 1f);
  private static readonly Vector4 TextCol = new(1f, 1f, 1f, 1f);
  private static readonly Vector4 TextDisCol = new(0.5f, 0.5f, 0.5f, 1f);
  private static readonly Vector4 BorderDark = new(0x37 / 255f, 0x37 / 255f, 0x37 / 255f, 1f);
  private static readonly Vector4 BorderLight = new(1f, 1f, 1f, 1f);

  public Slider(string label, float min, float max, float value, float step = 1f)
  {
    Label = label;
    Min = min;
    Max = max;
    Step = step;
    Value = Math.Clamp(value, min, max);
  }

  public void Draw(TextRenderer text, Vector2 screen, float mouseX, float mouseY)
  {
    bool hover = !Disabled && mouseX >= X && mouseX <= X + Width
                            && mouseY >= Y && mouseY <= Y + Height;

    // Border
    text.DrawRect(X - 1, Y - 1, Width + 2, 1, screen, BorderDark);
    text.DrawRect(X - 1, Y - 1, 1, Height + 2, screen, BorderDark);
    text.DrawRect(X - 1, Y + Height, Width + 2, 1, screen, BorderLight);
    text.DrawRect(X + Width, Y - 1, 1, Height + 2, screen, BorderLight);

    // Background
    var bg = Disabled ? BgDisabled : hover || _dragging ? BgHover : BgNormal;
    text.DrawRect(X, Y, Width, Height, screen, bg);

    // Handle — vertikale Linie bei Value-Position
    float t = (Value - Min) / (Max - Min);
    float handleX = X + t * (Width - 4f);
    float handleW = 4f;
    text.DrawRect(handleX, Y, handleW, Height, screen, HandleCol);

    // Label zentriert im Balken
    string displayText = $"{Label}: {(Step < 1f ? Value.ToString("F1") : ((int)Value).ToString())}";
    float scale = UiScale.Scale;
    float tw = text.MeasureTextWidth(displayText, scale * 0.85f);
    float tx = X + (Width - tw) / 2f;
    float ty = Y + (Height - 8f * scale) / 2f;
    text.DrawText(displayText, tx, ty, screen,
        scale: scale * 0.85f,
        color: Disabled ? TextDisCol : TextCol);
  }

  public bool OnMouseClick(float mx, float my)
      => !Disabled && mx >= X && mx <= X + Width
                   && my >= Y && my <= Y + Height;

  public void HandleClick(float mx, float my)
  {
    if (Disabled) return;
    if (OnMouseClick(mx, my))
    {
      _dragging = true;
      SetFromMouse(mx);
    }
  }

  public void HandleMouseMove(float mx, float my)
  {
    if (_dragging)
      SetFromMouse(mx);
  }

  public void HandleMouseUp()
  {
    _dragging = false;
  }

  private void SetFromMouse(float mx)
  {
    float t = Math.Clamp((mx - X) / Width, 0f, 1f);
    float raw = Min + t * (Max - Min);
    float stepped = MathF.Round(raw / Step) * Step;
    float newVal = Math.Clamp(stepped, Min, Max);
    if (newVal != Value)
    {
      Value = newVal;
      OnValueChanged?.Invoke(Value);
    }
  }

  public void SetValue(float value)
  {
    Value = Math.Clamp(value, Min, Max);
  }
}