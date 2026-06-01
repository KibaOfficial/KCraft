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
  private int _uModel, _uView, _uProjection, _uAmbient;

  // ── World ─────────────────────────────────────────────────────────────
  private WorldManager _world = null!;
  private WorldTicker _ticker = null!;
  private Camera _camera = null!;
  private GameModeSwitcher _gameModeSwitcher = null!;
  private GameMode _currentGameMode = GameMode.Survival;
  private string _currentWorldName = "default";

  // ── Assets ────────────────────────────────────────────────────────────
  private TextureManager _textureManager = null!;

  // ── Renderers ─────────────────────────────────────────────────────────
  private SkyRenderer _sky = null!;
  private DebugOverlay _debug = null!;
  private ChunkBorderRenderer _chunkBorders = null!;
  private CrosshairRenderer _crosshair = null!;
  private BlockHighlightRenderer _blockHighlight = null!;
  private UiManager _ui = null!;
  private HotbarRenderer _hotbar = null!;
  private HitboxRenderer _hitbox = null!;

  // ── State ─────────────────────────────────────────────────────────────
  private RaycastHit _lastHit;
  private bool _firstMouse = true;
  private bool _freeCam = false;
  private float _jumpPressTimer = 0f;
  private bool _jumpPressedLastFrame = false;

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
    uniform float uAmbient;
    void main()
    {
      vec4 color = texture(uTexture, vTexCoord);
      if (color.a < 0.1) discard;
      FragColor = vec4(color.rgb * uTint * uAmbient, color.a);
    }
    """;

  public KCraftWindow() : base(
    new GameWindowSettings { UpdateFrequency = 60.0 },
    new NativeWindowSettings
    {
      ClientSize = new Vector2i(1280, 720),
      Title = "KCraft v0.3.0",
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
    _textureManager = new TextureManager("assets/dev/faithful");

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
    _hotbar.Dispose();
    _hitbox.Dispose();
    _gameModeSwitcher.Dispose();
    GL.DeleteProgram(_shader);
  }

  // ── Update ────────────────────────────────────────────────────────────

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    if (_ui.State == GameState.Playing)
    {
      _ui.Update((float)args.Time);
      bool jumpNow = KeyboardState.IsKeyDown(Keys.Space);
      if (jumpNow && !_jumpPressedLastFrame)
      {
        if (_jumpPressTimer > 0f)
        {
          _ticker.Player?.ToggleFly();
          _jumpPressTimer = 0f;
        }
        else
        {
          _jumpPressTimer = 0.3f;
          _ticker.Player?.Jump(); // nur springen, kein Fly toggle
        }
      }
      if (_jumpPressTimer > 0f)
        _jumpPressTimer -= (float)args.Time;

      _jumpPressedLastFrame = jumpNow;
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

    if (_ui.State == GameState.Playing || _ui.State == GameState.Paused)
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

      if (_ticker.Player != null && !_freeCam)
        _hitbox.Draw(_ticker.Player.BoundingBox, view, projection);

      // 2D Overlays
      GL.Clear(ClearBufferMask.DepthBufferBit);
      _debug.Draw(new Vector2(Size.X, Size.Y), _camera, 1.0 / args.Time,
        _world.ChunkCount, _lastHit, _ticker.Time, _freeCam, _hitbox.Visible);
      _gameModeSwitcher.Draw(new Vector2(Size.X, Size.Y));
      _crosshair.Draw(new Vector2(Size.X, Size.Y));
      _hotbar.Draw(new Vector2(Size.X, Size.Y), _textureManager);
    }

    _ui.Draw(new Vector2(Size.X, Size.Y), MouseState.X, MouseState.Y);
    SwapBuffers();
  }

  private void DrawChunks(Matrix4 view, Matrix4 projection)
  {
    GL.UseProgram(_shader);
    GL.UniformMatrix4(_uView, false, ref view);
    GL.UniformMatrix4(_uProjection, false, ref projection);

    float skyLight = _ticker.Time.SkyLight;
    float ambient = Math.Clamp(skyLight * (1.0f - 0.267f) + 0.267f, 0.267f, 1.0f);
    GL.Uniform1(_uAmbient, ambient);

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
    _ui.HandleKeyDown(e.Key, KeyboardState.IsKeyDown(Keys.LeftShift));

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
      case Keys.F4 when _ui.State == GameState.Playing:
        if (KeyboardState.IsKeyDown(Keys.F3))
        {
          _gameModeSwitcher.Visible = true;
          _gameModeSwitcher.CycleNext();
        }
        break;
      case Keys.B when _ui.State == GameState.Playing:
        if (KeyboardState.IsKeyDown(Keys.F3))
          _hitbox.Visible = !_hitbox.Visible;
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
      case Keys.G when _ui.State == GameState.Playing:
        if (KeyboardState.IsKeyDown(Keys.F3))
          _chunkBorders.Visible = !_chunkBorders.Visible;
        break;
      case Keys.D1: _hotbar.SelectedSlot = 0; break;
      case Keys.D2: _hotbar.SelectedSlot = 1; break;
      case Keys.D3: _hotbar.SelectedSlot = 2; break;
      case Keys.D4: _hotbar.SelectedSlot = 3; break;
      case Keys.D5: _hotbar.SelectedSlot = 4; break;
      case Keys.D6: _hotbar.SelectedSlot = 5; break;
      case Keys.D7: _hotbar.SelectedSlot = 6; break;
      case Keys.D8: _hotbar.SelectedSlot = 7; break;
      case Keys.D9: _hotbar.SelectedSlot = 8; break;
    }
  }

  protected override void OnKeyUp(KeyboardKeyEventArgs e)
  {
    base.OnKeyUp(e);
    if (e.Key == Keys.F3 && _gameModeSwitcher.Visible)
    {
      _currentGameMode = _gameModeSwitcher.Selected;
      _gameModeSwitcher.Visible = false;
      ApplyGameMode(_currentGameMode);
    }
  }

  protected override void OnMouseDown(MouseButtonEventArgs e)
  {
    base.OnMouseDown(e);

    if (_ui.State == GameState.Playing)
    {
      if (e.Button == MouseButton.Left && _lastHit.Hit)
        _world.BreakBlock(_lastHit.BlockPos);

      if (e.Button == MouseButton.Right && _lastHit.Hit)
      {
        var placePos = _lastHit.BlockPos + _lastHit.FaceNormal;
        var playerAabb = _ticker.Player?.BoundingBox;
        var blockAabb = new AABB(placePos.X, placePos.Y, placePos.Z,
                                  placePos.X + 1, placePos.Y + 1, placePos.Z + 1);
        if (playerAabb == null || !playerAabb.Value.Intersects(blockAabb))
          if (_hotbar.SelectedBlock != Blocks.Block.Air)
            _world.PlaceBlock(placePos, _hotbar.SelectedBlock);
      }

      if (e.Button == MouseButton.Middle && _lastHit.Hit)
        _hotbar.Slots[_hotbar.SelectedSlot] = _lastHit.Block;
    }
    else
    {
      if (e.Button == MouseButton.Left)
        _ui.HandleClick(MouseState.X, MouseState.Y);
    }
  }

  protected override void OnMouseMove(MouseMoveEventArgs e)
  {
    base.OnMouseMove(e);
    _ui.HandleMouseMove(e.X, e.Y);
    if (_ui.State != GameState.Playing) return;
    if (_firstMouse) { _firstMouse = false; return; }
    _camera.ProcessMouse(e.DeltaX, e.DeltaY);
  }

  protected override void OnMouseWheel(MouseWheelEventArgs e)
  {
    base.OnMouseWheel(e);
    if (_ui.State != GameState.Playing) return;
    int delta = e.OffsetY > 0 ? -1 : 1;
    _hotbar.SelectedSlot = (_hotbar.SelectedSlot + delta + 9) % 9;
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
    if (WorldSaveManager.WorldExists("default"))
      LoadWorld();

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

  private void SaveAndQuit()
  {
    SaveWorld();
    _ui.SetState(GameState.MainMenu);
    CursorState = CursorState.Normal;
  }

  private void SaveWorld()
  {
    if (_ticker.Player == null) return;

    var data = new WorldSaveData
    {
      WorldName = "default", // später aus New World Screen
      Seed = _world.Seed,
      PlayerX = _ticker.Player.Position.X,
      PlayerY = _ticker.Player.Position.Y,
      PlayerZ = _ticker.Player.Position.Z,
      CameraYaw = _camera.Yaw,
      CameraPitch = _camera.Pitch,
      GameMode = (int)_currentGameMode,
      TotalTicks = _ticker.Time.TotalTicks,
      LastPlayed = DateTime.Now,
    };

    var chunks = _world.ChunkMeshes
        .Select(c => (c.chunk, c.chunkPos.X, c.chunkPos.Z));

    WorldSaveManager.Save(_currentWorldName, data, chunks);
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
    _uAmbient = GL.GetUniformLocation(_shader, "uAmbient");
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
    _hotbar = new HotbarRenderer(font);
    _hitbox = new HitboxRenderer();
    _gameModeSwitcher = new GameModeSwitcher(font);
  }

  private void InitUi()
  {
    _ui = new UiManager("assets/dev/font_ascii.png");
    _ui.Layout(new Vector2(Size.X, Size.Y));
    _ui.MainMenu.OnQuit += Close;
    _ui.PauseMenu.OnResume += ResumeGame;
    _ui.PauseMenu.OnQuitToTitle += SaveAndQuit;
    _ui.OnWorldSelected += LoadAndStartWorld;
    _ui.OnNewWorldCreate += CreateAndStartWorld;
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

  private void ApplyGameMode(GameMode mode)
  {
    if (_ticker.Player == null) return;
    switch (mode)
    {
      case GameMode.Survival:
        _ticker.Player.IsCreative = false;
        _ticker.Player.IsSpectator = false;
        break;
      case GameMode.Creative:
        _ticker.Player.IsCreative = true;
        _ticker.Player.IsSpectator = false;
        break;
      case GameMode.Spectator:
        _ticker.Player.IsCreative = false;
        _ticker.Player.IsSpectator = true;
        break;
    }
  }

  protected override void OnTextInput(TextInputEventArgs e)
  {
    base.OnTextInput(e);
    _ui.HandleTextInput((char)e.Unicode);
  }

  private void LoadWorld(string name = "default")
  {
    _currentWorldName = name;
    var (data, chunks) = WorldSaveManager.Load(name); // ← name statt "default"
    if (data == null) return;

    _ticker.Player!.Position = new Vector3(data.PlayerX, data.PlayerY, data.PlayerZ);
    _camera.SetRotation(data.CameraYaw, data.CameraPitch);
    _currentGameMode = (GameMode)data.GameMode;
    ApplyGameMode(_currentGameMode);

    foreach (var ((cx, cz), rawData) in chunks)
    {
      for (int i = 0; i < _world.ChunkMeshes.Count; i++)
      {
        var (mesh, chunk, chunkPos) = _world.ChunkMeshes[i];
        if (chunkPos.X != cx || chunkPos.Z != cz) continue;
        chunk.LoadRawBlocks(rawData);
        var newMesh = new ChunkMesh();
        newMesh.Build(chunk);
        mesh.Dispose();
        _world.ChunkMeshes[i] = (newMesh, chunk, chunkPos);
        break;
      }
    }
  }

  private void LoadAndStartWorld(string name)
  {
    LoadWorld(name);
    _ui.SetState(GameState.Playing);
    CursorState = CursorState.Grabbed;
    _firstMouse = true;
  }

  private void CreateAndStartWorld(string name, int? seed)
  {
    _currentWorldName = name;
    int actualSeed = seed ?? Random.Shared.Next();

    _world.Dispose();
    _world = new WorldManager(seed: actualSeed);
    _ticker.SetGetBlock(_world.GetBlock);
    _ticker.Player!.Position = new Vector3(8, 80, -10);
    _camera.SetRotation(-90f, 0f);
    _currentGameMode = GameMode.Survival;
    ApplyGameMode(_currentGameMode);

    _ui.SetState(GameState.Playing);
    CursorState = CursorState.Grabbed;
    _firstMouse = true;
  }
}