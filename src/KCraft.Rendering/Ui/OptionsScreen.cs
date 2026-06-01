// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class OptionsScreen : Screen
{
  public event Action? OnBack;
  public event Action? OnBenchmark;

  private static readonly Vector4 Bg = new(0.0f, 0.0f, 0.0f, 0.55f);
  private static readonly Vector4 Title = new(1f, 1f, 1f, 1f);

  public OptionsScreen(TextRenderer text) : base(text)
  {
    var fov = new Button("FOV: Normal", 0, 0, 200, 40) { Disabled = true };
    var online = new Button("Online...", 0, 0, 200, 40) { Disabled = true };

    var skin = new Button("Skin Customization...", 0, 0, 200, 40) { Disabled = true };
    var sounds = new Button("Music & Sounds...", 0, 0, 200, 40) { Disabled = true };

    var video = new Button("Video Settings...", 0, 0, 200, 40) { Disabled = true };
    var controls = new Button("Controls...", 0, 0, 200, 40) { Disabled = true };

    var guiScale = new Button("GUI Scale: 1x", 0, 0, 200, 40);
    var benchmark = new Button("Benchmark...", 0, 0, 200, 40);

    var resource = new Button("Resource Packs...", 0, 0, 200, 40) { Disabled = true };
    var accessibility = new Button("Accessibility...", 0, 0, 200, 40) { Disabled = true };

    var done = new Button("Done", 0, 0, 300, 40);

    guiScale.OnClick += () =>
    {
      UiScale.CycleUp();
      guiScale.Text = $"GUI Scale: {(int)UiScale.Scale}x";
    };

    benchmark.OnClick += () => OnBenchmark?.Invoke();
    done.OnClick += () => OnBack?.Invoke();

    Buttons.AddRange([
      fov, online,
      skin, sounds,
      video, controls,
      guiScale, benchmark,
      resource, accessibility,
      done
    ]);
  }

  public override void Layout(Vector2 screen)
  {
    float buttonW = 272f;
    float buttonH = 34f;
    float gapX = 16f;
    float gapY = 10f;

    float startY = 76f;

    float leftX = screen.X / 2f - buttonW - gapX / 2f;
    float rightX = screen.X / 2f + gapX / 2f;

    for (int row = 0; row < 5; row++)
    {
      int left = row * 2;
      int right = left + 1;

      float y = startY + row * (buttonH + gapY);

      Buttons[left].X = leftX;
      Buttons[left].Y = y;
      Buttons[left].Width = buttonW;
      Buttons[left].Height = buttonH;

      Buttons[right].X = rightX;
      Buttons[right].Y = y;
      Buttons[right].Width = buttonW;
      Buttons[right].Height = buttonH;
    }

    Buttons[10].X = screen.X / 2f - buttonW / 2f;
    Buttons[10].Y = screen.Y - 64f;
    Buttons[10].Width = buttonW;
    Buttons[10].Height = buttonH;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    Buttons[6].Text = $"GUI Scale: {(int)UiScale.Scale}x";

    float scale = UiScale.Scale;
    string title = "Options";
    float titleScale = scale * 1.25f;
    float tw = Text.MeasureTextWidth(title, titleScale);

    Text.DrawText(title, (screen.X - tw) / 2f, 18f,
      screen, scale: titleScale, color: Title);

    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }
}