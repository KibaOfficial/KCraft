// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class NewWorldScreen : Screen
{
  public event Action<string, int?>? OnCreate; // worldName, seed (null = random)
  public event Action? OnBack;

  private readonly TextInput _nameInput;
  private readonly TextInput _seedInput;

  private static readonly Vector4 Bg = new(0.0f, 0.0f, 0.0f, 0.55f);
  private static readonly Vector4 Panel = new(0.1f, 0.1f, 0.1f, 0.85f);
  private static readonly Vector4 Title = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Label = new(0.75f, 0.75f, 0.75f, 1.0f);
  private static readonly Vector4 ErrorCol = new(1.0f, 0.3f, 0.3f, 1.0f);

  private string _errorMessage = "";

  public NewWorldScreen(TextRenderer text) : base(text)
  {
    _nameInput = new TextInput(0, 0, 300, 28,
        placeholder: "My World",
        maxLength: 32);

    _seedInput = new TextInput(0, 0, 300, 28,
        placeholder: "Leave empty for random seed",
        maxLength: 20);

    TextInputs.AddRange([_nameInput, _seedInput]);

    var create = new Button("Create World", 0, 0, 300, 40);
    var back = new Button("Back", 0, 0, 300, 40);

    create.OnClick += HandleCreate;
    back.OnClick += () => OnBack?.Invoke();

    Buttons.AddRange([create, back]);
  }

  private void HandleCreate()
  {
    string name = _nameInput.Value.Trim();
    if (string.IsNullOrEmpty(name))
      name = "My World";

    // Seed parsen
    int? seed = null;
    string seedStr = _seedInput.Value.Trim();
    if (!string.IsNullOrEmpty(seedStr))
    {
      if (int.TryParse(seedStr, out int parsedSeed))
        seed = parsedSeed;
      else
      {
        // String als Seed hashen wie MC
        seed = seedStr.GetHashCode();
      }
    }

    // World Name validieren — keine ungültigen Zeichen
    var invalid = Path.GetInvalidFileNameChars();
    if (name.Any(c => invalid.Contains(c)))
    {
      _errorMessage = "World name contains invalid characters!";
      return;
    }

    _errorMessage = "";
    OnCreate?.Invoke(name, seed);
  }

  public override void Layout(Vector2 screen)
  {
    float pw = 380f;
    float ph = 300f;
    float px = (screen.X - pw) / 2f;
    float py = (screen.Y - ph) / 2f;
    float iw = pw - 40f;
    float ix = px + 20f;

    _nameInput.X = ix; _nameInput.Y = py + 74f; _nameInput.Width = iw; _nameInput.Height = 28f;
    _seedInput.X = ix; _seedInput.Y = py + 136f; _seedInput.Width = iw; _seedInput.Height = 28f;

    Buttons[0].X = ix; Buttons[0].Y = py + 202f; Buttons[0].Width = iw; // Create
    Buttons[1].X = ix; Buttons[1].Y = py + 248f; Buttons[1].Width = iw; // Back
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    float s = UiScale.Scale;

    // gleiche logical Werte wie Layout()
    float pw = 380f;
    float ph = 300f;
    float px = (screen.X / s - pw) / 2f;
    float py = (screen.Y / s - ph) / 2f;
    float ix = px + 20f;

    // physical Werte fürs manuelle Zeichnen
    float spw = pw * s;
    float sph = ph * s;
    float spx = px * s;
    float spy = py * s;
    float six = ix * s;

    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);
    Text.DrawRect(spx, spy, spw, sph, screen, Panel);

    string t = "Create New World";
    float titleScale = s * 1.5f;
    float tw = Text.MeasureTextWidth(t, titleScale);
    Text.DrawText(t, spx + (spw - tw) / 2f, spy + 16f * s,
        screen, scale: titleScale, color: Title);

    Text.DrawText("World Name", six, spy + 58f * s,
        screen, scale: s, color: Label);
    _nameInput.Draw(Text, screen);

    Text.DrawText("Seed", six, spy + 120f * s,
        screen, scale: s, color: Label);
    _seedInput.Draw(Text, screen);

    if (!string.IsNullOrEmpty(_errorMessage))
    {
      float ew = Text.MeasureTextWidth(_errorMessage, s);
      Text.DrawText(_errorMessage, spx + (spw - ew) / 2f, spy + 176f * s,
          screen, scale: s, color: ErrorCol);
    }

    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  public override void Update(float deltaTime)
  {
    base.Update(deltaTime);
  }

  public override void HandleKeyDown(Keys key, bool shift)
  {
    base.HandleKeyDown(key, shift);
    if (key == Keys.Enter) HandleCreate();
  }
}
