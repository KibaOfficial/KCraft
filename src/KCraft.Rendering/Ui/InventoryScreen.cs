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
    float s = UiScale.Scale;
    _slotSize = 20f * s; // ← größer (war 18)
    _padding = 2f * s;

    float panelW = 9 * (_slotSize + _padding) + _padding * 2 + 8f;
    float panelH = 3 * (_slotSize + _padding)  // 3 main rows
                 + _slotSize                    // hotbar row
                 + _padding * 6                 // padding zwischen rows + border
                 + 22f * s;                     // title

    _invX = (screen.X - panelW) / 2f;
    _invY = (screen.Y - panelH) / 2f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    _mouseX = mouseX;
    _mouseY = mouseY;
    float s = UiScale.Scale;

    float panelW = 9 * (_slotSize + _padding) + _padding * 2 + 8f;
    float panelH = 3 * (_slotSize + _padding) + _slotSize + _padding * 6 + 22f * s;

    // Dimmed Background — mit Blending
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha,
                 BlendingFactor.OneMinusSrcAlpha);
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, BgDim);
    GL.Disable(EnableCap.Blend);

    // Panel
    Text.DrawRect(_invX, _invY, panelW, panelH, screen, PanelBg);

    // Title
    Text.DrawText("Inventory", _invX + 8f, _invY + 4f * s, screen, scale: s, color: BorderDark);

    float startX = _invX + _padding + 4f;
    float startY = _invY + 18f * s; // ← kleiner (war 22)

    // Main Inventory (3 rows × 9)
    for (int row = 0; row < 3; row++)
      for (int col = 0; col < 9; col++)
      {
        int slot = 9 + row * 9 + col;
        float sx = startX + col * (_slotSize + _padding);
        float sy = startY + row * (_slotSize + _padding);
        DrawSlot(sx, sy, slot, screen);
      }

    // Hotbar — mit extra Abstand
    float hotbarY = startY + 3 * (_slotSize + _padding) + _padding * 2;
    for (int col = 0; col < 9; col++)
    {
      float sx = startX + col * (_slotSize + _padding);
      DrawSlot(sx, hotbarY, col, screen);
    }

    // Held Block
    if (_heldBlock != Block.Air && _textures != null)
      _icon.Draw(_heldBlock, mouseX - _slotSize / 2f, mouseY - _slotSize / 2f, _slotSize, screen, _textures);
  }

  private void DrawSlot(float sx, float sy, int slotIndex, Vector2 screen)
  {
    bool hover = _mouseX >= sx && _mouseX <= sx + _slotSize && _mouseY >= sy && _mouseY <= sy + _slotSize;

    // Border
    Text.DrawRect(sx - 1, sy - 1, _slotSize + 2, 1, screen, BorderDark);  // Top
    Text.DrawRect(sx - 1, sy - 1, 1, _slotSize + 2, screen, BorderDark);  // Left
    Text.DrawRect(sx - 1, sy + _slotSize, _slotSize + 2, 1, screen, BorderLight); // Bottom
    Text.DrawRect(sx + _slotSize, sy - 1, 1, _slotSize + 2, screen, BorderLight); // Right

    // Background
    Text.DrawRect(sx, sy, _slotSize, _slotSize, screen, hover ? SlotHover : SlotBg);

    // Block Icon
    var block = _inventory.GetSlot(slotIndex);
    if (block != Block.Air && _textures != null)
      _icon.Draw(block, sx, sy, _slotSize, screen, _textures);
  }

  public override void HandleClick(float mx, float my)
  {
    float startX = _invX + _padding + 4f;
    float startY = _invY + 18f * UiScale.Scale;
    float hotbarY = startY + 3 * (_slotSize + _padding) + _padding * 2;

    // Main Inventory
    for (int row = 0; row < 3; row++)
      for (int col = 0; col < 9; col++)
      {
        int slot = 9 + row * 9 + col;
        float sx = startX + col * (_slotSize + _padding);
        float sy = startY + row * (_slotSize + _padding);
        if (mx >= sx && mx <= sx + _slotSize && my >= sy && my <= sy + _slotSize)
        { SwapWithHeld(slot); return; }
      }

    // Hotbar
    for (int col = 0; col < 9; col++)
    {
      float sx = startX + col * (_slotSize + _padding);
      if (mx >= sx && mx <= sx + _slotSize && my >= hotbarY && my <= hotbarY + _slotSize)
      { SwapWithHeld(col); return; }
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