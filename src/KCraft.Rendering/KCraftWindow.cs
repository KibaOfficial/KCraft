// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using KCraft.Assets;
using KCraft.Blocks;
using KCraft.World;
using KCraft.World.Generation;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  private int _shader;
  private float _time;
  private int _uModel, _uView, _uProjection;

  private TextureManager _textureManager = null!;
  private ChunkMesh _chunkMesh = null!;

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
    GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);
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

    GL.Enable(EnableCap.DepthTest);
    GL.Enable(EnableCap.CullFace);
    GL.CullFace(TriangleFace.Back);
    GL.FrontFace(FrontFaceDirection.Ccw);

    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    // Chunk generieren und Mesh bauen
    var chunk = new Chunk();
    var gen   = new FlatWorldGenerator();
    gen.Generate(chunk);

    _chunkMesh = new ChunkMesh();
    _chunkMesh.Build(chunk);
  }

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    var model      = Matrix4.Identity;
    var view = Matrix4.LookAt(
      new Vector3(8, 65, -10),  // Position: vor dem Chunk
      new Vector3(8, 62, 8),    // Ziel: Mitte des Chunks
      Vector3.UnitY
    );
    var projection = Matrix4.CreatePerspectiveFieldOfView(
      MathHelper.DegreesToRadians(60f), Size.X / (float)Size.Y, 0.1f, 500f);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(_uModel,      false, ref model);
    GL.UniformMatrix4(_uView,       false, ref view);
    GL.UniformMatrix4(_uProjection, false, ref projection);

    _chunkMesh.Draw(_textureManager, GL.GetUniformLocation(_shader, "uTexture"));

    SwapBuffers();
  }

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    _time += (float)args.Time;
  }

  protected override void OnUnload()
  {
    base.OnUnload();
    _chunkMesh.Dispose();
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
      Close();
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