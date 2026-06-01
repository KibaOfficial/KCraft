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
    float cx = screen.X / 2f;
    float cy = screen.Y / 2f;
    float inputW = 300f;
    float inputX = cx - inputW / 2f;

    _nameInput.X = inputX; _nameInput.Y = cy - 60f;
    _nameInput.Width = inputW;

    _seedInput.X = inputX; _seedInput.Y = cy;
    _seedInput.Width = inputW;

    Buttons[0].X = cx - 150f; Buttons[0].Y = cy + 60f;  // Create
    Buttons[1].X = cx - 150f; Buttons[1].Y = cy + 110f; // Back
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    float scale = UiScale.Scale;

    // Vollbild Hintergrund
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    // Panel
    float pw = 360f, ph = 260f;
    float px = (screen.X - pw) / 2f;
    float py = (screen.Y - ph) / 2f - 20f;
    Text.DrawRect(px, py, pw, ph, screen, Panel);

    // Titel
    string titleStr = "Create New World";
    float tw = Text.MeasureTextWidth(titleStr, scale * 1.5f);
    Text.DrawText(titleStr, (screen.X - tw) / 2f, py + 16f,
        screen, scale: scale * 1.5f, color: Title);

    // World Name Label
    Text.DrawText("World Name", _nameInput.X, _nameInput.Y - 14f * scale,
        screen, scale: scale, color: Label);
    _nameInput.Draw(Text, screen);

    // Seed Label
    Text.DrawText("Seed", _seedInput.X, _seedInput.Y - 14f * scale,
        screen, scale: scale, color: Label);
    _seedInput.Draw(Text, screen);

    // Error
    if (!string.IsNullOrEmpty(_errorMessage))
    {
      float ew = Text.MeasureTextWidth(_errorMessage, scale);
      Text.DrawText(_errorMessage, (screen.X - ew) / 2f,
          Buttons[0].Y - 18f * scale, screen, scale: scale, color: ErrorCol);
    }

    // Buttons
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