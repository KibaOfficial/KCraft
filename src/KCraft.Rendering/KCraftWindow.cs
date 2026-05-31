// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KCraft.Assets;
using KCraft.World;
using KCraft.World.Generation;
using KCraft.Rendering.Ui;
using KCraft.Blocks;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  // ── Shader ───────────────────────────────────────────────────────────
  private int _shader;
  private int _uModel, _uView, _uProjection, _uTint;
  private float _time;

  // ── World ─────────────────────────────────────────────────────────────
  private TextureManager _textureManager = null!;
  private List<(ChunkMesh mesh, Chunk chunk, Vector3i chunkPos)> _chunkMeshes = null!;
  private Camera _camera = null!;

  // ── Rendering ─────────────────────────────────────────────────────────
  private DebugOverlay _debug = null!;
  private ChunkBorderRenderer _chunkBorders = null!;
  private UiManager _ui = null!;
  private CrosshairRenderer _crosshair = null!;
  private BlockHighlightRenderer _blockHighlight = null!;
  private RaycastHit _lastHit;


  // ── Input ─────────────────────────────────────────────────────────────
  private bool _firstMouse = true;

  // ── Shaders ───────────────────────────────────────────────────────────
  private const string VertexShaderSource = """
    #version 410 core
    layout(location = 0) in vec3 aPosition;
    layout(location = 1) in vec2 aTexCoord;
    out vec2 vTexCoord;
    uniform mat4 uModel;
    uniform mat4 uView;
    uniform mat4 uProjection;
    void main()
    {
      vTexCoord   = aTexCoord;
      gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    }
    """;

  private const string FragmentShaderSource = """
    #version 410 core
    in vec2 vTexCoord;
    out vec4 FragColor;
    uniform sampler2D uTexture;
    uniform vec3 uTint;
    void main()
    {
      vec4 color = texture(uTexture, vTexCoord);
      FragColor = vec4(color.rgb * uTint, color.a);
    }
    """;

  public KCraftWindow() : base(
    new GameWindowSettings { UpdateFrequency = 60.0 },
    new NativeWindowSettings
    {
      ClientSize = new Vector2i(1280, 720),
      Title = "KCraft v0.2.0",
      APIVersion = new Version(4, 6),
    })
  { }

  // ── Lifecycle ─────────────────────────────────────────────────────────

  protected override void OnLoad()
  {
    base.OnLoad();
    GL.ClearColor(0.53f, 0.81f, 0.98f, 1.0f);

    // Assets
    _textureManager = new TextureManager("assets/dev");

    // Shader
    int vert = CompileShader(ShaderType.VertexShader, VertexShaderSource);
    int frag = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);
    GL.GetProgram(_shader, GetProgramParameterName.LinkStatus, out int linked);
    if (linked == 0)
      throw new Exception($"Shader link error: {GL.GetProgramInfoLog(_shader)}");
    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    _uModel = GL.GetUniformLocation(_shader, "uModel");
    _uView = GL.GetUniformLocation(_shader, "uView");
    _uProjection = GL.GetUniformLocation(_shader, "uProjection");
    _uTint = GL.GetUniformLocation(_shader, "uTint");

    // GL State
    GL.Enable(EnableCap.DepthTest);
    GL.Enable(EnableCap.CullFace);
    GL.CullFace(TriangleFace.Back);
    GL.FrontFace(FrontFaceDirection.Ccw);

    // Camera
    _camera = new Camera(new Vector3(8, 65, -10));

    // Renderers
    _debug = new DebugOverlay("assets/dev/font_ascii.png");
    _chunkBorders = new ChunkBorderRenderer();
    _ui = new UiManager("assets/dev/font_ascii.png");
    _ui.Layout(new Vector2(Size.X, Size.Y));

    // UI Events
    _ui.MainMenu.OnSingleplayer += StartGame;
    _ui.MainMenu.OnQuit += Close;
    _ui.PauseMenu.OnResume += ResumeGame;
    _ui.PauseMenu.OnQuitToTitle += QuitToTitle;

    // World
    _chunkMeshes = new List<(ChunkMesh, Chunk, Vector3i)>();
    _crosshair = new CrosshairRenderer("assets/dev/font_ascii.png");
    _blockHighlight = new BlockHighlightRenderer();
    var generator = new NoiseWorldGenerator(seed: 42);
    const int renderRadius = 8;
    for (int cx = -renderRadius; cx <= renderRadius; cx++)
      for (int cz = -renderRadius; cz <= renderRadius; cz++)
      {
        var chunk = new Chunk();
        generator.Generate(chunk, cx, cz);
        var mesh = new ChunkMesh();
        mesh.Build(chunk);
        _chunkMeshes.Add((mesh, chunk, new Vector3i(cx, 0, cz)));
      }

    // Start on main menu
    _ui.SetState(GameState.MainMenu);
    CursorState = CursorState.Normal;
  }

  protected override void OnUnload()
  {
    base.OnUnload();
    foreach (var (mesh, _, _) in _chunkMeshes)
      mesh.Dispose();
    _textureManager.Dispose();
    _debug.Dispose();
    _chunkBorders.Dispose();
    _ui.Dispose();
    _crosshair.Dispose();
    _blockHighlight.Dispose();
    GL.DeleteProgram(_shader);
  }

  // ── Update ────────────────────────────────────────────────────────────

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    if (_ui.State == GameState.Playing)
    {
      _camera.ProcessKeyboard(KeyboardState, (float)args.Time);
      _time += (float)args.Time;
    }
  }

  // ── Render ────────────────────────────────────────────────────────────

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    if (_ui.State != GameState.MainMenu)
    {
      var view = _camera.GetViewMatrix();
      var projection = Matrix4.CreatePerspectiveFieldOfView(
        MathHelper.DegreesToRadians(60f), Size.X / (float)Size.Y, 0.1f, 500f);

      // 3D World
      GL.UseProgram(_shader);
      GL.UniformMatrix4(_uView, false, ref view);
      GL.UniformMatrix4(_uProjection, false, ref projection);

      foreach (var (mesh, _, chunkPos) in _chunkMeshes)
      {
        var chunkModel = Matrix4.CreateTranslation(
            new Vector3(chunkPos.X * Chunk.Width, 0, chunkPos.Z * Chunk.Depth));
        GL.UniformMatrix4(_uModel, false, ref chunkModel);
        mesh.Draw(_textureManager,
            GL.GetUniformLocation(_shader, "uTexture"),
            GL.GetUniformLocation(_shader, "uTint"));
      }

      _lastHit = BlockRaycaster.Cast(
        _camera.Position, _camera.Front, 8f, GetBlock);

      if (_lastHit.Hit)
        _blockHighlight.Draw(_lastHit.BlockPos, view, projection);

      _chunkBorders.Draw(_camera, view, projection);

      // 2D Overlays
      GL.Clear(ClearBufferMask.DepthBufferBit);
      _debug.Draw(new Vector2(Size.X, Size.Y), _camera, 1.0 / args.Time, _chunkMeshes.Count, _lastHit);
      _crosshair.Draw(new Vector2(Size.X, Size.Y));
    }

    // UI (always on top)
    _ui.Draw(new Vector2(Size.X, Size.Y), MouseState.X, MouseState.Y);

    SwapBuffers();
  }

  // ── Input ─────────────────────────────────────────────────────────────

  protected override void OnKeyDown(KeyboardKeyEventArgs e)
  {
    base.OnKeyDown(e);
    if (e.IsRepeat) return;

    switch (e.Key)
    {
      case Keys.Escape:
        if (_ui.State == GameState.Playing) PauseGame();
        else if (_ui.State == GameState.Paused) ResumeGame();
        break;

      case Keys.F3 when _ui.State == GameState.Playing:
        if (!KeyboardState.IsKeyDown(Keys.G))
          _debug.Visible = !_debug.Visible;
        break;

      case Keys.G when _ui.State == GameState.Playing:
        if (KeyboardState.IsKeyDown(Keys.F3))
          _chunkBorders.Visible = !_chunkBorders.Visible;
        break;
    }
  }

  protected override void OnMouseDown(MouseButtonEventArgs e)
  {
    base.OnMouseDown(e);
    if (e.Button == MouseButton.Left)
      _ui.HandleClick(MouseState.X, MouseState.Y);
  }

  protected override void OnMouseMove(MouseMoveEventArgs e)
  {
    base.OnMouseMove(e);
    _ui.HandleMouseMove(e.X, e.Y);

    if (_ui.State != GameState.Playing) return;
    if (_firstMouse) { _firstMouse = false; return; }
    _camera.ProcessMouse(e.DeltaX, e.DeltaY);
  }

  protected override void OnResize(ResizeEventArgs e)
  {
    base.OnResize(e);
    GL.Viewport(0, 0, e.Width, e.Height);
    _ui?.Layout(new Vector2(e.Width, e.Height));
  }

  // ── Game State ────────────────────────────────────────────────────────

  private void StartGame()
  {
    _ui.SetState(GameState.Playing);
    CursorState = CursorState.Grabbed;
    _firstMouse = true;
  }

  private void PauseGame()
  {
    _ui.SetState(GameState.Paused);
    CursorState = CursorState.Normal;
  }

  private void ResumeGame()
  {
    _ui.SetState(GameState.Playing);
    CursorState = CursorState.Grabbed;
    _firstMouse = true;
  }

  private void QuitToTitle()
  {
    _ui.SetState(GameState.MainMenu);
    CursorState = CursorState.Normal;
  }

  // ── Helpers ───────────────────────────────────────────────────────────

  private static int CompileShader(ShaderType type, string source)
  {
    int shader = GL.CreateShader(type);
    GL.ShaderSource(shader, source);
    GL.CompileShader(shader);
    GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
    if (success == 0)
      throw new Exception($"Shader compile error: {GL.GetShaderInfoLog(shader)}");
    return shader;
  }

  private Block? GetBlock(int wx, int wy, int wz)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);

    foreach (var (_, chunk, chunkPos) in _chunkMeshes)
    {
      if (chunkPos.X != cx || chunkPos.Z != cz) continue;

      int lx = wx - cx * Chunk.Width;
      int ly = wy;
      int lz = wz - cz * Chunk.Depth;

      if (!chunk.IsInside(lx, ly, lz)) return null;
      var block = chunk.GetBlock(lx, ly, lz);
      return block == Block.Air ? null : block;
    }
    return null;
  }
}