// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class BlockIconRenderer : IDisposable
{
  private int _vao, _vbo, _shader;

  // Face Helligkeit wie MC
  private const float TopBrightness = 1.000f;
  private const float LeftBrightness = 0.800f;
  private const float RightBrightness = 0.608f;
  private static readonly Vector3 LeavesTint = new(0.38f, 0.62f, 0.25f);

  private const string VertSrc = """
        #version 410 core
        layout(location = 0) in vec2 aPos;
        layout(location = 1) in vec2 aUV;
        out vec2 vUV;
        uniform vec2 uScreen;
        void main()
        {
          vec2 ndc = (aPos / uScreen) * 2.0 - 1.0;
          gl_Position = vec4(ndc.x, -ndc.y, 0.0, 1.0);
          vUV = aUV;
        }
        """;

  private const string FragSrc = """
        #version 410 core
        in vec2 vUV;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        uniform vec3 uTint;
        uniform float uBrightness;
        void main()
        {
          vec4 color = texture(uTexture, vUV);
          if (color.a < 0.1) discard;
          FragColor = vec4(color.rgb * uTint * uBrightness, color.a);
        }
        """;

  public BlockIconRenderer()
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
    // 4 floats: x y u v
    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
    GL.EnableVertexAttribArray(1);
    GL.BindVertexArray(0);
  }

  // Zeichnet einen Block-Icon in den angegebenen Screen-Bereich
  public void Draw(Block block, float x, float y, float size,
    Vector2 screen, TextureManager textures)
  {
    if (block == Block.Air) return;

    var def = BlockRegistry.Get(block);

    GL.Enable(EnableCap.ScissorTest);
    GL.Scissor((int)x, (int)(screen.Y - y - size), (int)size, (int)size);

    float cx = x + size / 2f;
    float cy = y + size / 2f - 7f;
    float halfW = size * 0.34f;
    float topH = size * 0.18f;
    float sideH = size * 0.58f;

    GL.UseProgram(_shader);
    GL.Uniform2(GL.GetUniformLocation(_shader, "uScreen"), screen.X, screen.Y);
    GL.Disable(EnableCap.DepthTest);
    GL.Disable(EnableCap.CullFace);
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    var white = Vector3.One;
    var tint = block == Block.Grass    ? GrassTopTint
         : block == Block.OakLeaves ? LeavesTint
         : white;

    DrawFace(LeftFace(cx, cy, halfW, topH, sideH), def.TextureSide, LeftBrightness, tint, textures);
    DrawFace(RightFace(cx, cy, halfW, topH, sideH), def.TextureSide, RightBrightness, tint, textures);
    DrawFace(TopFace(cx, cy, halfW, topH), def.TextureTop, TopBrightness, tint, textures);

    GL.Disable(EnableCap.Blend);
    GL.Enable(EnableCap.CullFace);
    GL.Enable(EnableCap.DepthTest);
    GL.Disable(EnableCap.ScissorTest);
  }

  private void DrawFace(float[] verts, string texName, float brightness, Vector3 tint,
      TextureManager textures)
  {
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
        verts.Length * sizeof(float), verts, BufferUsageHint.DynamicDraw);

    var tex = textures.Get(texName);
    tex.Bind();

    int uTexture = GL.GetUniformLocation(_shader, "uTexture");
    int uTint = GL.GetUniformLocation(_shader, "uTint");
    int uBrightness = GL.GetUniformLocation(_shader, "uBrightness");

    GL.Uniform1(uTexture, 0);
    GL.Uniform3(uTint, tint.X, tint.Y, tint.Z);
    GL.Uniform1(uBrightness, brightness);

    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
  }

  // ── Face Vertices (x y u v) ───────────────────────────────────────
  private static readonly Vector3 GrassTopTint = new(0.45f, 0.80f, 0.28f);

  private static float[] TopFace(float cx, float cy, float halfW, float topH) => new float[]
  {
        cx,         cy - topH, 0.5f, 0f,
        cx + halfW, cy,        1f,   0.5f,
        cx,         cy + topH, 0.5f, 1f,
        cx,         cy - topH, 0.5f, 0f,
        cx,         cy + topH, 0.5f, 1f,
        cx - halfW, cy,        0f,   0.5f,
  };

  private static float[] LeftFace(float cx, float cy, float halfW, float topH, float sideH) => new float[]
  {
        cx - halfW, cy,          0f, 1f,
        cx,         cy + topH,   1f, 1f,
        cx,         cy + sideH,  1f, 0f,
        cx - halfW, cy,          0f, 1f,
        cx,         cy + sideH,  1f, 0f,
        cx - halfW, cy + sideH - topH, 0f, 0f,
  };

  private static float[] RightFace(float cx, float cy, float halfW, float topH, float sideH) => new float[]
  {
        cx + halfW, cy,          1f, 1f,
        cx,         cy + topH,   0f, 1f,
        cx,         cy + sideH,  0f, 0f,
        cx + halfW, cy,          1f, 1f,
        cx,         cy + sideH,  0f, 0f,
        cx + halfW, cy + sideH - topH, 1f, 0f,
  };

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
