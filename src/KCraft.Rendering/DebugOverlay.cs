// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using KCraft.World;
using KCraft.Core;

namespace KCraft.Rendering;

public sealed class DebugOverlay : IDisposable
{
  private readonly TextRenderer _text;
  private readonly string _glVersion;
  private readonly string _gpu;
  private float _textScale = 2f;

  public bool Visible { get; set; } = false;

  public DebugOverlay(string fontPath)
  {
    _text = new TextRenderer(fontPath);
    _glVersion = GL.GetString(StringName.Version) ?? "unknown";
    _gpu = GL.GetString(StringName.Renderer) ?? "unknown";
  }

  public void Draw(Vector2 screen, Camera camera, double fps,
    int chunks, int visibleChunks, RaycastHit lastHit, WorldTime time, bool freeCam, bool _hitboxVisible)
  {
    if (!Visible) return;

    var pos = camera.Position;
    var block = new Vector3i(
      (int)MathF.Floor(pos.X),
      (int)MathF.Floor(pos.Y),
      (int)MathF.Floor(pos.Z));
    var chunk = new Vector2i(
      FloorDiv(block.X, Chunk.Width),
      FloorDiv(block.Z, Chunk.Depth));
    var relative = new Vector3i(
      FloorMod(block.X, Chunk.Width),
      FloorMod(block.Y, Chunk.Height),
      FloorMod(block.Z, Chunk.Depth));

    float leftX = 6;
    float leftY = 6;
    float rightY = 6;
    _textScale = UiScale.Scale;
    float lineH = 9f * _textScale;

    LineParts(leftX, ref leftY, lineH, screen,
      ("KCraft ", Orange),
      ($"v{KCraftVersion.Version}", Green));
    LineParts(leftX, ref leftY, lineH, screen,
      ($"{fps:F0} fps ", Green),
      ($"/ {1000.0 / fps:F1} ms", Cyan));
    LineParts(leftX, ref leftY, lineH, screen,
      ("Render Distance: ", Orange),
      ("8", Cyan));
    Gap(ref leftY);
    LineParts(leftX, ref leftY, lineH, screen,
      ("Time: ", Orange),
      ($"{time.TimeString} ", Yellow),
      ($"(Day {time.Day + 1})", Cyan));

    LineParts(leftX, ref leftY, lineH, screen,
      ("XYZ: ", Red),
      ($"{pos.X:F3} ", Red),
      ($"{pos.Y:F3} ", Green),
      ($"{pos.Z:F3}", Cyan));
    LineParts(leftX, ref leftY, lineH, screen,
      ("Block: ", Red),
      ($"{block.X} ", Red),
      ($"{block.Y} ", Green),
      ($"{block.Z}", Cyan));
    LineParts(leftX, ref leftY, lineH, screen,
      ("Chunk Relative: ", Orange),
      ($"{relative.X} ", Red),
      ($"{relative.Y} ", Green),
      ($"{relative.Z}", Cyan));
    LineParts(leftX, ref leftY, lineH, screen,
      ("Chunk Coordinates: ", Orange),
      ($"{chunk.X} ", Red),
      ($"{chunk.Y}", Cyan));
    Gap(ref leftY);

    LineParts(leftX, ref leftY, lineH, screen,
    ("Loaded Chunks: ", Blue),
    ($"{chunks}", Yellow));
    LineParts(leftX, ref leftY, lineH, screen,
        ("Chunk Meshes: ", Blue),
        ($"{visibleChunks}", Yellow),
        (" / ", Gray),
        ($"{chunks}", Yellow));
    LineParts(leftX, ref leftY, lineH, screen,
        ("Chunk Culling: ", Blue),
        ("Enabled", Green)); // ← war Disabled
    LineParts(leftX, ref leftY, lineH, screen,
      ("Dimension: ", Green),
      ("kcraft:overworld", Cyan));
    LineParts(leftX, ref leftY, lineH, screen,
      ("Facing: ", Green),
      ($"{Facing(camera.Yaw)} ", White),
      ($"(Yaw {camera.Yaw:F1} Pitch {camera.Pitch:F1})", Cyan));
    Gap(ref leftY);

    LineParts(leftX, ref leftY, lineH, screen,
      ("[F3] ", Yellow),
      ("Toggle Debug", White));
    LineParts(leftX, ref leftY, lineH, screen,
      ("[F3+G] ", Yellow),
      ("Chunk Borders", Gray));
    LineParts(leftX, ref leftY, lineH, screen,
      ("[F3+N] ", Yellow),
      (freeCam ? "Free Cam" : "Player Cam", freeCam ? Green : Gray));
    LineParts(leftX, ref leftY, lineH, screen,
      ("[F3+B] ", Yellow),
      ("Hitboxes", _hitboxVisible ? Green : Gray));

    RightLine(ref rightY, lineH, screen,
      (".NET Version: ", Orange),
      (Environment.Version.ToString(), Cyan));
    RightLine(ref rightY, lineH, screen,
      ("Memory Usage: ", Orange),
      ($"{GC.GetTotalMemory(false) / 1024 / 1024} MB", Green));
    RightLine(ref rightY, lineH, screen,
      ("Display: ", Orange),
      ($"{screen.X:F0} x {screen.Y:F0}", Cyan));
    RightLine(ref rightY, lineH, screen,
      ("OpenGL Version: ", Orange),
      (Shorten(_glVersion, 26), Cyan));
    RightLine(ref rightY, lineH, screen,
      ("GPU: ", Orange),
      (Shorten(_gpu, 30), Cyan));
    Gap(ref rightY);
    RightLine(ref rightY, lineH, screen,
      ("Targeted Block: ", Blue),
      (lastHit.Hit ? $"{lastHit.BlockPos.X}, {lastHit.BlockPos.Y}, {lastHit.BlockPos.Z}" : "-", Yellow));
    RightLine(ref rightY, lineH, screen,
      ("Block ID: ", Blue),
      (lastHit.Hit ? $"kcraft:{lastHit.Block.ToString().ToLower()}" : "kcraft:unknown", Yellow));
    RightLine(ref rightY, lineH, screen,
      ("Targeted Fluid: ", Blue),
      ("-", Yellow));
  }

