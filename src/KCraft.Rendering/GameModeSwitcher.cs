// Copyright (c) 2026 KibaOfficial
// All rights reserved.

// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class GameModeSwitcher : IDisposable
{
  private readonly TextRenderer _text;

  public bool Visible { get; set; } = false;
  public GameMode Selected { get; private set; } = GameMode.Survival;

  private static readonly Vector4 BgColor = new(0.0f, 0.0f, 0.0f, 0.6f);
  private static readonly Vector4 SlotBg = new(0.15f, 0.15f, 0.15f, 0.8f);
  private static readonly Vector4 SlotSelected = new(0.3f, 0.3f, 0.3f, 0.95f);
  private static readonly Vector4 BorderSelected = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 TextNormal = new(0.88f, 0.88f, 0.88f, 1.0f);
  private static readonly Vector4 TextSelected = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 TextSub = new(0.6f, 0.6f, 0.6f, 1.0f);

  private static readonly (GameMode mode, string label, string icon)[] Modes =
  [
      (GameMode.Survival,  "Survival",  "♥"),
        (GameMode.Creative,  "Creative",  "✦"),
        (GameMode.Spectator, "Spectator", "👁"),
    ];

  public GameModeSwitcher(string fontPath)
  {
    _text = new TextRenderer(fontPath);
  }

  public void CycleNext()
  {
    int next = ((int)Selected + 1) % Modes.Length;
    Selected = Modes[next].mode;
  }

  public void SetSelected(GameMode mode)
  {
    Selected = mode;
  }

  public void Draw(Vector2 screen)
  {
    if (!Visible) return;

    float scale = UiScale.Scale;
    float slotW = 90f * scale;
    float slotH = 50f * scale;
    float padding = 6f * scale;
    float totalW = Modes.Length * slotW + (Modes.Length - 1) * padding + padding * 2;
    float totalH = slotH + padding * 2 + 20f * scale;

    float panelX = (screen.X - totalW) / 2f;
    float panelY = screen.Y / 2f - totalH / 2f - 40f * scale;

    // Panel Hintergrund
    _text.DrawRect(panelX, panelY, totalW, totalH, screen, BgColor);

    // Titel
    string title = "Select Game Mode";
    float tw = _text.MeasureTextWidth(title, scale);
    _text.DrawText(title, (screen.X - tw) / 2f, panelY + padding, screen,
        scale: scale, color: TextSub);

    // Slots
    for (int i = 0; i < Modes.Length; i++)
    {
      var (mode, label, icon) = Modes[i];
      bool isSelected = mode == Selected;

      float sx = panelX + padding + i * (slotW + padding);
      float sy = panelY + 18f * scale;

      // Border wenn selected
      if (isSelected)
      {
        float b = 2f * scale;
        _text.DrawRect(sx - b, sy - b, slotW + b * 2, slotH + b * 2,
            screen, BorderSelected);
      }

      // Slot Background
      _text.DrawRect(sx, sy, slotW, slotH, screen,
          isSelected ? SlotSelected : SlotBg);

      // Icon
      float iw = _text.MeasureTextWidth(icon, scale * 1.5f);
      _text.DrawText(icon, sx + (slotW - iw) / 2f, sy + 4f * scale,
          screen, scale: scale * 1.5f,
          color: isSelected ? TextSelected : TextNormal);

      // Label
      float lw = _text.MeasureTextWidth(label, scale);
      _text.DrawText(label, sx + (slotW - lw) / 2f, sy + 28f * scale,
          screen, scale: scale,
          color: isSelected ? TextSelected : TextNormal);
    }
  }

  public void Dispose() => _text.Dispose();
}
