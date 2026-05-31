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

  private const float SlotSize = 40f;
  private const float SlotPadding = 4f;
  private const float BorderWidth = 2f;

  private static readonly Vector4 SlotBg = new(0.15f, 0.15f, 0.15f, 0.80f);
  private static readonly Vector4 SlotBorder = new(0.50f, 0.50f, 0.50f, 1.00f);
  private static readonly Vector4 SelectedBg = new(0.25f, 0.25f, 0.25f, 0.95f);
  private static readonly Vector4 SelectedBorder = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 HotbarBg = new(0.10f, 0.10f, 0.10f, 0.70f);

  public int SelectedSlot { get; set; } = 0;
  public Block[] Slots { get; } = new Block[9]
  {
        Block.Grass, Block.Dirt, Block.Stone,
        Block.Air,   Block.Air,  Block.Air,
        Block.Air,   Block.Air,  Block.Air,
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
      var bg = selected ? SelectedBg : SlotBg;
      var border = selected ? SelectedBorder : SlotBorder;

      // Border
      _text.DrawRect(x - BorderWidth, hotbarY - BorderWidth,
          SlotSize + BorderWidth * 2, SlotSize + BorderWidth * 2, screen, border);
      // Background
      _text.DrawRect(x, hotbarY, SlotSize, SlotSize, screen, bg);

      // Block Icon
      var block = Slots[i];
      if (block != Block.Air)
        _icon.Draw(block, x, hotbarY, SlotSize, screen, textures);

      // Slot Nummer
      string num = (i + 1).ToString();
      _text.DrawText(num, x + 2f, hotbarY + 2f, screen,
          scale: 1f,
          color: selected
              ? new Vector4(1f, 1f, 1f, 1f)
              : new Vector4(0.6f, 0.6f, 0.6f, 1f));
    }
  }

  public void Dispose()
  {
    _text.Dispose();
    _icon.Dispose();
  }
}