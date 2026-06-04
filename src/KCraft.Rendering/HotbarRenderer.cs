// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class HotbarRenderer : IDisposable
{
  private readonly TextRenderer _text;
  private readonly BlockIconRenderer _icon;

  private static float S => UiScale.Scale;
  private static float SlotSize => 20f * S;
  private static float SlotPadding => 2f * S;
  private static float SelectedExtra => 4f * S;
  private static float BorderWidth => 1f * S;

  private static readonly Vector4 SlotBg = new(0x8B / 255f, 0x8B / 255f, 0x8B / 255f, 1.0f);
  private static readonly Vector4 SlotBorderDark = new(0x37 / 255f, 0x37 / 255f, 0x37 / 255f, 1.0f); // Top+Left
  private static readonly Vector4 SlotBorderLight = new(1.0f, 1.0f, 1.0f, 1.0f); // Bottom+Right
  private static readonly Vector4 HotbarBg = new(0xC6 / 255f, 0xC6 / 255f, 0xC6 / 255f, 0.90f);

  public int SelectedSlot { get; set; } = 0;
  public Block[] Slots { get; } = new Block[9]
  {
    Block.Grass, Block.Dirt, Block.Stone,
    Block.OakLog, Block.OakLeaves, Block.Sand,
    Block.Glass, Block.Cobblestone, Block.Gravel,
  };

  public Block SelectedBlock => Slots[SelectedSlot];

  public HotbarRenderer(string fontPath)
  {
    _text = new TextRenderer(fontPath);
    _icon = new BlockIconRenderer();
  }

  public void Draw(Vector2 screen, TextureManager textures)
  {
    const int slots = 9;
    float totalWidth = slots * SlotSize + (slots - 1) * SlotPadding + 8f;
    float hotbarX = (screen.X - totalWidth) / 2f;
    float hotbarY = screen.Y - SlotSize - 12f;

    // Hotbar Hintergrund
    _text.DrawRect(hotbarX - 4f, hotbarY - 4f, totalWidth, SlotSize + 8f, screen, HotbarBg);

    for (int i = 0; i < slots; i++)
    {
      float x = hotbarX + i * (SlotSize + SlotPadding);
      bool selected = i == SelectedSlot;

      if (selected)
      {
        // Selected: 24×24 weißer Rahmen, 2px auf jeder Seite größer
        float sx = x - SelectedExtra / 2f;
        float sy = hotbarY - SelectedExtra / 2f;
        _text.DrawRect(sx, sy, SlotSize + SelectedExtra, SlotSize + SelectedExtra,
            screen, new Vector4(1f, 1f, 1f, 1f));
      }
      else
      {
        // Normal: 1px Dark Border Top+Left, Light Bottom+Right
        _text.DrawRect(x - BorderWidth, hotbarY - BorderWidth,
            SlotSize + BorderWidth * 2, BorderWidth, screen, SlotBorderDark); // Top
        _text.DrawRect(x - BorderWidth, hotbarY - BorderWidth,
            BorderWidth, SlotSize + BorderWidth * 2, screen, SlotBorderDark); // Left
        _text.DrawRect(x - BorderWidth, hotbarY + SlotSize,
            SlotSize + BorderWidth * 2, BorderWidth, screen, SlotBorderLight); // Bottom
        _text.DrawRect(x + SlotSize, hotbarY - BorderWidth,
            BorderWidth, SlotSize + BorderWidth * 2, screen, SlotBorderLight); // Right
      }

      // Slot Background
      _text.DrawRect(x, hotbarY, SlotSize, SlotSize, screen, SlotBg);

      // Block Icon
      var block = Slots[i];
      if (block != Block.Air)
        _icon.Draw(block, x, hotbarY, SlotSize, screen, textures);

      // Slot Nummer
      string num = (i + 1).ToString();
      _text.DrawText(num, x + 1f, hotbarY + 1f, screen,
          scale: 1f,
          color: selected
              ? new Vector4(1f, 1f, 0.33f, 1f)
              : new Vector4(0.6f, 0.6f, 0.6f, 1f));
    }
  }
  public void Dispose()
  {
    _text.Dispose();
    _icon.Dispose();
  }
}