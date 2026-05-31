// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class TextRenderer : IDisposable
{
  private readonly Texture2D _font;
  private int _vao, _vbo;
  private int _shader;

  // ascii.png ist 128x128, 16x16 Zeichen, jedes 8x8 Pixel
  private const float CharW = 1.0f / 16.0f;
  private const float CharH = 1.0f / 16.0f;
  private const float GlyphPixels = 8.0f;
  private const float CharAdvancePixels = 7.0f;
  private const float SpaceAdvancePixels = 4.0f;

  private const string VertSrc = """
    #version 410 core
    layout(location = 0) in vec2 aPos;
    layout(location = 1) in vec2 aUV;
    layout(location = 2) in vec4 aColor;
    out vec2 vUV;
    out vec4 vColor;
    uniform vec2 uScreen;
    void main()
    {
      vec2 ndc = (aPos / uScreen) * 2.0 - 1.0;
      gl_Position = vec4(ndc.x, -ndc.y, 0.0, 1.0);
      vUV    = aUV;
      vColor = aColor;
    }
    """;

  private const string FragSrc = """
    #version 410 core
    in vec2 vUV;
    in vec4 vColor;
    out vec4 FragColor;
    uniform sampler2D uFont;
    uniform bool uUseFont;
    void main()
    {
      if (!uUseFont)
      {
        FragColor = vColor;
        return;
      }

      vec4 tex = texture(uFont, vUV);
      if (tex.a < 0.1) discard;
      FragColor = vec4(vColor.rgb, vColor.a * tex.a);
    }
    """;

  public TextRenderer(string fontPath)
  {
    _font = new Texture2D(fontPath);

    int vert = CompileShader(ShaderType.VertexShader,   VertSrc);
    int frag = CompileShader(ShaderType.FragmentShader, FragSrc);
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
    // 8 floats per vertex: x y u v r g b a
    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 2 * sizeof(float));
    GL.EnableVertexAttribArray(1);
    GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 8 * sizeof(float), 4 * sizeof(float));
    GL.EnableVertexAttribArray(2);
    GL.BindVertexArray(0);
  }

  public void DrawText(string text, float x, float y, Vector2 screen,
    float scale = 2f, Vector4? color = null, bool fixedAdvance = false)
  {
    var col = color ?? new Vector4(1, 1, 1, 1);
    var verts = new List<float>();
    float cx = x;

    foreach (char c in text)
    {
      int idx = c;
      if (idx >= 256)
        idx = '?';

      if (c == ' ')
      {
        cx += SpaceAdvancePixels * scale;
        continue;
      }

      float u0 = (idx % 16) * CharW;
      float v0 = 1.0f - (idx / 16) * CharH - CharH; 
      float u1 = u0 + CharW;
      float v1 = v0 + CharH;
      float w = GlyphPixels * scale;
      float h = GlyphPixels * scale;

      // 2 triangles per char
      AddVert(verts, cx,     y,     u0, v1, col);
      AddVert(verts, cx+w,   y,     u1, v1, col);
      AddVert(verts, cx+w,   y+h,   u1, v0, col);
      AddVert(verts, cx,     y,     u0, v1, col);
      AddVert(verts, cx+w,   y+h,   u1, v0, col);
      AddVert(verts, cx,     y+h,   u0, v0, col);

      cx += GetAdvance(c) * scale;
    }

    var data = verts.ToArray();
    GL.UseProgram(_shader);
    GL.Uniform2(GL.GetUniformLocation(_shader, "uScreen"), screen.X, screen.Y);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uFont"), 0);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uUseFont"), 1);
    _font.Bind();

    DrawVertices(data);
  }

  public void DrawRect(float x, float y, float width, float height,
    Vector2 screen, Vector4 color)
  {
    var verts = new List<float>();

    AddVert(verts, x,         y,          0, 0, color);
    AddVert(verts, x + width, y,          0, 0, color);
    AddVert(verts, x + width, y + height, 0, 0, color);
    AddVert(verts, x,         y,          0, 0, color);
    AddVert(verts, x + width, y + height, 0, 0, color);
    AddVert(verts, x,         y + height, 0, 0, color);

    GL.UseProgram(_shader);
    GL.Uniform2(GL.GetUniformLocation(_shader, "uScreen"), screen.X, screen.Y);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uUseFont"), 0);

    DrawVertices(verts.ToArray());
  }

  private void DrawVertices(float[] data)
  {
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);

    GL.Disable(EnableCap.DepthTest);
    GL.Disable(EnableCap.CullFace);
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length / 8);

    GL.Disable(EnableCap.Blend);
    GL.Enable(EnableCap.CullFace);
    GL.Enable(EnableCap.DepthTest);
  }

  public float MeasureTextWidth(string text, float scale = 2f, bool fixedAdvance = false)
  {
    float width = 0;
    foreach (char c in text)
      width += GetAdvance(c) * scale;

    return width;
  }

  private static float GetAdvance(char c)
    => c switch
    {
      ' ' => SpaceAdvancePixels,
      'i' or 'l' or '!' or '.' or ',' or ':' or ';' or '\'' => 4.0f,
      'I' or '[' or ']' or '(' or ')' or '|' => 5.0f,
      'f' or 'j' or 'r' or 't' => 6.0f,
      _ => CharAdvancePixels,
    };

  private static void AddVert(List<float> v, float x, float y,
    float u, float vv, Vector4 c)
    => v.AddRange([x, y, u, vv, c.X, c.Y, c.Z, c.W]);

  private static int CompileShader(ShaderType type, string src)
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
    _font.Dispose();
    GL.DeleteVertexArray(_vao);
    GL.DeleteBuffer(_vbo);
    GL.DeleteProgram(_shader);
  }
}
