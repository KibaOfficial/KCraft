// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Rendering.Benchmark;
using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class BenchmarkResultScreen : Screen
{
  public event Action? OnBack;
  public BenchmarkData? Data { get; set; }

  private static readonly Vector4 Bg = new(0.05f, 0.05f, 0.08f, 1.00f);
  private static readonly Vector4 Panel = new(0.10f, 0.10f, 0.15f, 1.00f);
  private static readonly Vector4 White = new(1.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 Yellow = new(1.00f, 1.00f, 0.20f, 1.00f);
  private static readonly Vector4 Cyan = new(0.00f, 1.00f, 1.00f, 1.00f);
  private static readonly Vector4 Green = new(0.30f, 1.00f, 0.30f, 1.00f);
  private static readonly Vector4 Gray = new(0.60f, 0.60f, 0.60f, 1.00f);
  private static readonly Vector4 Gold = new(1.00f, 0.84f, 0.00f, 1.00f);
  private static readonly Vector4 Divider = new(0.25f, 0.25f, 0.35f, 1.00f);

  public BenchmarkResultScreen(TextRenderer text) : base(text)
  {
    var back = new Button("Back to Main Menu", 0, 0, 300, 40);
    back.OnClick += () => OnBack?.Invoke();
    Buttons.Add(back);
  }

  public override void Layout(Vector2 screen)
  {
    Buttons[0].X = (screen.X - 300f) / 2f;
    Buttons[0].Y = screen.Y - 60f;
  }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    float s = UiScale.Scale;
    float cx = screen.X / 2f;

    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    if (Data == null)
    {
      Text.DrawText("No benchmark data.", cx - 100f, screen.Y / 2f, screen, color: Gray);
      foreach (var btn in Buttons)
        btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
      return;
    }

    // ── Title ─────────────────────────────────────────────────────────
    string title = "Benchmark Results";
    float tw = Text.MeasureTextWidth(title, s * 2f);
    Text.DrawText(title, cx - tw / 2f, 20f * s,
        screen, scale: s * 2f, color: Yellow);

    // ── Score ─────────────────────────────────────────────────────────
    float panelW = 600f * s;
    float panelX = cx - panelW / 2f;
    float panelY = 55f * s;

    // Score Box
    Text.DrawRect(panelX, panelY, panelW, 60f * s, screen, Panel);
    string scoreLabel = "KCRAFT SCORE";
    float slw = Text.MeasureTextWidth(scoreLabel, s * 0.9f);
    Text.DrawText(scoreLabel, cx - slw / 2f, panelY + 6f * s,
        screen, scale: s * 0.9f, color: Gray);
    string scoreStr = $"{Data.Score:N0}";
    float ssw = Text.MeasureTextWidth(scoreStr, s * 3f);
    Text.DrawText(scoreStr, cx - ssw / 2f, panelY + 18f * s,
        screen, scale: s * 3f, color: Gold);

    // ── FPS Metrics ───────────────────────────────────────────────────
    float col1X = panelX;
    float col2X = panelX + panelW / 2f;
    float rowY = panelY + 68f * s;

    Text.DrawRect(panelX, rowY - 4f * s, panelW, 1f, screen, Divider);

    DrawMetricRow("Avg FPS", $"{Data.AvgFps:F1}", col1X, rowY, s, screen, Green);
    DrawMetricRow("Min FPS", $"{Data.MinFps:F1}", col2X, rowY, s, screen, White);
    DrawMetricRow("1% Low FPS", $"{Data.P1LowFps:F1}", col1X, rowY + 18f * s, s, screen, Cyan);
    DrawMetricRow("0.1% Low FPS", $"{Data.P01LowFps:F1}", col2X, rowY + 18f * s, s, screen, Cyan);
    DrawMetricRow("Avg Frame", $"{Data.AvgFrameMs:F2}ms", col1X, rowY + 36f * s, s, screen, White);
    DrawMetricRow("Chunk Gen Avg", $"{Data.ChunkGenAvgMs:F2}ms", col2X, rowY + 36f * s, s, screen, White);
    DrawMetricRow("Total Chunks", $"{Data.TotalChunks}", col1X, rowY + 54f * s, s, screen, Gray);

    // ── Hardware ──────────────────────────────────────────────────────
    float hwY = rowY + 76f * s;
    Text.DrawRect(panelX, hwY - 4f * s, panelW, 1f, screen, Divider);

    DrawMetricRow("CPU", $"{Data.Cpu} ({Data.CpuCores} cores)", col1X, hwY, s, screen, Gray);
    DrawMetricRow("RAM", $"{Data.RamMb} MB", col1X, hwY + 14f * s, s, screen, Gray);
    DrawMetricRow("GPU", Data.GpuRenderer, col1X, hwY + 28f * s, s, screen, Gray);
    DrawMetricRow("OS", Data.Os, col1X, hwY + 42f * s, s, screen, Gray);

    // ── ID ────────────────────────────────────────────────────────────
    float idW = Text.MeasureTextWidth(Data.Id, s * 0.8f);
    Text.DrawText(Data.Id, cx - idW / 2f, hwY + 58f * s,
        screen, scale: s * 0.8f, color: new Vector4(0.4f, 0.4f, 0.4f, 1f));

    foreach (var btn in Buttons)
      btn.Draw(Text, screen, btn.OnMouseClick(mouseX, mouseY));
  }

  private void DrawMetricRow(string label, string value,
      float x, float y, float s, Vector2 screen, Vector4 valueColor)
  {
    Text.DrawText($"{label}:", x + 4f * s, y, screen, scale: s * 0.85f, color: Gray);
    Text.DrawText(value, x + 100f * s, y, screen, scale: s * 0.85f, color: valueColor);
  }
}