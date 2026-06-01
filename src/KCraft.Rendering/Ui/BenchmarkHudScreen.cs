// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Rendering.Benchmark;
using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class BenchmarkHudScreen : Screen
{
  private static readonly Vector4 Bg = new(0.0f, 0.0f, 0.0f, 0.45f);
  private static readonly Vector4 White = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Yellow = new(1.0f, 1.0f, 0.2f, 1.0f);
  private static readonly Vector4 Green = new(0.3f, 1.0f, 0.3f, 1.0f);
  private static readonly Vector4 BarBg = new(0.2f, 0.2f, 0.2f, 0.8f);
  private static readonly Vector4 BarFill = new(0.2f, 0.7f, 1.0f, 1.0f);

  public BenchmarkSession? Session { get; set; }

  public BenchmarkHudScreen(TextRenderer text) : base(text) { }

  public override void Layout(Vector2 screen) { }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    if (Session == null) return;
    float scale = UiScale.Scale;
    float cx = screen.X / 2f;

    // Top Bar
    Text.DrawRect(0, 0, screen.X, 40f * scale, screen, Bg);

    string label = $"KCraft Benchmark  —  {Session.PhaseLabel}";
    float lw = Text.MeasureTextWidth(label, scale * 1.2f);
    Text.DrawText(label, cx - lw / 2f, 8f * scale,
        screen, scale: scale * 1.2f, color: Yellow);

    // Progress Bar
    float barW = 400f * scale;
    float barH = 12f * scale;
    float barX = cx - barW / 2f;
    float barY = 26f * scale;

    Text.DrawRect(barX, barY, barW, barH, screen, BarBg);
    Text.DrawRect(barX, barY, barW * Session.Progress, barH, screen, BarFill);

    // Prozent
    string pct = $"{Session.Progress * 100f:F0}%";
    float pw = Text.MeasureTextWidth(pct, scale);
    Text.DrawText(pct, cx - pw / 2f, barY + barH + 4f * scale,
        screen, scale: scale, color: White);
  }
}