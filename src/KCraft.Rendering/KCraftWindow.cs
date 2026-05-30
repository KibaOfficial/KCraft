// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class KCraftWindow : GameWindow
{
  private int _vao, _vbo, _shader;

  private static readonly float[] Vertices =
  {
    // X     Y     Z
    -0.5f, -0.5f, 0.0f, // Bottom-left
     0.5f, -0.5f, 0.0f, // Bottom-right
     0.0f,  0.5f, 0.0f  // Top-center
  };

  private const string VertexShaderSource = """
  #version 410 core
  layout(location = 0) in vec3 aPosition;
  void main()
  {
    gl_Position = vec4(aPosition, 1.0);
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

    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    // VAO + VBO
    _vao = GL.GenVertexArray();
    _vbo = GL.GenBuffer();

    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw);

    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);

    GL.BindVertexArray(0);
  }

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    GL.UseProgram(_shader);
    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

    SwapBuffers();
  }

  protected override void OnUnload()
  {
    base.OnUnload();
    GL.DeleteVertexArray(_vao);
    GL.DeleteBuffer(_vbo);
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