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

  // ── Layout, physical coords ───────────────────────────────────────────
  private float _panelX, _panelY, _slotSize, _padding;
  private float _tabH;
  private Vector2 _screen;
  private float _mouseX, _mouseY;
  private float MouseYOffset => _slotSize + _padding;

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
    _screen = screen;

    float s = UiScale.Scale;

    // CreativeInventory arbeitet hier bewusst in physical coords,
    // wie dein normales InventoryScreen.
    _slotSize = 20f * s;
    _padding = 2f * s;
    _tabH = 22f * s;

    float panelW = CalcPanelW();
    float panelH = CalcPanelH();

    _panelX = (screen.X - panelW) / 2f;
    _panelY = (screen.Y - panelH) / 2f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX;
    _mouseY = SlotMouseY(mouseY);

    float s = UiScale.Scale;
    float panelW = CalcPanelW();
    float panelH = CalcPanelH();

    // Dim background
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, BgDim);
    GL.Disable(EnableCap.Blend);

    // Panel
    Text.DrawRect(_panelX, _panelY, panelW, panelH, screen, PanelBg);

    // Tabs
    float tabW = (_slotSize + _padding) * 2f;
    float tabsStartX = _panelX + _padding + 4f;

    for (int i = 0; i < _tabs.Count; i++)
    {
      float tx = tabsStartX + i * (tabW + _padding);
      bool active = i == _activeTab;
      bool hover = mouseX >= tx && mouseX <= tx + tabW
          && SlotMouseY(mouseY) >= _panelY
          && SlotMouseY(mouseY) <= _panelY + _tabH;

      Text.DrawRect(
        tx,
        _panelY + 2f * s,
        tabW,
        _tabH - 2f * s,
        screen,
        active ? TabActive : hover ? SlotHover : TabInactive);

      Text.DrawRect(tx, _panelY + 2f * s, tabW, 1f * s, screen, active ? BorderLight : BorderDark);
      Text.DrawRect(tx, _panelY + 2f * s, 1f * s, _tabH - 2f * s, screen, BorderDark);
      Text.DrawRect(tx + tabW, _panelY + 2f * s, 1f * s, _tabH - 2f * s, screen, BorderLight);

      float textScale = s * 0.8f;
      float tw = Text.MeasureTextWidth(_tabs[i].name, textScale);

      Text.DrawText(
        _tabs[i].name,
        tx + (tabW - tw) / 2f,
        _panelY + 4f * s,
        screen,
        scale: textScale,
        color: active ? BorderDark : new Vector4(0.7f, 0.7f, 0.7f, 1f));
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
      _icon.Draw(
        _heldBlock,
        mouseX - _slotSize / 2f,
        SlotMouseY(mouseY) - _slotSize / 2f,
        _slotSize,
        screen,
        _textures);
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
    float s = UiScale.Scale;
    float scrollW = 8f * s;
    float sbX = gridX + GridCols * (_slotSize + _padding) + _padding;
    float sbH = GridRows * (_slotSize + _padding);
    float barH = sbH * GridRows / totalRows;
    float barY = gridY + (maxScroll > 0 ? _scrollOffset / maxScroll * (sbH - barH) : 0);

    Text.DrawRect(sbX, gridY, scrollW, sbH, screen, ScrollBg);
    Text.DrawRect(sbX, barY, scrollW, barH, screen, ScrollBar);
  }

  private void DrawHotbar(float gridX, float hotbarY, Vector2 screen)
  {
    float s = UiScale.Scale;

    for (int col = 0; col < GridCols; col++)
    {
      float sx = gridX + col * (_slotSize + _padding);

      if (col == _inventory.SelectedHotbarSlot)
      {
        Text.DrawRect(
          sx - 2f * s,
          hotbarY - 2f * s,
          _slotSize + 4f * s,
          _slotSize + 4f * s,
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

    float b = UiScale.Scale;

    Text.DrawRect(sx - b, sy - b, size + b * 2, b, screen, BorderDark);
    Text.DrawRect(sx - b, sy - b, b, size + b * 2, screen, BorderDark);
    Text.DrawRect(sx - b, sy + size, size + b * 2, b, screen, BorderLight);
    Text.DrawRect(sx + size, sy - b, b, size + b * 2, screen, BorderLight);
    Text.DrawRect(sx, sy, size, size, screen, hover ? SlotHover : SlotBg);
  }

  private void DrawSlotBlock(float sx, float sy, float size, Block block, Vector2 screen)
  {
    if (block != Block.Air && _textures != null)
      _icon.Draw(block, sx, sy, size, screen, _textures);
  }

  private void DrawSlot(float sx, float sy, float size, Block block, Vector2 screen)
  {
    DrawSlotEmpty(sx, sy, size, screen);
    DrawSlotBlock(sx, sy, size, block, screen);
  }

  public override void HandleClick(float mx, float my)
  {
    float tabW = (_slotSize + _padding) * 2f;
    float tabsStartX = _panelX + _padding + 4f;
    float hitMy = SlotMouseY(my);

    // Tabs
    for (int i = 0; i < _tabs.Count; i++)
    {
      float tx = tabsStartX + i * (tabW + _padding);

      if (mx >= tx && mx <= tx + tabW && hitMy >= _panelY && hitMy <= _panelY + _tabH)
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
      if (HandlePlayerInventoryClick(mx, hitMy, gridX, gridY))
        return;
    }
    else
    {
      if (HandleCreativeGridClick(mx, hitMy, gridX, gridY))
        return;
    }

    if (HandleHotbarClick(mx, hitMy, gridX, hotbarY))
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

  private float SlotMouseY(float my) => my + _slotSize + _padding;

  public override void Update(float deltaTime) { }

  public void Dispose() => _icon.Dispose();
}