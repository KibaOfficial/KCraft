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

  // ── Tabs ──────────────────────────────────────────────────────────────
  private readonly List<(string name, List<Block> items)> _tabs = new()
  {
    ("Blocks",   [Block.Grass, Block.Dirt, Block.Stone, Block.Sand, Block.Cobblestone, Block.Gravel, Block.Glass]),
    ("Wood",     [Block.OakLog, Block.OakPlanks, Block.OakLeaves]),
    ("Natural",  [Block.Water]),
    ("Building", [Block.OakStairs, Block.StoneStairs, Block.OakSlope, Block.StoneSlope]),
    ("Inv",      []), // Special: zeigt Player Inventory
  };

  private int _activeTab;
  private float _scrollOffset = 0f;

  // ── Layout, UI units ─────────────────────────────────────────────────
  private float _panelX, _panelY, _slotSize, _padding;
  private float _tabH;
  private float _mouseX, _mouseY;

  // ── Drag State ────────────────────────────────────────────────────────
  private Block _heldBlock = Block.Air;
  private int _heldFromSlot = -1; // -1 = none, -2 = creative source, 0-35 = player inv/hotbar

  // ── Colors ────────────────────────────────────────────────────────────
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

  public CreativeInventoryScreen(TextRenderer text, PlayerInventory inventory)
    : base(text)
  {
    _inventory = inventory;
    _icon = new BlockIconRenderer();

    _activeTab = _tabs.FindIndex(t => t.name == "Inv");
    if (_activeTab < 0)
      _activeTab = 0;
  }

  public void SetTextures(TextureManager textures) => _textures = textures;

  private float CalcPanelW()
  {
    float gridW = GridCols * (_slotSize + _padding)
                + _padding * 2f
                + 8f
                + 10f; // scrollbar/gap reserve

    float tabW = (_slotSize + _padding) * 2f;
    float tabsW = _padding * 2f
                + 4f
                + _tabs.Count * tabW
                + (_tabs.Count - 1) * _padding;

    return MathF.Max(gridW, tabsW);
  }

  private float CalcPanelH()
    => _tabH
     + GridRows * (_slotSize + _padding)
     + _padding * 4f
     + _slotSize
     + _padding * 2f;

  public override void Layout(Vector2 screen)
  {
    _slotSize = 20f;
    _padding = 2f;
    _tabH = 22f;

    float panelW = CalcPanelW();
    float panelH = CalcPanelH();

    _panelX = (screen.X - panelW) / 2f;
    _panelY = (screen.Y - panelH) / 2f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    var mouseUi = UiCoordinates.ToUi(new Vector2(mouseX, mouseY));

    _mouseX = mouseUi.X;
    _mouseY = mouseUi.Y;

    float panelW = CalcPanelW();
    float panelH = CalcPanelH();

    // Dim background
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, BgDim);
    GL.Disable(EnableCap.Blend);

    // Panel
    DrawRectUi(_panelX, _panelY, panelW, panelH, screen, PanelBg);

    // Tabs
    float tabW = (_slotSize + _padding) * 2f;
    float tabsStartX = _panelX + _padding + 4f;

    for (int i = 0; i < _tabs.Count; i++)
    {
      float tx = tabsStartX + i * (tabW + _padding);
      bool active = i == _activeTab;
      bool hover = _mouseX >= tx && _mouseX <= tx + tabW
        && _mouseY >= _panelY
        && _mouseY <= _panelY + _tabH;

      DrawRectUi(
        tx,
        _panelY + 2f,
        tabW,
        _tabH - 2f,
        screen,
        active ? TabActive : hover ? SlotHover : TabInactive);

      DrawRectUi(tx, _panelY + 2f, tabW, 1f, screen, active ? BorderLight : BorderDark);
      DrawRectUi(tx, _panelY + 2f, 1f, _tabH - 2f, screen, BorderDark);
      DrawRectUi(tx + tabW, _panelY + 2f, 1f, _tabH - 2f, screen, BorderLight);

      const float textScale = 0.8f;
      float tw = Text.MeasureTextWidth(_tabs[i].name, textScale);

      DrawTextUi(
        _tabs[i].name,
        tx + (tabW - tw) / 2f,
        _panelY + 4f,
        screen,
        textScale,
        active ? BorderDark : new Vector4(0.7f, 0.7f, 0.7f, 1f));
    }

    // Grid
    float gridX = _panelX + _padding + 4f;
    float gridY = _panelY + _tabH + _padding;

    int invTab = _tabs.FindIndex(t => t.name == "Inv");
    bool isInvTab = _activeTab == invTab;

    if (isInvTab)
      DrawPlayerInventoryGrid(gridX, gridY, screen);
    else
      DrawCreativeGrid(gridX, gridY, screen);

    // Hotbar
    float hotbarY = gridY + GridRows * (_slotSize + _padding) + _padding * 2f;
    DrawHotbar(gridX, hotbarY, screen);

    // Held block follows mouse
    if (_heldBlock != Block.Air && _textures != null)
    {
      DrawIconUi(
        _heldBlock,
        _mouseX - _slotSize / 2f,
        _mouseY - _slotSize / 2f,
        _slotSize,
        screen);
    }
  }

  private void DrawPlayerInventoryGrid(float gridX, float gridY, Vector2 screen)
  {
    for (int row = 0; row < GridRows; row++)
    {
      for (int col = 0; col < GridCols; col++)
      {
        int slot = 9 + row * 9 + col;
        float sx = gridX + col * (_slotSize + _padding);
        float sy = gridY + row * (_slotSize + _padding);

        DrawSlot(sx, sy, _slotSize, _inventory.GetSlot(slot), screen);
      }
    }
  }

  private void DrawCreativeGrid(float gridX, float gridY, Vector2 screen)
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

        DrawSlotEmpty(sx, sy, _slotSize, screen);

        if (idx < items.Count)
          DrawSlotBlock(sx, sy, _slotSize, items[idx], screen);
      }
    }

    if (totalRows > GridRows)
      DrawScrollbar(gridX, gridY, totalRows, maxScroll, screen);
  }

  private void DrawScrollbar(float gridX, float gridY, int totalRows, float maxScroll, Vector2 screen)
  {
    const float scrollW = 8f;
    float sbX = gridX + GridCols * (_slotSize + _padding) + _padding;
    float sbH = GridRows * (_slotSize + _padding);
    float barH = sbH * GridRows / totalRows;
    float barY = gridY + (maxScroll > 0 ? _scrollOffset / maxScroll * (sbH - barH) : 0);

    DrawRectUi(sbX, gridY, scrollW, sbH, screen, ScrollBg);
    DrawRectUi(sbX, barY, scrollW, barH, screen, ScrollBar);
  }

  private void DrawHotbar(float gridX, float hotbarY, Vector2 screen)
  {
    for (int col = 0; col < GridCols; col++)
    {
      float sx = gridX + col * (_slotSize + _padding);

      if (col == _inventory.SelectedHotbarSlot)
      {
        DrawRectUi(
          sx - 2f,
          hotbarY - 2f,
          _slotSize + 4f,
          _slotSize + 4f,
          screen,
          BorderLight);
      }

      DrawSlot(sx, hotbarY, _slotSize, _inventory.GetHotbar(col), screen);
    }
  }

  private void DrawSlotEmpty(float sx, float sy, float size, Vector2 screen)
  {
    bool hover = _mouseX >= sx && _mouseX <= sx + size
          && _mouseY >= sy && _mouseY <= sy + size;

    const float border = 1f;

    DrawRectUi(sx - border, sy - border, size + border * 2f, border, screen, BorderDark);
    DrawRectUi(sx - border, sy - border, border, size + border * 2f, screen, BorderDark);
    DrawRectUi(sx - border, sy + size, size + border * 2f, border, screen, BorderLight);
    DrawRectUi(sx + size, sy - border, border, size + border * 2f, screen, BorderLight);
    DrawRectUi(sx, sy, size, size, screen, hover ? SlotHover : SlotBg);
  }

  private void DrawSlotBlock(float sx, float sy, float size, Block block, Vector2 screen)
  {
    if (block != Block.Air && _textures != null)
      DrawIconUi(block, sx, sy, size, screen);
  }

  private void DrawSlot(float sx, float sy, float size, Block block, Vector2 screen)
  {
    DrawSlotEmpty(sx, sy, size, screen);
    DrawSlotBlock(sx, sy, size, block, screen);
  }

  public override void HandleClick(float mx, float my)
  {
    var mouseUi = UiCoordinates.ToUi(new Vector2(mx, my));

    float hitMx = mouseUi.X;
    float hitMy = mouseUi.Y;
    float tabW = (_slotSize + _padding) * 2f;
    float tabsStartX = _panelX + _padding + 4f;

    // Tabs
    for (int i = 0; i < _tabs.Count; i++)
    {
      float tx = tabsStartX + i * (tabW + _padding);

      if (hitMx >= tx && hitMx <= tx + tabW && hitMy >= _panelY && hitMy <= _panelY + _tabH)
      {
        _activeTab = i;
        _scrollOffset = 0f;
        return;
      }
    }

    float gridX = _panelX + _padding + 4f;
    float gridY = _panelY + _tabH + _padding;
    float hotbarY = gridY + GridRows * (_slotSize + _padding) + _padding * 2f;

    int invTab = _tabs.FindIndex(t => t.name == "Inv");
    bool isInvTab = _activeTab == invTab;

    if (isInvTab)
    {
      if (HandlePlayerInventoryClick(hitMx, hitMy, gridX, gridY))
        return;
    }
    else
    {
      if (HandleCreativeGridClick(hitMx, hitMy, gridX, gridY))
        return;
    }

    if (HandleHotbarClick(hitMx, hitMy, gridX, hotbarY))
      return;
  }

  private bool HandlePlayerInventoryClick(float mx, float hitMy, float gridX, float gridY)
  {
    for (int row = 0; row < GridRows; row++)
    {
      for (int col = 0; col < GridCols; col++)
      {
        int slot = 9 + row * 9 + col;

        float sx = gridX + col * (_slotSize + _padding);
        float sy = gridY + row * (_slotSize + _padding);

        if (mx >= sx && mx <= sx + _slotSize
         && hitMy >= sy && hitMy <= sy + _slotSize)
        {
          SwapWithHeld(slot);
          return true;
        }
      }
    }

    return false;
  }

  private bool HandleCreativeGridClick(float mx, float my, float gridX, float gridY)
  {

    var items = _tabs[_activeTab].items;
    int startRow = (int)(_scrollOffset / (_slotSize + _padding));

    for (int row = 0; row < GridRows; row++)
    {
      for (int col = 0; col < GridCols; col++)
      {
        int idx = (startRow + row) * GridCols + col;

        float sx = gridX + col * (_slotSize + _padding);
        float sy = gridY + row * (_slotSize + _padding);

        if (mx >= sx && mx <= sx + _slotSize
          && my >= sy && my <= sy + _slotSize)
        {
          if (_heldBlock != Block.Air)
          {
            DropHeld();
            return true;
          }

          if (idx < items.Count)
          {
            PickCreative(items[idx]);
            return true;
          }

          return true;
        }
      }
    }

    return false;
  }

  private bool HandleHotbarClick(float mx, float hitMy, float gridX, float hotbarY)
  {
    for (int col = 0; col < GridCols; col++)
    {
      float sx = gridX + col * (_slotSize + _padding);

      if (mx >= sx && mx <= sx + _slotSize
       && hitMy >= hotbarY && hitMy <= hotbarY + _slotSize)
      {
        Block current = _inventory.GetHotbar(col);

        if (_heldBlock != Block.Air || current != Block.Air)
          SwapWithHeld(col);
        else
          _inventory.SelectedHotbarSlot = col;

        return true;
      }
    }

    return false;
  }

  private void SwapWithHeld(int slot)
  {
    Block current = _inventory.GetSlot(slot);
    _inventory.SetSlot(slot, _heldBlock);
    _heldBlock = current;
    _heldFromSlot = slot;
  }

  private void PickCreative(Block block)
  {
    _heldBlock = block;
    _heldFromSlot = -2;
  }

  private void DropHeld()
  {
    _heldBlock = Block.Air;
    _heldFromSlot = -1;
  }

  public void HandleScroll(float delta)
  {
    _scrollOffset -= delta * (_slotSize + _padding);

    if (_activeTab < 0 || _activeTab >= _tabs.Count)
      return;

    int invTab = _tabs.FindIndex(t => t.name == "Inv");
    if (_activeTab == invTab)
    {
      _scrollOffset = 0f;
      return;
    }

    var items = _tabs[_activeTab].items;
    int totalRows = (int)MathF.Ceiling(items.Count / (float)GridCols);
    float maxScroll = Math.Max(0, (totalRows - GridRows) * (_slotSize + _padding));

    _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
  }

  public override void HandleKeyDown(Keys key, bool shift)
  {
    if (key == Keys.Escape || key == Keys.E)
    {
      DropHeld();
      OnClose?.Invoke();
    }
  }

  private void DrawRectUi(
    float x,
    float y,
    float width,
    float height,
    Vector2 screen,
    Vector4 color)
  {
    Text.DrawRect(
      UiCoordinates.ToScreen(x),
      UiCoordinates.ToScreen(y),
      UiCoordinates.ToScreen(width),
      UiCoordinates.ToScreen(height),
      screen,
      color);
  }

  private void DrawTextUi(
    string text,
    float x,
    float y,
    Vector2 screen,
    float scale,
    Vector4 color)
  {
    Text.DrawText(
      text,
      UiCoordinates.ToScreen(x),
      UiCoordinates.ToScreen(y),
      screen,
      scale: UiCoordinates.ToScreen(scale),
      color: color);
  }

  private void DrawIconUi(
    Block block,
    float x,
    float y,
    float size,
    Vector2 screen)
  {
    if (_textures == null)
      return;

    _icon.Draw(
      block,
      UiCoordinates.ToScreen(x),
      UiCoordinates.ToScreen(y),
      UiCoordinates.ToScreen(size),
      screen,
      _textures);
  }

  public override void Update(float deltaTime) { }

  public void Dispose() => _icon.Dispose();
}