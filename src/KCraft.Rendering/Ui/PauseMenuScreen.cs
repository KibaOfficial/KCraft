// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class PauseMenuScreen : Screen
{
  public event Action? OnResume;
  public event Action? OnQuitToTitle;
  public event Action? OnOptions;

  private static readonly Vector4 Bg = new(0.0f, 0.0f, 0.0f, 0.45f);
  private static readonly Vector4 Title = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Panel = new(0.1f, 0.1f, 0.1f, 0.80f);

  public float _mouseX, _mouseY;
  private float _panelX, _panelY, _panelW, _panelH;

  public PauseMenuScreen(TextRenderer text) : base(text)
  {
    var resume = new Button("Return", 0, 0, 300, 40);
    var opts = new Button("Options", 0, 0, 300, 40) { Disabled = true };
    var quit = new Button("Save & Quit", 0, 0, 300, 40);

    resume.OnClick += () => OnResume?.Invoke();
    quit.OnClick += () => OnQuitToTitle?.Invoke();
    opts.Disabled = false;
    opts.OnClick += () => OnOptions?.Invoke();

    Buttons.AddRange([resume, opts, quit]);
  }

  public override void Layout(Vector2 screen)
  {
    _panelW = 340f;
    _panelH = 240f;

    _panelX = (screen.X - _panelW) / 2f;
    _panelY = (screen.Y - _panelH) / 2f - 20f;

    float buttonX = _panelX + 24f;
    float buttonY = _panelY + 76f;

    Buttons[0].X = buttonX; Buttons[0].Y = buttonY;
    Buttons[1].X = buttonX; Buttons[1].Y = buttonY + 50f;
    Buttons[2].X = buttonX; Buttons[2].Y = buttonY + 100f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX; _mouseY = mouseY;

    float s = UiScale.Scale;

    float spx = _panelX * s;
    float spy = _panelY * s;
    float spw = _panelW * s;
    float sph = _panelH * s;

    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);
    Text.DrawRect(spx, spy, spw, sph, screen, Panel);

    string titleText = "Game Paused";
    float titleScale = 2f * s;
    float tw = Text.MeasureTextWidth(titleText, titleScale);

    Text.DrawText(titleText,
      spx + (spw - tw) / 2f,
      spy + 18f * s,
      screen,
      scale: titleScale,
      color: Title);

    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  public override void HandleMouseMove(float mx, float my)
  {
    _mouseX = mx; _mouseY = my;
  }
}
