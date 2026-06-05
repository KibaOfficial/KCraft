// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class OptionsScreen : Screen
{
  public event Action? OnBack;
  public event Action? OnBenchmark;

  private enum Page { Main, Video, Sound, Controls, }
  private Page _currentPage = Page.Main;
  private Page _previousPage = Page.Main;

  private static readonly Vector4 Bg = new(0f, 0f, 0f, 0.55f);
  private static readonly Vector4 White = new(1f, 1f, 1f, 1f);
  private static readonly Vector4 Gray = new(0.6f, 0.6f, 0.6f, 1f);

  // Sliders
  private readonly Slider _renderDistSlider;
  private readonly Slider _fovSlider;

  // Shared
  private readonly Button _doneBtn;

  // Main Page Buttons
  private readonly Button _videoBtn;
  private readonly Button _soundBtn;
  private readonly Button _controlsBtn;
  private readonly Button _guiScaleBtn;
  private readonly Button _benchmarkBtn;
  private readonly Button _onlineBtn;
  private readonly Button _skinBtn;
  private readonly Button _resourceBtn;
  private readonly Button _accessibilityBtn;

  private Vector2 _screen;
  private float _buttonW, _buttonH, _gapX, _gapY, _startY;

  public OptionsScreen(TextRenderer text) : base(text)
  {
    // Sliders
    _renderDistSlider = new Slider("Render Distance", 4, 16, GameSettings.RenderDistance, 2f);
    _renderDistSlider.OnValueChanged += v => GameSettings.RenderDistance = (int)v;

    _fovSlider = new Slider("FOV", 60, 110, GameSettings.Fov, 5f);
    _fovSlider.OnValueChanged += v => GameSettings.Fov = v;

    // Shared
    _doneBtn = new Button("Done", 0, 0, 200, 34);
    _doneBtn.OnClick += () =>
    {
      if (_currentPage == Page.Main)
        OnBack?.Invoke();
      else
      {
        _currentPage = _previousPage;
        _previousPage = Page.Main;
      }
    };

    // Main Page
    _videoBtn = new Button("Video Settings...", 0, 0, 200, 34);
    _videoBtn.OnClick += () => NavigateTo(Page.Video);

    _soundBtn = new Button("Music & Sounds...", 0, 0, 200, 34) { Disabled = true };
    _controlsBtn = new Button("Controls...", 0, 0, 200, 34) { Disabled = true };
    _onlineBtn = new Button("Online...", 0, 0, 200, 34) { Disabled = true };
    _skinBtn = new Button("Skin Customization...", 0, 0, 200, 34) { Disabled = true };
    _resourceBtn = new Button("Resource Packs...", 0, 0, 200, 34) { Disabled = true };
    _accessibilityBtn = new Button("Accessibility...", 0, 0, 200, 34) { Disabled = true };

    _guiScaleBtn = new Button($"GUI Scale: {(int)UiScale.Scale}x", 0, 0, 200, 34);
    _guiScaleBtn.OnClick += () =>
    {
      UiScale.CycleUp();
      _guiScaleBtn.Text = $"GUI Scale: {(int)UiScale.Scale}x";
    };

    _benchmarkBtn = new Button("Benchmark...", 0, 0, 200, 34);
    _benchmarkBtn.OnClick += () => OnBenchmark?.Invoke();

    // Buttons Liste für Base-Class Handling
    Buttons.AddRange([
        _videoBtn, _onlineBtn,
            _skinBtn, _soundBtn,
            _controlsBtn, _guiScaleBtn,
            _resourceBtn, _accessibilityBtn,
            _benchmarkBtn,
            _doneBtn,
        ]);
  }

  private void NavigateTo(Page page)
  {
    _previousPage = _currentPage;
    _currentPage = page;
  }

  public override void Layout(Vector2 screen)
  {
    _screen = screen;
    _buttonW = 272f;
    _buttonH = 34f;
    _gapX = 16f;
    _gapY = 10f;
    _startY = 76f;

    float leftX = screen.X / 2f - _buttonW - _gapX / 2f;
    float rightX = screen.X / 2f + _gapX / 2f;
    float fullW = _buttonW * 2f + _gapX;
    float fullX = screen.X / 2f - fullW / 2f;

    // Main Page Buttons (4 rows × 2 + 1 row benchmark)
    Button[][] mainRows =
    [
        [_videoBtn,    _onlineBtn],
            [_skinBtn,     _soundBtn],
            [_controlsBtn, _guiScaleBtn],
            [_resourceBtn, _accessibilityBtn],
        ];

    for (int row = 0; row < mainRows.Length; row++)
    {
      float y = _startY + row * (_buttonH + _gapY);
      mainRows[row][0].X = leftX; mainRows[row][0].Y = y;
      mainRows[row][0].Width = _buttonW; mainRows[row][0].Height = _buttonH;
      mainRows[row][1].X = rightX; mainRows[row][1].Y = y;
      mainRows[row][1].Width = _buttonW; mainRows[row][1].Height = _buttonH;
    }

    // Benchmark — full width
    float benchY = _startY + mainRows.Length * (_buttonH + _gapY);
    _benchmarkBtn.X = fullX; _benchmarkBtn.Y = benchY;
    _benchmarkBtn.Width = fullW; _benchmarkBtn.Height = _buttonH;

    // Done — immer unten zentriert
    _doneBtn.X = screen.X / 2f - _buttonW / 2f;
    _doneBtn.Y = screen.Y - 64f;
    _doneBtn.Width = _buttonW;
    _doneBtn.Height = _buttonH;

    // Sliders — Video Page
    float sliderW = fullW;
    float sliderH = _buttonH;
    _renderDistSlider.X = fullX; _renderDistSlider.Y = _startY;
    _renderDistSlider.Width = sliderW; _renderDistSlider.Height = sliderH;

    _fovSlider.X = fullX; _fovSlider.Y = _startY + sliderH + _gapY;
    _fovSlider.Width = sliderW; _fovSlider.Height = sliderH;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    float scale = UiScale.Scale;
    string title = _currentPage switch
    {
      Page.Video => "Video Settings",
      Page.Sound => "Music & Sounds",
      Page.Controls => "Controls",
      _ => "Options",
    };

    float tw = Text.MeasureTextWidth(title, scale * 1.25f);
    Text.DrawText(title, (screen.X - tw) / 2f, 18f,
        screen, scale: scale * 1.25f, color: White);

    _guiScaleBtn.Text = $"GUI Scale: {(int)UiScale.Scale}x";

    switch (_currentPage)
    {
      case Page.Main:
        foreach (var btn in Buttons.Where(b => b != _doneBtn))
          btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
        break;

      case Page.Video:
        _renderDistSlider.Draw(Text, screen, mouseX, mouseY);
        _fovSlider.Draw(Text, screen, mouseX, mouseY);
        break;
    }

    _doneBtn.Draw(Text, screen, _doneBtn.OnMouseClick(mouseX, mouseY));
  }

  public override void HandleClick(float mx, float my)
  {
    switch (_currentPage)
    {
      case Page.Main:
        foreach (var btn in Buttons)
          if (btn.OnMouseClick(mx, my)) { btn.Click(); return; }
        break;
      case Page.Video:
        _renderDistSlider.HandleClick(mx, my);
        _fovSlider.HandleClick(mx, my);
        break;
    }

    if (_doneBtn.OnMouseClick(mx, my)) _doneBtn.Click();
  }

  public override void HandleMouseMove(float mx, float my)
  {
    _renderDistSlider.HandleMouseMove(mx, my);
    _fovSlider.HandleMouseMove(mx, my);
  }

  public override void HandleMouseUp(float mx, float my)
  {
    _renderDistSlider.HandleMouseUp();
    _fovSlider.HandleMouseUp();
  }

  public override void HandleKeyDown(Keys key, bool shift)
  {
    if (key == Keys.Escape)
    {
      if (_currentPage != Page.Main)
        _currentPage = Page.Main;
      else
        OnBack?.Invoke();
    }
  }
}