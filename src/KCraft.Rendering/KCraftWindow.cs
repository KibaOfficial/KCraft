// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  private int _vao, _vbo, _ebo, _shader;
  private float _time;
  private int _uModel, _uView, _uProjection;

  private static readonly float[] Vertices =
  {
    // X      Y      Z
    -0.5f, -0.5f, -0.5f,  // 0: Back-bottom-left
    0.5f, -0.5f, -0.5f,  // 1: Back-bottom-right
    0.5f,  0.5f, -0.5f,  // 2: Back-top-right
    -0.5f,  0.5f, -0.5f,  // 3: Back-top-left
    -0.5f, -0.5f,  0.5f,  // 4: Front-bottom-left
    0.5f, -0.5f,  0.5f,  // 5: Front-bottom-right
    0.5f,  0.5f,  0.5f,  // 6: Front-top-right
    -0.5f,  0.5f,  0.5f,  // 7: Front-top-left
  };

  private static readonly uint[] Indices =
  {
    0, 1, 2,  2, 3, 0,  // Back
    4, 5, 6,  6, 7, 4,  // Front
    0, 4, 7,  7, 3, 0,  // Left
    1, 5, 6,  6, 2, 1,  // Right
    3, 7, 6,  6, 2, 3,  // Top
    0, 4, 5,  5, 1, 0,  // Bottom
  };

  private const string VertexShaderSource = """
  #version 410 core
  layout(location = 0) in vec3 aPosition;
  uniform mat4 uModel;
  uniform mat4 uView;
  uniform mat4 uProjection;
  void main()
  {
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
  }
  """;

  private const string FragmentShaderSource = """
  #version 410 core
  out vec4 FragColor;
  void main()
  {
    FragColor = vec4(0.4, 0.8, 0.4, 1.0); // Set the triangle color to a light green
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


    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);

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