// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class HitboxRenderer : IDisposable
{
  private int _vao, _vbo, _shader;

  private const string VertSrc = """
        #version 410 core
        layout(location = 0) in vec3 aPos;
        uniform mat4 uView;
        uniform mat4 uProjection;
        void main()
        {
          gl_Position = uProjection * uView * vec4(aPos, 1.0);
        }
        """;

  private const string FragSrc = """
        #version 410 core
        out vec4 FragColor;
        uniform vec4 uColor;
        void main()
        {
          FragColor = uColor;
        }
        """;

  public bool Visible { get; set; } = false;

  public HitboxRenderer()
  {
    int vert = Compile(ShaderType.VertexShader, VertSrc);
    int frag = Compile(ShaderType.FragmentShader, FragSrc);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);
    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    _vao = GL.GenVertexArray();
    _vbo = GL.GenBuffer();
    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.BindVertexArray(0);
  }

  public void Draw(AABB aabb, Matrix4 view, Matrix4 projection, Vector4? color = null)
  {
    if (!Visible) return;

    var col = color ?? new Vector4(1f, 1f, 0f, 0.8f); // Gelb wie MC

    float x0 = aabb.MinX, y0 = aabb.MinY, z0 = aabb.MinZ;
    float x1 = aabb.MaxX, y1 = aabb.MaxY, z1 = aabb.MaxZ;

    var lines = new float[]
    {
            // Bottom
            x0,y0,z0, x1,y0,z0,
            x1,y0,z0, x1,y0,z1,
            x1,y0,z1, x0,y0,z1,
            x0,y0,z1, x0,y0,z0,
            // Top
            x0,y1,z0, x1,y1,z0,
            x1,y1,z0, x1,y1,z1,
            x1,y1,z1, x0,y1,z1,
            x0,y1,z1, x0,y1,z0,
            // Verticals
            x0,y0,z0, x0,y1,z0,
            x1,y0,z0, x1,y1,z0,
            x1,y0,z1, x1,y1,z1,
            x0,y0,z1, x0,y1,z1,
      // Eye level line (rot)
    };

    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, lines.Length * sizeof(float), lines, BufferUsageHint.DynamicDraw);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uView"), false, ref view);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uProjection"), false, ref projection);
    GL.Uniform4(GL.GetUniformLocation(_shader, "uColor"), col);

    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    GL.LineWidth(1.5f);
    GL.Disable(EnableCap.DepthTest);

    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.Lines, 0, lines.Length / 3);

    // Eye-Level Linie in Rot
    var eyeLines = new float[]
    {
            x0, 1.62f + aabb.MinY, z0,  x1, 1.62f + aabb.MinY, z0,
            x1, 1.62f + aabb.MinY, z0,  x1, 1.62f + aabb.MinY, z1,
            x1, 1.62f + aabb.MinY, z1,  x0, 1.62f + aabb.MinY, z1,
            x0, 1.62f + aabb.MinY, z1,  x0, 1.62f + aabb.MinY, z0,
    };
    GL.BufferData(BufferTarget.ArrayBuffer, eyeLines.Length * sizeof(float), eyeLines, BufferUsageHint.DynamicDraw);
    GL.Uniform4(GL.GetUniformLocation(_shader, "uColor"), new Vector4(1f, 0f, 0f, 0.9f));
    GL.DrawArrays(PrimitiveType.Lines, 0, eyeLines.Length / 3);

    GL.Enable(EnableCap.DepthTest);
    GL.Disable(EnableCap.Blend);
  }

  private static int Compile(ShaderType type, string src)
  {
    int s = GL.CreateShader(type);
    GL.ShaderSource(s, src);
    GL.CompileShader(s);
    GL.GetShader(s, ShaderParameter.CompileStatus, out int ok);
    if (ok == 0) throw new Exception(GL.GetShaderInfoLog(s));
    return s;
  }

  public void Dispose()
  {
    GL.DeleteVertexArray(_vao);
    GL.DeleteBuffer(_vbo);
    GL.DeleteProgram(_shader);
  }
}