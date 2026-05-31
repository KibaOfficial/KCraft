// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class OptionsScreen : Screen
{
  public event Action? OnBack;

  private static readonly Vector4 Bg = new(0.0f, 0.0f, 0.0f, 0.55f);
  private static readonly Vector4 Title = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Panel = new(0.1f, 0.1f, 0.1f, 0.85f);

  public float _mouseX, _mouseY;

  public OptionsScreen(TextRenderer text) : base(text)
  {
    var scaleUp = new Button("GUI Scale +", 0, 0, 140, 40);
    var scaleDown = new Button("GUI Scale -", 0, 0, 140, 40);
    var back = new Button("Back", 0, 0, 300, 40);

    scaleUp.OnClick += () => UiScale.CycleUp();
    scaleDown.OnClick += () => UiScale.CycleDown();
    back.OnClick += () => OnBack?.Invoke();

    Buttons.AddRange([scaleUp, scaleDown, back]);
  }

  public override void Layout(Vector2 screen)
  {
    float cx = screen.X / 2f;
    float cy = screen.Y / 2f;

    Buttons[0].X = cx - 148f; Buttons[0].Y = cy - 20f;
    Buttons[1].X = cx + 8f; Buttons[1].Y = cy - 20f;
    Buttons[2].X = cx - 150f; Buttons[2].Y = cy + 40f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX; _mouseY = mouseY;

    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    float pw = 340f, ph = 180f;
    float px = (screen.X - pw) / 2f;
    float py = (screen.Y - ph) / 2f - 20f;
    Text.DrawRect(px, py, pw, ph, screen, Panel);

    string title = "Options";
    float tw = Text.MeasureTextWidth(title, 2f);
    Text.DrawText(title, (screen.X - tw) / 2f, py + 16f, screen, color: Title);

    string scaleText = $"GUI Scale: {(int)UiScale.Scale}x";
    float stw = Text.MeasureTextWidth(scaleText, 2f);
    Text.DrawText(scaleText, (screen.X - stw) / 2f, py + 50f, screen, color: Title);

    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  public override void HandleMouseMove(float mx, float my)
  {
    _mouseX = mx; _mouseY = my;
  }
}