// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class BlockHighlightRenderer : IDisposable
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
        void main()
        {
          FragColor = vec4(0.0, 0.0, 0.0, 0.6);
        }
        """;

  public BlockHighlightRenderer()
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

  public void Draw(Vector3i blockPos, Matrix4 view, Matrix4 projection)
  {
    float x = blockPos.X, y = blockPos.Y, z = blockPos.Z;
    const float e = 0.002f; // epsilon gegen Z-Fighting

    // 12 Kanten der Box als Linien
    var lines = new float[]
    {
            // Bottom
            x-e,y-e,z-e,  x+1+e,y-e,z-e,
            x+1+e,y-e,z-e,  x+1+e,y-e,z+1+e,
            x+1+e,y-e,z+1+e,  x-e,y-e,z+1+e,
            x-e,y-e,z+1+e,  x-e,y-e,z-e,
            // Top
            x-e,y+1+e,z-e,  x+1+e,y+1+e,z-e,
            x+1+e,y+1+e,z-e,  x+1+e,y+1+e,z+1+e,
            x+1+e,y+1+e,z+1+e,  x-e,y+1+e,z+1+e,
            x-e,y+1+e,z+1+e,  x-e,y+1+e,z-e,
            // Verticals
            x-e,y-e,z-e,  x-e,y+1+e,z-e,
            x+1+e,y-e,z-e,  x+1+e,y+1+e,z-e,
            x+1+e,y-e,z+1+e,  x+1+e,y+1+e,z+1+e,
            x-e,y-e,z+1+e,  x-e,y+1+e,z+1+e,
    };

    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, lines.Length * sizeof(float), lines, BufferUsageHint.DynamicDraw);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uView"), false, ref view);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uProjection"), false, ref projection);

    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    GL.LineWidth(2.0f);

    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.Lines, 0, lines.Length / 3);

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