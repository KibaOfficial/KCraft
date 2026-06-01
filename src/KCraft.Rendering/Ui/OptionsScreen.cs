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
    float panelW = 360f;
    float panelH = 210f;
    float panelX = (screen.X - panelW) / 2f;
    float panelY = (screen.Y - panelH) / 2f;
    float pad = 24f;
    float gap = 12f;
    float buttonW = (panelW - pad * 2f - gap) / 2f;

    Buttons[0].X = panelX + pad; Buttons[0].Y = panelY + 98f; Buttons[0].Width = buttonW;
    Buttons[1].X = panelX + pad + buttonW + gap; Buttons[1].Y = panelY + 98f; Buttons[1].Width = buttonW;
    Buttons[2].X = panelX + pad; Buttons[2].Y = panelY + 150f; Buttons[2].Width = panelW - pad * 2f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX; _mouseY = mouseY;

    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    float scale = UiScale.Scale;
    float pw = 360f, ph = 210f;
    float px = (screen.X - pw) / 2f;
    float py = (screen.Y - ph) / 2f;
    Text.DrawRect(px, py, pw, ph, screen, Panel);

    string title = "Options";
    float titleScale = scale * 1.5f;
    float tw = Text.MeasureTextWidth(title, titleScale);
    Text.DrawText(title, (screen.X - tw) / 2f, py + 18f, screen, scale: titleScale, color: Title);

    string scaleText = $"GUI Scale: {(int)UiScale.Scale}x";
    float stw = Text.MeasureTextWidth(scaleText, scale);
    Text.DrawText(scaleText, (screen.X - stw) / 2f, py + 62f, screen, scale: scale, color: Title);

    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  public override void HandleMouseMove(float mx, float my)
  {
    _mouseX = mx; _mouseY = my;
  }
}
