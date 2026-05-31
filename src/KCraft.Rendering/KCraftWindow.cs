// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using KCraft.Assets;
using KCraft.World;
using KCraft.World.Generation;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  private int _shader;
  private float _time;
  private int _uModel, _uView, _uProjection;

  private TextureManager _textureManager = null!;
  private List<(ChunkMesh mesh, Vector3 offset)> _chunkMeshes = null!;

  private Camera _camera = null!;
  private bool _firstMouse = true;
  private Vector2 _lastMousePos;

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
    void main()
    {
      FragColor = texture(uTexture, vTexCoord);
    }
    """;

  public KCraftWindow() : base(
    new GameWindowSettings { UpdateFrequency = 60.0 },
    new NativeWindowSettings
    {
      ClientSize = new Vector2i(1280, 720),
      Title = "KCraft v0.1.0",
      APIVersion = new Version(4, 6),
    })
  { }

  protected override void OnLoad()
  {
    base.OnLoad();
    GL.ClearColor(0.53f, 0.81f, 0.98f, 1.0f); // Himmelblau
    _textureManager = new TextureManager("assets/dev");

    // Compile Shaders
    int vert = CompileShader(ShaderType.VertexShader, VertexShaderSource);
    int frag = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);

    GL.GetProgram(_shader, GetProgramParameterName.LinkStatus, out int linked);
    if (linked == 0)
      throw new Exception($"Shader link error: {GL.GetProgramInfoLog(_shader)}");

    _uModel      = GL.GetUniformLocation(_shader, "uModel");
    _uView       = GL.GetUniformLocation(_shader, "uView");
    _uProjection = GL.GetUniformLocation(_shader, "uProjection");

    _camera = new Camera(new Vector3(8, 65, -10));
    CursorState = CursorState.Grabbed;

    GL.Enable(EnableCap.DepthTest);
    GL.Enable(EnableCap.CullFace);
    GL.CullFace(TriangleFace.Back);
    GL.FrontFace(FrontFaceDirection.Ccw);

    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    var generator = new FlatWorldGenerator();
    _chunkMeshes = new List<(ChunkMesh mesh, Vector3 offset)>();

    for (int cx = -2; cx <= 2; cx++)
    for (int cz = -2; cz <= 2; cz++)
    {
      var chunk = new Chunk();
      generator.Generate(chunk);
      var mesh = new ChunkMesh();
      mesh.Build(chunk);
      _chunkMeshes.Add((mesh, new Vector3(cx * Chunk.Width, 0, cz * Chunk.Depth)));
    }

  }

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    var model      = Matrix4.Identity;
    var view = _camera.GetViewMatrix();
    var projection = Matrix4.CreatePerspectiveFieldOfView(
      MathHelper.DegreesToRadians(60f), Size.X / (float)Size.Y, 0.1f, 500f);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(_uModel,      false, ref model);
    GL.UniformMatrix4(_uView,       false, ref view);
    GL.UniformMatrix4(_uProjection, false, ref projection);

    foreach (var (mesh, offset) in _chunkMeshes)
    {
      var chunkModel = Matrix4.CreateTranslation(offset);
      GL.UniformMatrix4(_uModel, false, ref chunkModel);
      mesh.Draw(_textureManager, GL.GetUniformLocation(_shader, "uTexture"));
    }

    SwapBuffers();
  }

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    _camera.ProcessKeyboard(KeyboardState, (float)args.Time);
    _time += (float)args.Time;
  }

  protected override void OnUnload()
  {
    base.OnUnload();
    foreach (var (mesh, _) in _chunkMeshes)
    {
      mesh.Dispose();
    }
    _textureManager.Dispose();
    GL.DeleteProgram(_shader);
  }

  protected override void OnResize(ResizeEventArgs e)
  {
    base.OnResize(e);
    GL.Viewport(0, 0, e.Width, e.Height);
  }

  protected override void OnKeyDown(KeyboardKeyEventArgs e)
  {
    base.OnKeyDown(e);
    if (e.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
      CursorState = CursorState == CursorState.Grabbed 
        ? CursorState.Normal 
        : CursorState.Grabbed;
  }

  protected override void OnMouseMove(MouseMoveEventArgs e)
  {
    base.OnMouseMove(e);
    if (_firstMouse)
    {
      _lastMousePos = new Vector2(e.X, e.Y);
      _firstMouse = false;
      return;
    }
    _camera.ProcessMouse(e.DeltaX, e.DeltaY);
  }

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
}