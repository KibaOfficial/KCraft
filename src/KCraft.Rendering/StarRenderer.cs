// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class StarRenderer : IDisposable
{
  private int _vao, _vbo, _ebo, _shader;
  private int _indexCount;

  private const string VertSrc = """
        #version 410 core
        layout(location = 0) in vec3 aPos;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform float uBrightness;
        out float vBrightness;
        void main()
        {
            vBrightness = uBrightness;
            // Translation aus View entfernen (nur Rotation)
            mat4 viewNoTrans = uView;
            viewNoTrans[3] = vec4(0.0, 0.0, 0.0, 1.0);
            gl_Position = uProjection * viewNoTrans * vec4(aPos, 1.0);
        }
        """;

  private const string FragSrc = """
        #version 410 core
        in float vBrightness;
        out vec4 FragColor;
        void main()
        {
            FragColor = vec4(1.0, 1.0, 1.0, vBrightness);
        }
        """;

  public StarRenderer()
  {
    // Shader
    int vert = Compile(ShaderType.VertexShader, VertSrc);
    int frag = Compile(ShaderType.FragmentShader, FragSrc);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);
    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    // Sterne generieren — fixer Seed wie MC (10842)
    GenerateStars(10842, 1500);
  }

  private void GenerateStars(int seed, int attempts)
  {
    var rng = new Random(seed);
    var verts = new List<float>();
    var indices = new List<uint>();
    uint offset = 0;

    const float radius = 200f;
    const float starSize = 0.3f;
    int generated = 0;

    for (int i = 0; i < attempts; i++)
    {
      // Zufälligen Punkt auf Einheitssphäre
      float u = (float)rng.NextDouble() * 2f - 1f;
      float phi = (float)(rng.NextDouble() * Math.PI * 2);
      float sinTheta = MathF.Sqrt(1f - u * u);

      var center = new Vector3(
          sinTheta * MathF.Cos(phi),
          sinTheta * MathF.Sin(phi),
          u
      ) * radius;

      // Nur obere Hemisphäre
      if (center.Y < 5f) continue;

      // Zufällige Helligkeit
      float brightness = 0.5f + (float)rng.NextDouble() * 0.5f;
      float size = starSize * (0.5f + (float)rng.NextDouble() * 0.8f);

      // Billboard — 2 senkrechte Vektoren zur Zentrums-Richtung
      var normal = Vector3.Normalize(center);
      var right = Vector3.Normalize(Vector3.Cross(normal,
          Math.Abs(normal.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX));
      var up = Vector3.Cross(right, normal);

      var v0 = center + (-right - up) * size;
      var v1 = center + (right - up) * size;
      var v2 = center + (right + up) * size;
      var v3 = center + (-right + up) * size;

      verts.AddRange([v0.X, v0.Y, v0.Z]);
      verts.AddRange([v1.X, v1.Y, v1.Z]);
      verts.AddRange([v2.X, v2.Y, v2.Z]);
      verts.AddRange([v3.X, v3.Y, v3.Z]);

      indices.AddRange([offset, offset + 1, offset + 2, offset, offset + 2, offset + 3]);
      offset += 4;
      generated++;
    }

    _indexCount = indices.Count;

    // Upload
    _vao = GL.GenVertexArray();
    _vbo = GL.GenBuffer();
    _ebo = GL.GenBuffer();

    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
        verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.StaticDraw);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
    GL.BufferData(BufferTarget.ElementArrayBuffer,
        indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.BindVertexArray(0);

    Console.WriteLine($"[StarRenderer] Generated {generated} stars");
  }

  public void Draw(Matrix4 view, Matrix4 projection, float nightFactor, float time)
  {
    if (nightFactor < 0.01f) return;

    // Twinkle — sehr langsam
    float brightness = nightFactor * (0.8f + 0.2f * MathF.Sin(time * 0.0003f));

    GL.UseProgram(_shader);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uView"), false, ref view);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uProjection"), false, ref projection);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uBrightness"), brightness);

    GL.Disable(EnableCap.DepthTest);
    GL.Disable(EnableCap.CullFace);
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One); // Additive Blending

    GL.BindVertexArray(_vao);
    GL.DrawElements(PrimitiveType.Triangles, _indexCount,
        DrawElementsType.UnsignedInt, 0);

    GL.Disable(EnableCap.Blend);
    GL.Enable(EnableCap.CullFace);
    GL.Enable(EnableCap.DepthTest);
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
    GL.DeleteBuffer(_ebo);
    GL.DeleteProgram(_shader);
  }
}