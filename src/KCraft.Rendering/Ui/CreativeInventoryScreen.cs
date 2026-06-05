// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class CreativeInventoryScreen : Screen
{
  public event Action? OnClose;

  private readonly PlayerInventory _inventory;
  private readonly BlockIconRenderer _icon;
  private TextureManager? _textures;

  // Tabs
  private readonly List<(string name, List<Block> items)> _tabs = new()
    {
        ("Blocks",   [Block.Grass, Block.Dirt, Block.Stone, Block.Sand, Block.Cobblestone, Block.Gravel, Block.Glass]),
        ("Wood",     [Block.OakLog, Block.OakPlanks, Block.OakLeaves]),
        ("Natural",  [Block.Water]),
        ("Building", [Block.OakStairs, Block.StoneStairs]),
        ("Inv",      []), // Special: zeigt Survival Inv
    };
  private int _activeTab = 0;
  private float _scrollOffset = 0f;

  // Layout
  private float _panelX, _panelY, _slotSize, _padding;
  private float _tabH;
  private Vector2 _screen;
  private float _mouseX, _mouseY;

  // Colors
  private static readonly Vector4 BgDim = new(0f, 0f, 0f, 0.5f);
  private static readonly Vector4 PanelBg = new(0xC6 / 255f, 0xC6 / 255f, 0xC6 / 255f, 1f);
  private static readonly Vector4 SlotBg = new(0x8B / 255f, 0x8B / 255f, 0x8B / 255f, 1f);
  private static readonly Vector4 SlotHover = new(0xA0 / 255f, 0xA0 / 255f, 0xA0 / 255f, 1f);
  private static readonly Vector4 TabActive = new(0xC6 / 255f, 0xC6 / 255f, 0xC6 / 255f, 1f);
  private static readonly Vector4 TabInactive = new(0x8B / 255f, 0x8B / 255f, 0x8B / 255f, 1f);
  private static readonly Vector4 BorderDark = new(0x37 / 255f, 0x37 / 255f, 0x37 / 255f, 1f);
  private static readonly Vector4 BorderLight = new(1f, 1f, 1f, 1f);
  private static readonly Vector4 ScrollBg = new(0x55 / 255f, 0x55 / 255f, 0x55 / 255f, 1f);
  private static readonly Vector4 ScrollBar = new(0x9F / 255f, 0x9F / 255f, 0x9F / 255f, 1f);

  private const int GridCols = 9;
  private const int GridRows = 3;

  public CreativeInventoryScreen(TextRenderer text, PlayerInventory inventory) : base(text)
  {
    _inventory = inventory;
    _icon = new BlockIconRenderer();
  }

  public void SetTextures(TextureManager textures) => _textures = textures;

  public override void Layout(Vector2 screen)
  {
    _screen = screen;
    float s = UiScale.Scale;
    _slotSize = 20f * s;
    _padding = 2f * s;
    _tabH = 22f * s;

    float panelW = GridCols * (_slotSize + _padding) + _padding * 2 + 8f + 10f * s; // +scrollbar
    float panelH = _tabH                              // tabs
                 + GridRows * (_slotSize + _padding)  // item grid
                 + _padding * 4                       // gaps
                 + _slotSize + _padding * 2;          // hotbar

    _panelX = (screen.X - panelW) / 2f;
    _panelY = (screen.Y - panelH) / 2f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX;
    _mouseY = mouseY;
    float s = UiScale.Scale;

    float panelW = GridCols * (_slotSize + _padding) + _padding * 2 + 8f + 10f * s;
    float panelH = _tabH + GridRows * (_slotSize + _padding) + _padding * 4 + _slotSize + _padding * 2;

    // Dim
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, BgDim);
    GL.Disable(EnableCap.Blend);

    // Panel
    Text.DrawRect(_panelX, _panelY, panelW, panelH, screen, PanelBg);

    // ── Tabs oben ──
    float tabW = (_slotSize + _padding) * 2f;
    float tabsStartX = _panelX + _padding + 4f;

    // Pfeile nur wenn nötig (aktuell 5 Tabs passen alle rein)
    int visibleTabs = (int)((panelW - _padding * 4) / tabW);
    bool needArrows = _tabs.Count > visibleTabs;

    for (int i = 0; i < _tabs.Count && i < visibleTabs; i++)
    {
      float tx = tabsStartX + i * (tabW + _padding);
      bool active = i == _activeTab;
      bool hover = mouseX >= tx && mouseX <= tx + tabW
                 && mouseY >= _panelY && mouseY <= _panelY + _tabH;

      Text.DrawRect(tx, _panelY + 2f, tabW, _tabH - 2f, screen, active ? TabActive : hover ? SlotHover : TabInactive);
      Text.DrawRect(tx, _panelY + 2f, tabW, 1f, screen, active ? BorderLight : BorderDark);
      Text.DrawRect(tx, _panelY + 2f, 1f, _tabH - 2f, screen, BorderDark);
      Text.DrawRect(tx + tabW, _panelY + 2f, 1f, _tabH - 2f, screen, BorderLight);

      float tw = Text.MeasureTextWidth(_tabs[i].name, s * 0.8f);
      Text.DrawText(_tabs[i].name, tx + (tabW - tw) / 2f, _panelY + 4f * s,
          screen, scale: s * 0.8f, color: active ? BorderDark : new Vector4(0.7f, 0.7f, 0.7f, 1f));
    }

    // ── Item Grid ──
    float gridX = _panelX + _padding + 4f;
    float gridY = _panelY + _tabH + _padding;
    float scrollW = 8f * s;

    bool isInvTab = _activeTab == _tabs.Count - 1;

    if (isInvTab)
    {
      // Survival Inventory anzeigen (3×9)
      for (int row = 0; row < GridRows; row++)
        for (int col = 0; col < GridCols; col++)
        {
          int slot = 9 + row * 9 + col;
          float sx = gridX + col * (_slotSize + _padding);
          float sy = gridY + row * (_slotSize + _padding);
          DrawSlot(sx, sy, _inventory.GetSlot(slot), screen);
        }
    }
    else
    {
      var items = _tabs[_activeTab].items;
      int totalRows = (int)MathF.Ceiling(items.Count / (float)GridCols);
      float maxScroll = Math.Max(0, (totalRows - GridRows) * (_slotSize + _padding));
      _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

      int startRow = (int)(_scrollOffset / (_slotSize + _padding));

      for (int row = 0; row < GridRows; row++)
      {
        for (int col = 0; col < GridCols; col++)
        {
          int idx = (startRow + row) * GridCols + col;
          float sx = gridX + col * (_slotSize + _padding);
          float sy = gridY + row * (_slotSize + _padding);
          DrawSlotEmpty(sx, sy, screen);
          if (idx < items.Count)
            DrawSlotBlock(sx, sy, items[idx], screen);
        }
      }

      // Scrollbar
      if (totalRows > GridRows)
      {
        float sbX = gridX + GridCols * (_slotSize + _padding) + _padding;
        float sbH = GridRows * (_slotSize + _padding);
        float barH = sbH * GridRows / totalRows;
        float barY = gridY + (maxScroll > 0 ? _scrollOffset / maxScroll * (sbH - barH) : 0);

        Text.DrawRect(sbX, gridY, scrollW, sbH, screen, ScrollBg);
        Text.DrawRect(sbX, barY, scrollW, barH, screen, ScrollBar);
      }
    }

    // ── Hotbar ──
    float hotbarY = gridY + GridRows * (_slotSize + _padding) + _padding * 2;
    for (int col = 0; col < GridCols; col++)
    {
      float sx = gridX + col * (_slotSize + _padding);
      bool sel = col == _inventory.SelectedHotbarSlot;
      if (sel)
        Text.DrawRect(sx - 2, hotbarY - 2, _slotSize + 4, _slotSize + 4, screen, BorderLight);
      DrawSlot(sx, hotbarY, _inventory.GetHotbar(col), screen);
    }
  }

  private void DrawSlotEmpty(float sx, float sy, Vector2 screen)
  {
    bool hover = _mouseX >= sx && _mouseX <= sx + _slotSize
              && _mouseY >= sy && _mouseY <= sy + _slotSize;
    Text.DrawRect(sx - 1, sy - 1, _slotSize + 2, 1, screen, BorderDark);
    Text.DrawRect(sx - 1, sy - 1, 1, _slotSize + 2, screen, BorderDark);
    Text.DrawRect(sx - 1, sy + _slotSize, _slotSize + 2, 1, screen, BorderLight);
    Text.DrawRect(sx + _slotSize, sy - 1, 1, _slotSize + 2, screen, BorderLight);
    Text.DrawRect(sx, sy, _slotSize, _slotSize, screen, hover ? SlotHover : SlotBg);
  }

  private void DrawSlotBlock(float sx, float sy, Block block, Vector2 screen)
  {
    if (block != Block.Air && _textures != null)
      _icon.Draw(block, sx, sy, _slotSize, screen, _textures);
  }

  private void DrawSlot(float sx, float sy, Block block, Vector2 screen)
  {
    DrawSlotEmpty(sx, sy, screen);
    DrawSlotBlock(sx, sy, block, screen);
  }

  public override void HandleClick(float mx, float my)
  {
    float s = UiScale.Scale;
    float panelW = GridCols * (_slotSize + _padding) + _padding * 2 + 8f + 10f * s;
    float tabW = (_slotSize + _padding) * 2f;
    float tabsStartX = _panelX + _padding + 4f;
    int visibleTabs = (int)((panelW - _padding * 4) / tabW);

    // Tab klick
    for (int i = 0; i < _tabs.Count && i < visibleTabs; i++)
    {
      float tx = tabsStartX + i * (tabW + _padding);
      if (mx >= tx && mx <= tx + tabW && my >= _panelY && my <= _panelY + _tabH)
      {
        _activeTab = i;
        _scrollOffset = 0f;
        return;
      }
    }

    float gridX = _panelX + _padding + 4f;
    float gridY = _panelY + _tabH + _padding;
    float hotbarY = gridY + GridRows * (_slotSize + _padding) + _padding * 2;

    // Item Grid klick
    bool isInvTab = _activeTab == _tabs.Count - 1;
    if (!isInvTab)
    {
      var items = _tabs[_activeTab].items;
      int startRow = (int)(_scrollOffset / (_slotSize + _padding));

      for (int row = 0; row < GridRows; row++)
        for (int col = 0; col < GridCols; col++)
        {
          int idx = (startRow + row) * GridCols + col;
          if (idx >= items.Count) continue;
          float sx = gridX + col * (_slotSize + _padding);
          float sy = gridY + row * (_slotSize + _padding);
          if (mx >= sx && mx <= sx + _slotSize && my >= sy && my <= sy + _slotSize)
          {
            // Block in selektierten Hotbar-Slot legen
            _inventory.SetHotbar(_inventory.SelectedHotbarSlot, items[idx]);
            return;
          }
        }
    }

    // Hotbar klick
    for (int col = 0; col < GridCols; col++)
    {
      float sx = gridX + col * (_slotSize + _padding);
      if (mx >= sx && mx <= sx + _slotSize && my >= hotbarY && my <= hotbarY + _slotSize)
      {
        _inventory.SelectedHotbarSlot = col;
        return;
      }
    }
  }

  public void HandleScroll(float delta)
  {
    _scrollOffset -= delta * (_slotSize + _padding);
    var items = _tabs[_activeTab].items;
    int totalRows = (int)MathF.Ceiling(items.Count / (float)GridCols);
    float maxScroll = Math.Max(0, (totalRows - GridRows) * (_slotSize + _padding));
    _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
  }

  public override void HandleKeyDown(Keys key, bool shift)
  {
    if (key == Keys.Escape || key == Keys.E)
      OnClose?.Invoke();
  }

  public override void Update(float deltaTime) { }

  public void Dispose() => _icon.Dispose();
}