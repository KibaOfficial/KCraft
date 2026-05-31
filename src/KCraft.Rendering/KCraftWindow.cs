// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KCraft.Assets;
using KCraft.World;
using KCraft.Rendering.Ui;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  // ── Shader ────────────────────────────────────────────────────────────
  private int _shader;
  private int _uModel, _uView, _uProjection;

  // ── World ─────────────────────────────────────────────────────────────
  private WorldManager _world = null!;
  private WorldTicker _ticker = null!;
  private Camera _camera = null!;

  // ── Assets ────────────────────────────────────────────────────────────
  private TextureManager _textureManager = null!;

  // ── Renderers ─────────────────────────────────────────────────────────
  private SkyRenderer _sky = null!;
  private DebugOverlay _debug = null!;
  private ChunkBorderRenderer _chunkBorders = null!;
  private CrosshairRenderer _crosshair = null!;
  private BlockHighlightRenderer _blockHighlight = null!;
  private UiManager _ui = null!;

  // ── State ─────────────────────────────────────────────────────────────
  private RaycastHit _lastHit;
  private bool _firstMouse = true;
  private bool _freeCam = false;

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
    GL.ClearColor(0f, 0f, 0f, 1f);

    InitShader();
    InitGL();

    _camera = new Camera(new Vector3(8, 65, -10));
    _ticker = new WorldTicker();
    _world = new WorldManager();
    _ticker.Player = new Player(new Vector3(8, 80, -10));
    _ticker.SetGetBlock(_world.GetBlock);
    _textureManager = new TextureManager("assets/dev");

    InitRenderers();
    InitUi();

    _ui.SetState(GameState.MainMenu);
    CursorState = CursorState.Normal;
  }

  protected override void OnUnload()
  {
    base.OnUnload();
    _world.Dispose();
    _textureManager.Dispose();
    _sky.Dispose();
    _debug.Dispose();
    _chunkBorders.Dispose();
    _crosshair.Dispose();
    _blockHighlight.Dispose();
    _ui.Dispose();
    GL.DeleteProgram(_shader);
  }

  // ── Update ────────────────────────────────────────────────────────────

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    if (_ui.State == GameState.Playing)
    {
      if (_freeCam)
      {
        // alter Flug-Modus
        _camera.ProcessKeyboard(KeyboardState, (float)args.Time);
      }
      else
      {
        // Player Input + Tick
        _ticker.Player?.ProcessInput(KeyboardState, _camera.Yaw);
      }

      _ticker.Update((float)args.Time);

      if (!_freeCam && _ticker.Player != null)
      {
        float alpha = _ticker.Accumulator / (1f / WorldTime.TicksPerSecond);
        alpha = Math.Clamp(alpha, 0f, 1f);

        float eyeOffset = _ticker.Player.IsSneaking ? Player.EyeHeightSneak : Player.EyeHeight;
        var prevEye = _ticker.PlayerPrevPosition + new Vector3(0, eyeOffset, 0);
        var currEye = _ticker.Player.EyePosition;
        _camera.Position = Vector3.Lerp(prevEye, currEye, alpha);
      }
    }
  }

  // ── Render ────────────────────────────────────────────────────────────

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    if (_ui.State != GameState.MainMenu)
    {
      float aspect = Size.X / (float)Size.Y;
      var view = _camera.GetViewMatrix();
      var projection = Matrix4.CreatePerspectiveFieldOfView(
        MathHelper.DegreesToRadians(60f), aspect, 0.1f, 500f);

      // Sky
      _sky.Draw(_ticker.Time, view, projection, _camera, aspect);

      // 3D World
      DrawChunks(view, projection);

      // Ray-Cast + Highlight
      _lastHit = BlockRaycaster.Cast(_camera.Position, _camera.Front, 8f, _world.GetBlock);
      if (_lastHit.Hit)
        _blockHighlight.Draw(_lastHit.BlockPos, view, projection);

      _chunkBorders.Draw(_camera, view, projection);

      // 2D Overlays
      GL.Clear(ClearBufferMask.DepthBufferBit);
      _debug.Draw(new Vector2(Size.X, Size.Y), _camera, 1.0 / args.Time,
        _world.ChunkCount, _lastHit, _ticker.Time, _freeCam);
      _crosshair.Draw(new Vector2(Size.X, Size.Y));
    }

    _ui.Draw(new Vector2(Size.X, Size.Y), MouseState.X, MouseState.Y);
    SwapBuffers();
  }

  private void DrawChunks(Matrix4 view, Matrix4 projection)
  {
    GL.UseProgram(_shader);
    GL.UniformMatrix4(_uView, false, ref view);
    GL.UniformMatrix4(_uProjection, false, ref projection);

    foreach (var (mesh, _, chunkPos) in _world.ChunkMeshes)
    {
      var model = Matrix4.CreateTranslation(
        new Vector3(chunkPos.X * Chunk.Width, 0, chunkPos.Z * Chunk.Depth));
      GL.UniformMatrix4(_uModel, false, ref model);
      mesh.Draw(_textureManager,
        GL.GetUniformLocation(_shader, "uTexture"),
        GL.GetUniformLocation(_shader, "uTint"));
    }
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
      case Keys.Space when _ui.State == GameState.Playing:
        _ticker.Player?.Jump();
        break;
      case Keys.N when _ui.State == GameState.Playing:
        if (KeyboardState.IsKeyDown(Keys.F3))
        {
          _freeCam = !_freeCam;
          // Beim Wechsel zurück: Kamera auf Player-Eye snappen
          if (!_freeCam && _ticker.Player != null)
            _camera.Position = _ticker.Player.EyePosition;
        }
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

  // ── Init Helpers ──────────────────────────────────────────────────────

  private void InitShader()
  {
    int vert = CompileShader(ShaderType.VertexShader, VertexShaderSource);
    int frag = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);
    GL.GetProgram(_shader, GetProgramParameterName.LinkStatus, out int linked);
    if (linked == 0) throw new Exception($"Shader link error: {GL.GetProgramInfoLog(_shader)}");
    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    _uModel = GL.GetUniformLocation(_shader, "uModel");
    _uView = GL.GetUniformLocation(_shader, "uView");
    _uProjection = GL.GetUniformLocation(_shader, "uProjection");
  }

  private static void InitGL()
  {
    GL.Enable(EnableCap.DepthTest);
    GL.Enable(EnableCap.CullFace);
    GL.CullFace(TriangleFace.Back);
    GL.FrontFace(FrontFaceDirection.Ccw);
  }

  private void InitRenderers()
  {
    const string font = "assets/dev/font_ascii.png";
    _sky = new SkyRenderer();
    _debug = new DebugOverlay(font);
    _chunkBorders = new ChunkBorderRenderer();
    _crosshair = new CrosshairRenderer(font);
    _blockHighlight = new BlockHighlightRenderer();
  }

  private void InitUi()
  {
    _ui = new UiManager("assets/dev/font_ascii.png");
    _ui.Layout(new Vector2(Size.X, Size.Y));
    _ui.MainMenu.OnSingleplayer += StartGame;
    _ui.MainMenu.OnQuit += Close;
    _ui.PauseMenu.OnResume += ResumeGame;
    _ui.PauseMenu.OnQuitToTitle += QuitToTitle;
  }

  private static int CompileShader(ShaderType type, string source)
  {
    int shader = GL.CreateShader(type);
    GL.ShaderSource(shader, source);
    GL.CompileShader(shader);
    GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
    if (success == 0) throw new Exception($"Shader compile error: {GL.GetShaderInfoLog(shader)}");
    return shader;
  }
}