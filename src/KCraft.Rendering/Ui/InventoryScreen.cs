// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public sealed class InventoryScreen : Screen
{
  public event Action? OnClose;

  private readonly PlayerInventory _inventory;
  private readonly BlockIconRenderer _icon;
  private TextureManager? _textures;

  // Layout
  private float _invX, _invY, _slotSize, _padding;
  private Vector2 _screen;

  // Drag State
  private Block _heldBlock = Block.Air;
  private int _heldFromSlot = -1; // -1 = not dragging, 0-8 = hotbar, 9-35 = inventory
  private float _mouseX, _mouseY;

  // Colors
  private static readonly Vector4 BgDim = new(0f, 0f, 0f, 0.5f);
  private static readonly Vector4 PanelBg = new(0xC6 / 255f, 0xC6 / 255f, 0xC6 / 255f, 1f);
  private static readonly Vector4 SlotBg = new(0x8B / 255f, 0x8B / 255f, 0x8B / 255f, 1f);
  private static readonly Vector4 SlotHover = new(0xA0 / 255f, 0xA0 / 255f, 0xA0 / 255f, 1f);
  private static readonly Vector4 BorderDark = new(0x37 / 255f, 0x37 / 255f, 0x37 / 255f, 1f);
  private static readonly Vector4 BorderLight = new(1f, 1f, 1f, 1f);
  private static readonly Vector4 White = new(1f, 1f, 1f, 1f);

  public InventoryScreen(TextRenderer text, PlayerInventory inventory) : base(text)
  {
    _inventory = inventory;
    _icon = new BlockIconRenderer();
  }

  public void SetTextures(TextureManager textures)
  {
    _textures = textures;
  }

  public override void Layout(Vector2 screen)
  {
    _screen = screen;

    _slotSize = 20f;
    _padding = 2f;

    float panelW = 9 * (_slotSize + _padding) + _padding * 2 + 8f;
    float panelH = 3 * (_slotSize + _padding)
                 + _slotSize
                 + _padding * 6
                 + 22f;

    _invX = (screen.X - panelW) / 2f;
    _invY = (screen.Y - panelH) / 2f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    var mouseUi = UiCoordinates.ToUi(new Vector2(mouseX, mouseY));

    _mouseX = mouseUi.X;
    _mouseY = mouseUi.Y;

    float panelW = 9 * (_slotSize + _padding) + _padding * 2 + 8f;
    float panelH = 3 * (_slotSize + _padding)
                 + _slotSize
                 + _padding * 6
                 + 22f;

    // Dimmed Background
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(
      BlendingFactor.SrcAlpha,
      BlendingFactor.OneMinusSrcAlpha);

    Text.DrawRect(0, 0, screen.X, screen.Y, screen, BgDim);

    GL.Disable(EnableCap.Blend);

    // Panel
    Text.DrawRect(
      UiCoordinates.ToScreen(_invX),
      UiCoordinates.ToScreen(_invY),
      UiCoordinates.ToScreen(panelW),
      UiCoordinates.ToScreen(panelH),
      screen,
      PanelBg);

    // Title
    Text.DrawText(
      "Inventory",
      UiCoordinates.ToScreen(_invX + 8f),
      UiCoordinates.ToScreen(_invY + 4f),
      screen,
      scale: UiScale.Scale,
      color: BorderDark);

    float startX = _invX + _padding + 4f;
    float startY = _invY + 18f;

    // Main Inventory
    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 9; col++)
      {
        int slot = 9 + row * 9 + col;

        float sx = startX + col * (_slotSize + _padding);
        float sy = startY + row * (_slotSize + _padding);

        DrawSlot(sx, sy, slot, screen);
      }
    }

    // Hotbar
    float hotbarY =
      startY +
      3 * (_slotSize + _padding) +
      _padding * 2;

    for (int col = 0; col < 9; col++)
    {
      float sx = startX + col * (_slotSize + _padding);
      DrawSlot(sx, hotbarY, col, screen);
    }

    // Held Block
    if (_heldBlock != Block.Air && _textures != null)
    {
      float slotSize = UiCoordinates.ToScreen(_slotSize);

      _icon.Draw(
        _heldBlock,
        mouseX - slotSize / 2f,
        mouseY - slotSize / 2f,
        slotSize,
        screen,
        _textures);
    }
  }

  private void DrawSlot(
  float sx,
  float sy,
  int slotIndex,
  Vector2 screen)
  {
    bool hover =
      _mouseX >= sx &&
      _mouseX <= sx + _slotSize &&
      _mouseY >= sy &&
      _mouseY <= sy + _slotSize;

    float x = UiCoordinates.ToScreen(sx);
    float y = UiCoordinates.ToScreen(sy);
    float size = UiCoordinates.ToScreen(_slotSize);
    float border = UiCoordinates.ToScreen(1f);

    // Border
    Text.DrawRect(
      x - border,
      y - border,
      size + border * 2f,
      border,
      screen,
      BorderDark);

    Text.DrawRect(
      x - border,
      y - border,
      border,
      size + border * 2f,
      screen,
      BorderDark);

    Text.DrawRect(
      x - border,
      y + size,
      size + border * 2f,
      border,
      screen,
      BorderLight);

    Text.DrawRect(
      x + size,
      y - border,
      border,
      size + border * 2f,
      screen,
      BorderLight);

    Text.DrawRect(
      x,
      y,
      size,
      size,
      screen,
      hover ? SlotHover : SlotBg);

    var block = _inventory.GetSlot(slotIndex);

    if (block != Block.Air && _textures != null)
      _icon.Draw(block, x, y, size, screen, _textures);
  }

  public override void HandleClick(float mx, float my)
  {
    var mouseUi = UiCoordinates.ToUi(new Vector2(mx, my));

    float ux = mouseUi.X;
    float uy = mouseUi.Y;

    float startX = _invX + _padding + 4f;
    float startY = _invY + 18f;

    float hotbarY =
      startY +
      3 * (_slotSize + _padding) +
      _padding * 2;

    // Main Inventory
    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 9; col++)
      {
        int slot = 9 + row * 9 + col;

        float sx = startX + col * (_slotSize + _padding);
        float sy = startY + row * (_slotSize + _padding);

        if (ux >= sx &&
            ux <= sx + _slotSize &&
            uy >= sy &&
            uy <= sy + _slotSize)
        {
          SwapWithHeld(slot);
          return;
        }
      }
    }

    // Hotbar
    for (int col = 0; col < 9; col++)
    {
      float sx = startX + col * (_slotSize + _padding);

      if (ux >= sx &&
          ux <= sx + _slotSize &&
          uy >= hotbarY &&
          uy <= hotbarY + _slotSize)
      {
        SwapWithHeld(col);
        return;
      }
    }
  }

  private void SwapWithHeld(int slot)
  {
    var current = _inventory.GetSlot(slot);
    _inventory.SetSlot(slot, _heldBlock);
    _heldBlock = current;
    _heldFromSlot = slot;
  }

  private void DropHeld()
  {
    // Held zurücklegen wenn außerhalb geklickt
    if (_heldBlock != Block.Air && _heldFromSlot >= 0)
    {
      _inventory.SetSlot(_heldFromSlot, _heldBlock);
      _heldBlock = Block.Air;
      _heldFromSlot = -1;
    }
  }

  public override void HandleKeyDown(Keys key, bool shift)
  {
    if (key == Keys.Escape || key == Keys.E)
    {
      DropHeld();
      OnClose?.Invoke();
    }
  }

  public override void Update(float deltaTime) { }

  public void Dispose() => _icon.Dispose();
}