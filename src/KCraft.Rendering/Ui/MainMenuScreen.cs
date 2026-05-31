// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class MainMenuScreen : Screen
{
  public event Action? OnSingleplayer;
  public event Action? OnQuit;
  public event Action? OnOptions;

  private static readonly Vector4 Title = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Subtitle = new(0.7f, 0.7f, 0.7f, 1.0f);
  private static readonly Vector4 Bg = new(0.0f, 0.0f, 0.0f, 0.55f);

  public float _mouseX, _mouseY;

  public MainMenuScreen(TextRenderer text) : base(text)
  {
    var sp = new Button("Singleplayer", 0, 0, 400, 40);
    var mp = new Button("Multiplayer", 0, 0, 400, 40) { Disabled = true };
    var opts = new Button("Options", 0, 0, 196, 40) { Disabled = true };
    var quit = new Button("Quit Game", 0, 0, 196, 40);

    sp.OnClick += () => OnSingleplayer?.Invoke();
    quit.OnClick += () => OnQuit?.Invoke();
    opts.Disabled = false; // TODO: Remove when options are implemented
    opts.OnClick += () => OnOptions?.Invoke();

    Buttons.AddRange([sp, mp, opts, quit]);
  }

  public override void Layout(Vector2 screen)
  {
    float centerX = screen.X / 2f;
    float centerY = screen.Y / 2f;

    // Singleplayer
    Buttons[0].X = centerX - 200; Buttons[0].Y = centerY - 60;
    // Multiplayer
    Buttons[1].X = centerX - 200; Buttons[1].Y = centerY - 10;
    // Options (links)
    Buttons[2].X = centerX - 200; Buttons[2].Y = centerY + 50;
    // Quit (rechts)
    Buttons[3].X = centerX + 4; Buttons[3].Y = centerY + 50;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX; _mouseY = mouseY;

    // Vollbild-Hintergrund
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    // Titel
    float titleScale = 4f;
    string titleText = "KCraft";
    float tw = Text.MeasureTextWidth(titleText, titleScale);
    Text.DrawText(titleText, (screen.X - tw) / 2f, screen.Y / 2f - 150f,
      screen, scale: titleScale, color: Title);

    // Subtitle
    string sub = "The KibaOfficial Minecraft Clone";
    float sw = Text.MeasureTextWidth(sub);
    Text.DrawText(sub, (screen.X - sw) / 2f, screen.Y / 2f - 100f,
      screen, color: Subtitle);

    // Buttons
    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  public override void HandleMouseMove(float mx, float my)
  {
    _mouseX = mx; _mouseY = my;
  }
}
