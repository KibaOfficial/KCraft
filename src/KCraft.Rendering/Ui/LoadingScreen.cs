// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public sealed class LoadingScreen : Screen
{
  // ── State ─────────────────────────────────────────────────────────────
  public int LoadedChunks { get; set; } = 0;
  public int TargetChunks { get; set; } = 289; // RenderRadius 8 = 17×17
  public bool IsReady => LoadedChunks >= TargetChunks;

  // Chunk Map — welche Chunks geladen sind
  private readonly HashSet<(int cx, int cz)> _loadedSet = [];
  private int _centerCx, _centerCz;
  private const int MapRadius = 8;

  // ── Farben ────────────────────────────────────────────────────────────
  private static readonly Vector4 Bg = new(0.05f, 0.05f, 0.05f, 1.0f);
  private static readonly Vector4 ChunkEmpty = new(0.15f, 0.15f, 0.15f, 1.0f);
  private static readonly Vector4 ChunkDone = new(0.40f, 0.75f, 0.40f, 1.0f);
  private static readonly Vector4 BarBg = new(0.20f, 0.20f, 0.20f, 1.0f);
  private static readonly Vector4 BarFill = new(0.25f, 0.65f, 0.25f, 1.0f);
  private static readonly Vector4 White = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Gray = new(0.6f, 0.6f, 0.6f, 1.0f);

  public LoadingScreen(TextRenderer text) : base(text) { }

  public void SetCenter(int cx, int cz)
  {
    _centerCx = cx;
    _centerCz = cz;
  }

  public void MarkLoaded(int cx, int cz) => _loadedSet.Add((cx, cz));

  public void Reset()
  {
    _loadedSet.Clear();
    LoadedChunks = 0;
  }

  public override void Layout(Vector2 screen) { }

  public override void Draw(Vector2 screen, float mouseX, float mouseY)
  {
    float s = UiScale.Scale;
    float cx = screen.X / 2f;
    float cy = screen.Y / 2f;

    // Hintergrund
    Text.DrawRect(0, 0, screen.X, screen.Y, screen, Bg);

    // Titel
    string title = "Loading World...";
    float tw = Text.MeasureTextWidth(title, s * 1.5f);
    Text.DrawText(title, cx - tw / 2f, cy - 140f * s,
        screen, scale: s * 1.5f, color: White);

    // Chunk Colormap
    int mapSize = MapRadius * 2 + 1; // 17×17
    float cellSize = 8f * s;
    float mapW = mapSize * cellSize;
    float mapH = mapSize * cellSize;
    float mapX = cx - mapW / 2f;
    float mapY = cy - mapH / 2f - 20f * s;

    for (int dz = -MapRadius; dz <= MapRadius; dz++)
      for (int dx = -MapRadius; dx <= MapRadius; dx++)
      {
        float px = mapX + (dx + MapRadius) * cellSize;
        float py = mapY + (dz + MapRadius) * cellSize;

        bool loaded = _loadedSet.Contains((_centerCx + dx, _centerCz + dz));
        var color = loaded ? ChunkDone : ChunkEmpty;

        Text.DrawRect(px + 1, py + 1, cellSize - 2, cellSize - 2, screen, color);
      }

    // Progress Bar
    float progress = TargetChunks > 0
        ? Math.Clamp(LoadedChunks / (float)TargetChunks, 0f, 1f)
        : 0f;

    float barW = 300f * s;
    float barH = 14f * s;
    float barX = cx - barW / 2f;
    float barY = mapY + mapH + 20f * s;

    Text.DrawRect(barX, barY, barW, barH, screen, BarBg);
    Text.DrawRect(barX, barY, barW * progress, barH, screen, BarFill);

    // Prozent + Count
    string pct = $"{progress * 100f:F0}%  ({LoadedChunks} / {TargetChunks} chunks)";
    float pw = Text.MeasureTextWidth(pct, s);
    Text.DrawText(pct, cx - pw / 2f, barY + barH + 6f * s,
        screen, scale: s, color: Gray);
  }
}