  private void LineParts(float x, ref float y, float lineH, Vector2 screen,
    params (string Text, Vector4 Color)[] parts)
  {
    float width = MeasureParts(parts);
    _text.DrawRect(x - 2, y - 1, width + 4, lineH - 1, screen, Background);

    float cx = x;
    foreach (var (text, color) in parts)
    {
      _text.DrawText(text, cx, y, screen, scale: _textScale, color: color);
      cx += _text.MeasureTextWidth(text, _textScale);
    }
    y += lineH;
  }

  private void RightLine(ref float y, float lineH, Vector2 screen,
    params (string Text, Vector4 Color)[] parts)
  {
    float width = MeasureParts(parts);

    LineParts(screen.X - width - 8, ref y, lineH, screen, parts);
  }

  private float MeasureParts((string Text, Vector4 Color)[] parts)
  {
    float width = 0;
    foreach (var (text, _) in parts)
      width += _text.MeasureTextWidth(text, _textScale);
    return width;
  }

  private static void Gap(ref float y) => y += 8;

  private static int FloorDiv(int value, int divisor)
  {
    int result = value / divisor;
    int remainder = value % divisor;
    return remainder < 0 ? result - 1 : result;
  }

  private static int FloorMod(int value, int divisor)
  {
    int result = value % divisor;
    return result < 0 ? result + divisor : result;
  }

  private static string Facing(float yaw)
  {
    float normalized = (yaw % 360 + 360) % 360;
    return normalized switch
    {
      >= 45 and < 135 => "South",
      >= 135 and < 225 => "West",
      >= 225 and < 315 => "North",
      _ => "East"
    };
  }

  public void DrawFullscreenRect(Vector2 screen, Vector4 color)
  {
    _text.DrawRect(0, 0, screen.X, screen.Y, screen, color);
  }

  private static string Shorten(string text, int maxLength)
    => text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

  private static readonly Vector4 White = new(1.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Gray = new(0.65f, 0.65f, 0.65f, 1.0f);
  private static readonly Vector4 Red = new(1.0f, 0.25f, 0.25f, 1.0f);
  private static readonly Vector4 Orange = new(1.0f, 0.6f, 0.0f, 1.0f);
  private static readonly Vector4 Yellow = new(1.0f, 1.0f, 0.2f, 1.0f);
  private static readonly Vector4 Green = new(0.25f, 1.0f, 0.25f, 1.0f);
  private static readonly Vector4 Cyan = new(0.0f, 1.0f, 1.0f, 1.0f);
  private static readonly Vector4 Blue = new(0.0f, 0.55f, 1.0f, 1.0f);
  private static readonly Vector4 Background = new(0.0f, 0.0f, 0.0f, 0.35f);

  public void Dispose() => _text.Dispose();
}
