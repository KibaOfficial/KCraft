// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class BlockIconRenderer : IDisposable
{
  private int _vao, _vbo, _ebo, _shader;

  private const string VertSrc = """
        #version 410 core
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        layout(location = 2) in float aBrightness;
        out vec2 vTexCoord;
        out float vBrightness;
        uniform mat4 uMVP;
        void main()
        {
            vTexCoord   = aTexCoord;
            vBrightness = aBrightness;
            gl_Position = uMVP * vec4(aPosition, 1.0);
        }
        """;

  private const string FragSrc = """
        #version 410 core
        in vec2 vTexCoord;
        in float vBrightness;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        uniform vec3 uTint;
        void main()
        {
            vec4 color = texture(uTexture, vTexCoord);
            if (color.a < 0.1) discard;
            FragColor = vec4(color.rgb * uTint * vBrightness, color.a);
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
    _ebo = GL.GenBuffer();
    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
    GL.EnableVertexAttribArray(1);
    GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 6 * sizeof(float), 5 * sizeof(float));
    GL.EnableVertexAttribArray(2);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
    GL.BindVertexArray(0);
  }

  public void Draw(Block block, float x, float y, float size,
      Vector2 screen, TextureManager textures)
  {
    if (block == Block.Air) return;

    var def = BlockRegistry.Get(block);
    string texTop = def.UsesCTM ? def.CTMTexture.Replace("_ctm", "") : def.TextureTop;
    string texSide = def.UsesCTM ? def.CTMTexture.Replace("_ctm", "") : def.TextureSide;
    string texBottom = def.UsesCTM ? def.CTMTexture.Replace("_ctm", "") : def.TextureBottom;

    var tint = block == Block.OakLeaves ? new Vector3(0.38f, 0.62f, 0.25f)
             : block == Block.Grass ? Vector3.One
             : block == Block.Water ? new Vector3(0x3F / 255f, 0x76 / 255f, 0xE4 / 255f)
             : Vector3.One;

    var topTint = block == Block.Grass ? new Vector3(0.45f, 0.80f, 0.28f)
                : block == Block.OakLeaves ? new Vector3(0.38f, 0.62f, 0.25f)
                : block == Block.Water ? new Vector3(0x3F / 255f, 0x76 / 255f, 0xE4 / 255f)
                : Vector3.One;

    // Geometrie aufbauen
    var verts = new List<float>();
    var indices = new List<uint>();

    if (def.IsStairs)
      BuildStairMesh(verts, indices, texTop, texSide, texBottom);
    else if (def.IsSlope)
      BuildSlopeMesh(verts, indices, texTop, texSide, texBottom);
    else
      BuildCubeMesh(verts, indices, texTop, texSide, texBottom);

    // Upload
    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
        verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.DynamicDraw);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
    GL.BufferData(BufferTarget.ElementArrayBuffer,
        indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.DynamicDraw);

    // MVP — isometrische Icon-Kamera
    // Block zentriert um (0.5, 0.5, 0.5) → verschieben auf Ursprung
    var model = Matrix4.CreateTranslation(-0.5f, -0.5f, -0.5f)
          * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45f))
          * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(30f));
    var proj = Matrix4.CreateOrthographic(1.8f, 1.8f, 0.1f, 10f);
    var view = Matrix4.LookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
    var mvp = model * view * proj;

    // Viewport auf Slot-Bereich setzen
    int ix = (int)x;
    int iy = (int)(screen.Y - y - size);
    int isize = (int)size;

    GL.Viewport(ix, iy, isize, isize);
    GL.Scissor(ix, iy, isize, isize);
    GL.Enable(EnableCap.ScissorTest);

    // wichtig für 3D-Icons
    GL.Clear(ClearBufferMask.DepthBufferBit);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uMVP"), false, ref mvp);

    GL.Enable(EnableCap.DepthTest);
    GL.DepthFunc(DepthFunction.Lequal);
    GL.Disable(EnableCap.CullFace);

    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    // Faces nach Textur gruppiert zeichnen
    DrawMeshWithTextures(verts, indices, texTop, texSide, texBottom, tint, topTint, textures);

    GL.Disable(EnableCap.Blend);
    GL.Enable(EnableCap.DepthTest);
    GL.Enable(EnableCap.CullFace);
    GL.Disable(EnableCap.ScissorTest);

    GL.Viewport(0, 0, (int)screen.X, (int)screen.Y);
    GL.BindVertexArray(0);
  }

  // ── Mesh Builder Helpers ──────────────────────────────────────────────

  private static void AddQuad(List<float> verts, List<uint> indices,
      (float x, float y, float z) v0,
      (float x, float y, float z) v1,
      (float x, float y, float z) v2,
      (float x, float y, float z) v3,
      float u0, float v_0, float u1, float v_1,
      float brightness)
  {
    uint o = (uint)(verts.Count / 6);
    verts.AddRange([v0.x, v0.y, v0.z, u0, v_0, brightness]);
    verts.AddRange([v1.x, v1.y, v1.z, u1, v_0, brightness]);
    verts.AddRange([v2.x, v2.y, v2.z, u1, v_1, brightness]);
    verts.AddRange([v3.x, v3.y, v3.z, u0, v_1, brightness]);
    indices.AddRange([o, o + 1, o + 2, o, o + 2, o + 3]);
  }

  private static void AddBox(List<float> verts, List<uint> indices,
      float x0, float y0, float z0,
      float x1, float y1, float z1)
  {
    // Top (brightness 1.0)
    AddQuad(verts, indices,
        (x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1),
        0, 0, 1, 1, 1.0f);
    // Bottom (0.5)
    AddQuad(verts, indices,
        (x0, y0, z1), (x1, y0, z1), (x1, y0, z0), (x0, y0, z0),
        0, 0, 1, 1, 0.5f);
    // North (0.8)
    AddQuad(verts, indices,
        (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
        0, 0, 1, 1, 0.8f);
    // South (0.8)
    AddQuad(verts, indices,
        (x1, y0, z1), (x0, y0, z1), (x0, y1, z1), (x1, y1, z1),
        0, 0, 1, 1, 0.8f);
    // West (0.6)
    AddQuad(verts, indices,
        (x0, y0, z1), (x0, y0, z0), (x0, y1, z0), (x0, y1, z1),
        0, 0, 1, 1, 0.6f);
    // East (0.6)
    AddQuad(verts, indices,
        (x1, y0, z0), (x1, y0, z1), (x1, y1, z1), (x1, y1, z0),
        0, 0, 1, 1, 0.6f);
  }

  private static void BuildCubeMesh(List<float> verts, List<uint> indices,
      string texTop, string texSide, string texBottom)
  {
    AddBox(verts, indices, 0, 0, 0, 1, 1, 1);
  }

  private static void BuildStairMesh(List<float> verts, List<uint> indices,
      string texTop, string texSide, string texBottom)
  {
    // Untere Hälfte — volle Breite, volle Tiefe
    AddBox(verts, indices, 0, 0, 0, 1, 0.5f, 1);
    // Obere Hälfte — North (z=0..0.5) = hinten aus Kamera-Perspektive
    AddBox(verts, indices, 0, 0.5f, 0, 1, 1, 0.5f);
  }

  private static void BuildSlopeMesh(List<float> verts, List<uint> indices,
      string texTop, string texSide, string texBottom)
  {
    uint o;

    // Bottom
    AddQuad(verts, indices,
        (0, 0, 1), (1, 0, 1), (1, 0, 0), (0, 0, 0),
        0, 0, 1, 1, 0.5f);

    // Back (North, volle Höhe bei z=0)
    AddQuad(verts, indices,
        (0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0),
        0, 0, 1, 1, 0.8f);

    // Left triangle (West) — als dünnes Quad
    o = (uint)(verts.Count / 6);
    verts.AddRange([0f, 0f, 0f, 0f, 1f, 0.6f]);
    verts.AddRange([0f, 1f, 0f, 0f, 0f, 0.6f]);
    verts.AddRange([0f, 0f, 1f, 1f, 1f, 0.6f]);
    verts.AddRange([0f, 0f, 1f, 1f, 1f, 0.6f]); // degenerate
    indices.AddRange([o, o + 1, o + 2, o, o + 2, o + 3]);

    // Right triangle (East)
    o = (uint)(verts.Count / 6);
    verts.AddRange([1f, 0f, 0f, 0f, 1f, 0.6f]);
    verts.AddRange([1f, 0f, 1f, 1f, 1f, 0.6f]);
    verts.AddRange([1f, 1f, 0f, 0f, 0f, 0.6f]);
    verts.AddRange([1f, 1f, 0f, 0f, 0f, 0.6f]); // degenerate
    indices.AddRange([o, o + 1, o + 2, o, o + 2, o + 3]);

    // Schräge Top-Face
    o = (uint)(verts.Count / 6);
    verts.AddRange([0f, 1f, 0f, 0f, 0f, 1.0f]);
    verts.AddRange([1f, 1f, 0f, 1f, 0f, 1.0f]);
    verts.AddRange([1f, 0f, 1f, 1f, 1f, 1.0f]);
    verts.AddRange([0f, 0f, 1f, 0f, 1f, 1.0f]);
    indices.AddRange([o, o + 1, o + 2, o, o + 2, o + 3]);
  }

  // ── Draw mit Textur-Gruppen ───────────────────────────────────────────

  private void DrawMeshWithTextures(
      List<float> verts, List<uint> indices,
      string texTop, string texSide, string texBottom,
      Vector3 tint, Vector3 topTint,
      TextureManager textures)
  {
    // Alle Faces auf einmal mit Side-Textur zeichnen
    // (vereinfacht: alle Faces mit gleicher Textur + Tint)
    // Für korrekte Top/Side/Bottom Textur müssten wir separate Draw Calls machen.
    // Hier nutzen wir Side für alles außer Top (face index 0 = Top).

    GL.BindVertexArray(_vao);

    // Pass 1: Side + Bottom faces (indices 6..end)
    if (indices.Count > 6)
    {
      textures.Get(texSide).Bind();
      GL.Uniform3(GL.GetUniformLocation(_shader, "uTint"), tint.X, tint.Y, tint.Z);
      GL.Uniform1(GL.GetUniformLocation(_shader, "uTexture"), 0);
      GL.DrawElements(PrimitiveType.Triangles, indices.Count - 6,
          DrawElementsType.UnsignedInt, 6 * sizeof(uint));
    }

    // Pass 2: Top face (indices 0..5)
    textures.Get(texTop).Bind();
    GL.Uniform3(GL.GetUniformLocation(_shader, "uTint"), topTint.X, topTint.Y, topTint.Z);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uTexture"), 0);
    GL.DrawElements(PrimitiveType.Triangles, 6,
        DrawElementsType.UnsignedInt, 0);
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