// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class SelectWorldScreen : Screen
{
  public event Action<string>? OnPlay;   // worldName
  public event Action? OnCreate;
  public event Action<string>? OnDelete; // worldName
  public event Action? OnBack;

  // ── Farben ────────────────────────────────────────────────────────────
  private static readonly Vector4 Bg = new(0.05f, 0.05f, 0.05f, 1.00f);
  private static readonly Vector4 ListBg = new(0.08f, 0.08f, 0.08f, 1.00f);
  private static readonly Vector4 EntryNormal = new(0.12f, 0.12f, 0.12f, 1.00f);
  private static readonly Vector4 EntrySelected = new(0.20f, 0.20f, 0.35f, 1.00f);
  private static readonly Vector4 EntryHover = new(0.16f, 0.16f, 0.22f, 1.00f);
  private static readonly Vector4 TitleColor = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 NameColor = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 SubColor = new(0.65f, 0.65f, 0.65f, 1.00f);
  private static readonly Vector4 BorderColor = new(0.30f, 0.30f, 0.30f, 1.00f);

  // ── Layout ────────────────────────────────────────────────────────────
  private float _listX, _listY, _listW, _listH;
  private const float EntryHeight = 52f;
  private const float EntryPadding = 2f;

  // ── State ─────────────────────────────────────────────────────────────
  private readonly TextInput _filterInput;
  private List<WorldEntry> _allWorlds = [];
  private List<WorldEntry> _filtered = [];
  private int _selectedIndex = -1;
  private float _mouseX, _mouseY;
  private float _scrollOffset = 0f;

  private record WorldEntry(string Name, WorldSaveData Data);

  public SelectWorldScreen(TextRenderer text) : base(text)
  {
    _filterInput = new TextInput(0, 0, 400, 26,
        placeholder: "Search worlds...",
        maxLength: 64);
    TextInputs.Add(_filterInput);

    // Buttons — werden in Layout positioniert
    var play = new Button("Play Selected World", 0, 0, 200, 36);
    var create = new Button("Create New World", 0, 0, 200, 36);
    var delete = new Button("Delete", 0, 0, 96, 36) { Disabled = true };
    var cancel = new Button("Cancel", 0, 0, 96, 36);

    play.OnClick += () => { if (_selectedIndex >= 0) OnPlay?.Invoke(_filtered[_selectedIndex].Name); };
    create.OnClick += () => OnCreate?.Invoke();
    delete.OnClick += () =>
    {
      if (_selectedIndex >= 0)
      {
        OnDelete?.Invoke(_filtered[_selectedIndex].Name);
        RefreshWorlds();
        _selectedIndex = -1;
      }
    };
    cancel.OnClick += () => OnBack?.Invoke();

    Buttons.AddRange([play, create, delete, cancel]);
  }

  // ── World List ────────────────────────────────────────────────────────
  public void RefreshWorlds()
  {
    _allWorlds.Clear();
    foreach (var name in WorldSaveManager.GetWorldNames())
    {
      var (data, _) = WorldSaveManager.Load(name);
      if (data != null)
        _allWorlds.Add(new WorldEntry(name, data));
    }
    // Sortierung: zuletzt gespielt zuerst
    _allWorlds = [.. _allWorlds.OrderByDescending(w => w.Data.LastPlayed)];
    ApplyFilter();
  }

  private void ApplyFilter()
  {
    string filter = _filterInput.Value.Trim().ToLower();
    _filtered = string.IsNullOrEmpty(filter)
        ? [.. _allWorlds]
        : [.. _allWorlds.Where(w => w.Name.ToLower().Contains(filter))];

    if (_selectedIndex >= _filtered.Count)
      _selectedIndex = -1;

    UpdateButtonStates();
  }

  private void UpdateButtonStates()
  {
    bool hasSelection = _selectedIndex >= 0;
    Buttons[0].Disabled = !hasSelection; // Play
    Buttons[2].Disabled = !hasSelection; // Delete
  }

  // ── Layout ────────────────────────────────────────────────────────────
  public override void Layout(Vector2 screen)
  {
    float s = UiScale.Scale;

    // Filter oben
    _filterInput.X = (screen.X - 400f) / 2f;
    _filterInput.Y = 50f;
    _filterInput.Width = 400f;

    // Liste
    _listX = (screen.X - 500f) / 2f;
    _listY = _filterInput.Y + _filterInput.Height + 10f;
    _listW = 500f;
    _listH = screen.Y - _listY - 70f;

    // Buttons unten
    float bY = screen.Y - 52f;
    float cx = screen.X / 2f;
    Buttons[0].X = cx - 204f; Buttons[0].Y = bY; // Play
    Buttons[1].X = cx + 4f; Buttons[1].Y = bY; // Create
    Buttons[2].X = cx - 204f; Buttons[2].Y = bY + 38f; // Delete
    Buttons[3].X = cx + 4f; Buttons[3].Y = bY + 38f; // Cancel
  }

  // ── Draw ──────────────────────────────────────────────────────────────
  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    Console.WriteLine($"[SelectWorld] Draw called, worlds: {_filtered.Count}");

    _mouseX = mouseX; _mouseY = mouseY;
    float scale = UiScale.Scale;

    // Vollbild Hintergrund
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    // Titel
    string title = "Select World";
    float tw = Text.MeasureTextWidth(title, scale * 1.5f);
    Text.DrawText(title, (screen.X - tw) / 2f, 14f,
        screen, scale: scale * 1.5f, color: TitleColor);

    // Filter
    _filterInput.Draw(Text, screen);

    // Liste Hintergrund
    Text.DrawRect(_listX - 1, _listY - 1, _listW + 2, _listH + 2, screen, BorderColor);
    Text.DrawRect(_listX, _listY, _listW, _listH, screen, ListBg);

    // Einträge
    float entryH = EntryHeight * scale;
    float padH = EntryPadding * scale;
    float visibleH = _listH;
    float startY = _listY - _scrollOffset;

    for (int i = 0; i < _filtered.Count; i++)
    {
      float ey = startY + i * (entryH + padH);

      // Clipping — nur sichtbare Einträge zeichnen
      if (ey + entryH < _listY || ey > _listY + visibleH) continue;

      var entry = _filtered[i];
      bool isSelected = i == _selectedIndex;
      bool isHover = mouseX >= _listX && mouseX <= _listX + _listW
                     && mouseY >= ey && mouseY <= ey + entryH;

      var bg = isSelected ? EntrySelected : isHover ? EntryHover : EntryNormal;
      Text.DrawRect(_listX, ey, _listW, entryH, screen, bg);

      // World Name
      Text.DrawText(entry.Name,
          _listX + 10f * scale, ey + 6f * scale,
          screen, scale: scale, color: NameColor);

      // Sub Info
      string gameMode = entry.Data.GameMode switch
      {
        1 => "Creative Mode",
        2 => "Spectator Mode",
        _ => "Survival Mode"
      };
      string lastPlayed = entry.Data.LastPlayed.ToString("dd.MM.yyyy HH:mm");
      string sub = $"{entry.Name}  ({lastPlayed})  {gameMode}  v0.3.0";
      Text.DrawText(sub,
          _listX + 10f * scale, ey + 22f * scale,
          screen, scale: scale * 0.8f, color: SubColor);
    }

    // Leer-State
    if (_filtered.Count == 0)
    {
      string msg = _allWorlds.Count == 0
          ? "No worlds found. Create one!"
          : "No worlds match your filter.";
      float mw = Text.MeasureTextWidth(msg, scale);
      Text.DrawText(msg,
          (screen.X - mw) / 2f, _listY + _listH / 2f - 8f * scale,
          screen, scale: scale, color: SubColor);
    }

    // Buttons
    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  // ── Input ─────────────────────────────────────────────────────────────
  public override void HandleClick(float mx, float my)
  {
    base.HandleClick(mx, my);

    // Klick in die Liste
    if (mx >= _listX && mx <= _listX + _listW
     && my >= _listY && my <= _listY + _listH)
    {
      float scale = UiScale.Scale;
      float entryH = EntryHeight * scale + EntryPadding * scale;
      int idx = (int)((my - _listY + _scrollOffset) / entryH);

      if (idx >= 0 && idx < _filtered.Count)
      {
        _selectedIndex = idx;
        UpdateButtonStates();
      }
    }
  }

  public override void HandleKeyDown(Keys key, bool shift)
  {
    base.HandleKeyDown(key, shift);

    switch (key)
    {
      case Keys.Up:
        if (_selectedIndex > 0) { _selectedIndex--; UpdateButtonStates(); }
        break;
      case Keys.Down:
        if (_selectedIndex < _filtered.Count - 1) { _selectedIndex++; UpdateButtonStates(); }
        break;
      case Keys.Enter:
        if (_selectedIndex >= 0) OnPlay?.Invoke(_filtered[_selectedIndex].Name);
        break;
    }
  }

  public override void HandleTextInput(char c)
  {
    base.HandleTextInput(c);
    ApplyFilter();
  }

  public override void Update(float deltaTime)
  {
    base.Update(deltaTime);
  }
}