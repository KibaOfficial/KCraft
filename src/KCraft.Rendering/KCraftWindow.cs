// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using KCraft.Assets;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  private int _vao, _vbo, _ebo, _shader;
  private float _time;
  private int _uModel, _uView, _uProjection;

  private Texture2D _texture = null!;

  private static readonly float[] Vertices =
  {
    // X      Y      Z      U     V
    // Back
    -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
    0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
    0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
    -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
    // Front
    -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
    0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
    0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
    -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
    // Left
    -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
    -0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
    -0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
    -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
    // Right
    0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
    0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
    0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
    0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
    // Top
    -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
    0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
    0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
    -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
    // Bottom
    -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
    0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
    0.5f, -0.5f,  0.5f,  1.0f, 1.0f,
    -0.5f, -0.5f,  0.5f,  0.0f, 1.0f,
  };

  private static readonly uint[] Indices =
  {
    0,  1,  2,   2,  3,  0,  // Back
    4,  5,  6,   6,  7,  4,  // Front
    8,  9, 10,  10, 11,  8,  // Left
    12, 13, 14,  14, 15, 12,  // Right
    16, 17, 18,  18, 19, 16,  // Top
    20, 21, 22,  22, 23, 20,  // Bottom
  };

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
    GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f); // Set clear color to a dark blueish shade
    _texture = new Texture2D("assets/dev/grass_block_side.png");

    // Compile Shaders
    int vert = CompileShader(ShaderType.VertexShader, VertexShaderSource);
    int frag = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);

    // LinkStatus prüfen
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

    // VAO + VBO
    _vao = GL.GenVertexArray();
    _vbo = GL.GenBuffer();
    _ebo = GL.GenBuffer();

    GL.BindVertexArray(_vao);

    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw);

    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
    GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);


    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
    GL.EnableVertexAttribArray(1);
    GL.BindVertexArray(0);
  }

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    var model      = Matrix4.CreateRotationY(_time) * Matrix4.CreateRotationX(_time * 0.5f);
    var view       = Matrix4.LookAt(new Vector3(0, 1, 3), Vector3.Zero, Vector3.UnitY);
    var projection = Matrix4.CreatePerspectiveFieldOfView(
      MathHelper.DegreesToRadians(60f), Size.X / (float)Size.Y, 0.1f, 100f);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(_uModel,      false, ref model);
    GL.UniformMatrix4(_uView,       false, ref view);
    GL.UniformMatrix4(_uProjection, false, ref projection);

    GL.BindVertexArray(_vao);

    _texture.Bind();
    GL.Uniform1(GL.GetUniformLocation(_shader, "uTexture"), 0);
    
    GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);

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
    _texture.Dispose();
    GL.DeleteVertexArray(_vao);
    GL.DeleteBuffer(_vbo);
    GL.DeleteBuffer(_ebo);
    GL.DeleteProgram(_shader);
  }
  protected override void OnResize(ResizeEventArgs e)
  {
    base.OnResize(e);
    GL.Viewport(0, 0, e.Width, e.Height); // Adjust the viewport to the new window size
  }

  protected override void OnKeyDown(KeyboardKeyEventArgs e)
  {
    base.OnKeyDown(e);
    if (e.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
    {
      Close(); // Close the window when the Escape key is pressed
    }
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