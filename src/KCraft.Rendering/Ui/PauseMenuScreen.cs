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

  public PauseMenuScreen(TextRenderer text) : base(text)
  {
    var resume = new Button("Back to Game", 0, 0, 300, 40);
    var opts = new Button("Options", 0, 0, 300, 40) { Disabled = true };
    var quit = new Button("Quit to Title", 0, 0, 300, 40);

    resume.OnClick += () => OnResume?.Invoke();
    quit.OnClick += () => OnQuitToTitle?.Invoke();
    opts.Disabled = false;
    opts.OnClick += () => OnOptions?.Invoke();

    Buttons.AddRange([resume, opts, quit]);
  }

  public override void Layout(Vector2 screen)
  {
    const float panelWidth = 340;
    const float panelHeight = 200;
    float panelX = (screen.X - panelWidth) / 2f;
    float panelY = (screen.Y - panelHeight) / 2f - 20f;
    float buttonX = panelX + 24f;
    float buttonY = panelY + 62f;

    Buttons[0].X = buttonX; Buttons[0].Y = buttonY;
    Buttons[1].X = buttonX; Buttons[1].Y = buttonY + 50f;
    Buttons[2].X = buttonX; Buttons[2].Y = buttonY + 108f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX; _mouseY = mouseY;

    // Dimmed Overlay
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    // Panel
    float panelWidth = 340, panelHeight = 200;
    float panelX = (screen.X - panelWidth) / 2f;
    float panelY = (screen.Y - panelHeight) / 2f - 20f;
    Text.DrawRect(panelX, panelY, panelWidth, panelHeight, screen, Panel);

    // Titel
    string titleText = "Game Paused";
    float tw = Text.MeasureTextWidth(titleText, 2f);
    Text.DrawText(titleText, (screen.X - tw) / 2f, panelY + 16f,
      screen, color: Title);

    // Buttons
    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  public override void HandleMouseMove(float mx, float my)
  {
    _mouseX = mx; _mouseY = my;
  }
}
