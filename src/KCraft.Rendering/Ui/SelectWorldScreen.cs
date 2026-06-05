// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Core;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class SelectWorldScreen : Screen
{
  public event Action<string>? OnPlay;
  public event Action? OnCreate;
  public event Action<string>? OnDelete;
  public event Action? OnBack;

  // ── Farben ────────────────────────────────────────────────────────────
  private static readonly Vector4 Bg = new(0.10f, 0.10f, 0.10f, 1.00f);
  private static readonly Vector4 ListBg = new(0.07f, 0.07f, 0.07f, 1.00f);
  private static readonly Vector4 EntryNormal = new(0.15f, 0.15f, 0.15f, 1.00f);
  private static readonly Vector4 EntryHover = new(0.18f, 0.18f, 0.18f, 1.00f);
  private static readonly Vector4 EntrySelected = new(0.23f, 0.23f, 0.45f, 1.00f);
  private static readonly Vector4 BorderSelected = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 IconBg = new(0.20f, 0.20f, 0.20f, 1.00f);
  private static readonly Vector4 TitleColor = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 WorldName = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 SubInfo = new(0.65f, 0.65f, 0.65f, 1.00f);
  private static readonly Vector4 SubDim = new(0.45f, 0.45f, 0.45f, 1.00f);
  private static readonly Vector4 DividerColor = new(0.25f, 0.25f, 0.25f, 1.00f);

  // ── Layout ────────────────────────────────────────────────────────────
  private float _listX, _listY, _listW, _listH;
  private const float IconSize = 48f;
  private const float EntryH = 56f;
  private const float EntryGap = 2f;
  private const float EntryPadX = 8f;
  private const float EntryPadY = 6f;

  // ── State ─────────────────────────────────────────────────────────────
  private readonly TextInput _filterInput;
  private List<WorldEntry> _allWorlds = [];
  private List<WorldEntry> _filtered = [];
  private int _selectedIndex = -1;
  private float _scrollOffset = 0f;
  private float _mouseX, _mouseY;
  private float _lastClickTime = 0f;
  private int _lastClickIndex = -1;

  private record WorldEntry(string FolderName, WorldSaveData Data);

  public SelectWorldScreen(TextRenderer text) : base(text)
  {
    _filterInput = new TextInput(0, 0, 400, 26,
        placeholder: "Search worlds...",
        maxLength: 64);
    TextInputs.Add(_filterInput);

    var play = new Button("Play Selected World", 0, 0, 200, 36) { Disabled = true };
    var create = new Button("Create New World", 0, 0, 200, 36);
    var delete = new Button("Delete", 0, 0, 96, 36) { Disabled = true };
    var cancel = new Button("Cancel", 0, 0, 96, 36);

    play.OnClick += () => { if (_selectedIndex >= 0) OnPlay?.Invoke(_filtered[_selectedIndex].FolderName); };
    create.OnClick += () => OnCreate?.Invoke();
    delete.OnClick += () =>
    {
      if (_selectedIndex < 0) return;
      OnDelete?.Invoke(_filtered[_selectedIndex].FolderName);
      RefreshWorlds();
      _selectedIndex = -1;
      UpdateButtonStates();
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
      var (data, _, _) = WorldSaveManager.Load(name);
      if (data != null)
        _allWorlds.Add(new WorldEntry(name, data));
    }
    _allWorlds = [.. _allWorlds.OrderByDescending(w => w.Data.LastPlayed)];
    ApplyFilter();
  }

  private void ApplyFilter()
  {
    string f = _filterInput.Value.Trim().ToLower();
    _filtered = string.IsNullOrEmpty(f)
        ? [.. _allWorlds]
        : [.. _allWorlds.Where(w =>
                w.FolderName.ToLower().Contains(f) ||
                w.Data.WorldName.ToLower().Contains(f))];

    if (_selectedIndex >= _filtered.Count)
      _selectedIndex = -1;

    UpdateButtonStates();
  }

  private void UpdateButtonStates()
  {
    bool sel = _selectedIndex >= 0;
    Buttons[0].Disabled = !sel;
    Buttons[2].Disabled = !sel;
  }

  // ── Layout ────────────────────────────────────────────────────────────
  public override void Layout(Vector2 screen)
  {
    float scale = UiScale.Scale;

    // Filter
    float filterW = Math.Min(600f, screen.X - 40f);
    _filterInput.X = (screen.X - filterW) / 2f;
    _filterInput.Y = 44f;
    _filterInput.Width = filterW;
    _filterInput.Height = 24f;

    // Liste
    float listW = Math.Min(700f, screen.X - 40f);
    _listX = (screen.X - listW) / 2f;
    _listY = _filterInput.Y + _filterInput.Height + 8f;
    _listW = listW;
    _listH = screen.Y - _listY - 100f;

    // Buttons — zwei Reihen, zentriert
    // Reihe 1: Play (300px) + gap (4px) + Create (200px) = 504px total
    float row1W = 300f + 4f + 200f;
    float row1X = (screen.X - row1W) / 2f;
    float bY1 = screen.Y - 90f;

    Buttons[0].X = row1X; Buttons[0].Y = bY1; Buttons[0].Width = 300f; // Play
    Buttons[1].X = row1X + 304f; Buttons[1].Y = bY1; Buttons[1].Width = 200f; // Create

    // Reihe 2: Delete (148px) + gap (4px) + Cancel (148px) = 300px → zentriert
    float row2W = 148f + 4f + 148f;
    float row2X = (screen.X - row2W) / 2f;
    float bY2 = bY1 + 44f;

    Buttons[2].X = row2X; Buttons[2].Y = bY2; Buttons[2].Width = 148f; // Delete
    Buttons[3].X = row2X + 152f; Buttons[3].Y = bY2; Buttons[3].Width = 148f; // Cancel
  }

  // ── Draw ──────────────────────────────────────────────────────────────
  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX; _mouseY = mouseY;
    float scale = UiScale.Scale;

    // Hintergrund
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    // Titel
    string title = "Select World";
    float tw = Text.MeasureTextWidth(title, scale * 1.5f);
    Text.DrawText(title, (screen.X - tw) / 2f, 12f,
        screen, scale: scale * 1.5f, color: TitleColor);

    // Filter
    _filterInput.Draw(Text, screen);

    // Liste Rahmen + Hintergrund
    Text.DrawRect(_listX, _listY, _listW, _listH, screen, ListBg);

    // Einträge rendern mit Scissor-Clipping
    float entryH = EntryH * scale;
    float entryG = EntryGap * scale;
    float stride = entryH + entryG;

    GL.Enable(EnableCap.ScissorTest);
    GL.Scissor(
      (int)_listX,
      (int)(screen.Y - (_listY + _listH)),
      (int)_listW,
      (int)_listH);

    for (int i = 0; i < _filtered.Count; i++)
    {
      float ey = _listY + i * stride - _scrollOffset;
      if (ey + entryH < _listY) continue;
      if (ey > _listY + _listH) break;

      // Clip zu Listbereich
      float visTop = Math.Max(ey, _listY);
      float visBottom = Math.Min(ey + entryH, _listY + _listH);
      if (visBottom <= visTop) continue;

      var entry = _filtered[i];
      bool isSelected = i == _selectedIndex;
      bool isHover = mouseX >= _listX && mouseX <= _listX + _listW
                     && mouseY >= ey && mouseY <= ey + entryH;

      // Entry Background
      var bg = isSelected ? EntrySelected : isHover ? EntryHover : EntryNormal;
      Text.DrawRect(_listX, ey, _listW, entryH, screen, bg);

      // Selected Border links
      if (isSelected)
        Text.DrawRect(_listX, ey, 3f * scale, entryH, screen, BorderSelected);

      // Icon Placeholder
      float iconSize = IconSize * scale;
      float iconX = _listX + EntryPadX * scale;
      float iconY = ey + (entryH - iconSize) / 2f;
      Text.DrawRect(iconX, iconY, iconSize, iconSize, screen, IconBg);

      // Icon Text (Welt-Initiale)
      string initial = entry.Data.WorldName.Length > 0
          ? entry.Data.WorldName[0].ToString().ToUpper()
          : "?";
      float iw = Text.MeasureTextWidth(initial, scale * 2f);
      Text.DrawText(initial,
          iconX + (iconSize - iw) / 2f,
          iconY + (iconSize - 16f * scale) / 2f,
          screen, scale: scale * 2f, color: TitleColor);

      // Welt Name
      float textX = iconX + iconSize + EntryPadX * scale;
      float nameY = ey + EntryPadY * scale;
      Text.DrawText(entry.Data.WorldName, textX, nameY,
          screen, scale: scale, color: WorldName);

      // Sub Info Zeile 1: Ordnername (Datum)
      string lastPlayed = entry.Data.LastPlayed.ToString("dd.MM.yyyy HH:mm");
      string line1 = $"{entry.FolderName}  ({lastPlayed})";
      Text.DrawText(line1, textX, nameY + 10f * scale,
          screen, scale: scale * 0.85f, color: SubInfo);

      // Sub Info Zeile 2: GameMode / Version
      string gameMode = entry.Data.GameMode switch
      {
        1 => "Creative Mode",
        2 => "Spectator Mode",
        _ => "Survival Mode"
      };
      string line2 = $"{gameMode}  - {KCraftVersion.FullName}";
      Text.DrawText(line2, textX, nameY + 20f * scale,
          screen, scale: scale * 0.85f, color: SubDim);

      // Divider
      if (i < _filtered.Count - 1)
        Text.DrawRect(_listX, ey + entryH, _listW, 1f, screen, DividerColor);
    }

    GL.Disable(EnableCap.ScissorTest);

    // Leer-State
    if (_filtered.Count == 0)
    {
      string msg = _allWorlds.Count == 0
          ? "No worlds yet. Create one!"
          : "No worlds match your search.";
      float mw = Text.MeasureTextWidth(msg, scale);
      Text.DrawText(msg,
          (screen.X - mw) / 2f,
          _listY + _listH / 2f - 8f * scale,
          screen, scale: scale, color: SubInfo);
    }

    // Buttons
    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  // ── Input ─────────────────────────────────────────────────────────────
  public override void HandleClick(float mx, float my)
  {
    base.HandleClick(mx, my);

    if (mx >= _listX && mx <= _listX + _listW
     && my >= _listY && my <= _listY + _listH)
    {
      float scale = UiScale.Scale;
      float stride = (EntryH + EntryGap) * scale;
      int idx = (int)((my - _listY + _scrollOffset) / stride);

      if (idx >= 0 && idx < _filtered.Count)
      {
        // Doppelklick detection
        float now = (float)Environment.TickCount / 1000f;
        if (idx == _lastClickIndex && now - _lastClickTime < 0.4f)
        {
          // Doppelklick → sofort spielen
          OnPlay?.Invoke(_filtered[idx].FolderName);
          return;
        }
        _lastClickTime = now;
        _lastClickIndex = idx;
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
        if (_selectedIndex > 0) { _selectedIndex--; UpdateButtonStates(); EnsureVisible(); }
        break;
      case Keys.Down:
        if (_selectedIndex < _filtered.Count - 1) { _selectedIndex++; UpdateButtonStates(); EnsureVisible(); }
        break;
      case Keys.Enter:
        if (_selectedIndex >= 0) OnPlay?.Invoke(_filtered[_selectedIndex].FolderName);
        break;
      case Keys.Delete:
        if (_selectedIndex >= 0)
        {
          OnDelete?.Invoke(_filtered[_selectedIndex].FolderName);
          RefreshWorlds();
          _selectedIndex = -1;
          UpdateButtonStates();
        }
        break;
    }
  }

  public override void HandleTextInput(char c)
  {
    base.HandleTextInput(c);
    ApplyFilter();
  }

  public void HandleScroll(float delta)
  {
    float scale = UiScale.Scale;
    float stride = (EntryH + EntryGap) * scale;
    float maxScroll = Math.Max(0, _filtered.Count * stride - _listH);
    _scrollOffset = Math.Clamp(_scrollOffset - delta * stride, 0, maxScroll);
  }

  private void EnsureVisible()
  {
    float scale = UiScale.Scale;
    float stride = (EntryH + EntryGap) * scale;
    float itemTop = _selectedIndex * stride;
    float itemBottom = itemTop + EntryH * scale;

    if (itemTop < _scrollOffset)
      _scrollOffset = itemTop;
    else if (itemBottom > _scrollOffset + _listH)
      _scrollOffset = itemBottom - _listH;
  }

  public override void Update(float deltaTime) => base.Update(deltaTime);
}
