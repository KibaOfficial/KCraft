// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class CloudRenderer : IDisposable
{
  private int _vao, _vbo, _ebo, _shader;
  private int _indexCount;

  private const string VertSrc = """
    #version 410 core
    layout(location = 0) in vec3 aPos;
    uniform mat4 uView;
    uniform mat4 uProjection;
    uniform mat4 uModel;
    out float vFogFactor;
    void main()
    {
        vec4 worldPos = uModel * vec4(aPos, 1.0);
        gl_Position = uProjection * uView * worldPos;
        float dist = length(worldPos.xz - vec2(uModel[3][0], uModel[3][2]));
        vFogFactor = clamp(1.0 - (dist - 300.0) / 200.0, 0.0, 1.0);
    }
    """;

  private const string FragSrc = """
        #version 410 core
        in float vFogFactor;
        out vec4 FragColor;
        uniform vec4 uCloudColor;
        void main()
        {
            FragColor = vec4(uCloudColor.rgb, uCloudColor.a * vFogFactor);
        }
        """;

  private float _offset = 0f;
  private const float CloudY = 192f;
  private const float CloudSpeed = 0.15f; // Blöcke pro Sekunde

  public CloudRenderer()
  {
    int vert = Compile(ShaderType.VertexShader, VertSrc);
    int frag = Compile(ShaderType.FragmentShader, FragSrc);
    _shader = GL.CreateProgram();
    GL.AttachShader(_shader, vert);
    GL.AttachShader(_shader, frag);
    GL.LinkProgram(_shader);
    GL.DeleteShader(vert);
    GL.DeleteShader(frag);

    GenerateClouds(12345);
  }

  private void GenerateClouds(int seed)
  {
    var verts = new List<float>();
    var indices = new List<uint>();
    uint offset = 0;

    const int gridSize = 60;
    const float cellSize = 4f;
    const float cloudH = 2f;
    const float cloudY = 192f;

    var noise = new FastNoiseLite(seed);
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFrequency(0.02f);

    // 2D Boolean Map aufbauen
    bool[,] map = new bool[gridSize, gridSize];
    for (int cx = 0; cx < gridSize; cx++)
      for (int cz = 0; cz < gridSize; cz++)
      {
        float wx = (cx - gridSize / 2) * cellSize;
        float wz = (cz - gridSize / 2) * cellSize;
        map[cx, cz] = noise.GetNoise(wx, wz) > 0.15f;
      }

    // Greedy Mesh — Reihen in X zusammenfassen
    bool[,] used = new bool[gridSize, gridSize];
    for (int cz = 0; cz < gridSize; cz++)
    {
      for (int cx = 0; cx < gridSize; cx++)
      {
        if (!map[cx, cz] || used[cx, cz]) continue;

        // Wie weit geht diese Reihe in X?
        int endX = cx;
        while (endX + 1 < gridSize && map[endX + 1, cz] && !used[endX + 1, cz])
          endX++;

        // Wie weit können wir in Z erweitern?
        int endZ = cz;
        while (endZ + 1 < gridSize)
        {
          bool rowOk = true;
          for (int x = cx; x <= endX; x++)
            if (!map[x, endZ + 1] || used[x, endZ + 1]) { rowOk = false; break; }
          if (!rowOk) break;
          endZ++;
        }

        // Alle verwendeten Zellen markieren
        for (int x = cx; x <= endX; x++)
          for (int z = cz; z <= endZ; z++)
            used[x, z] = true;

        // Ein großes Quad für diesen Block
        float x0 = (cx - gridSize / 2) * cellSize;
        float z0 = (cz - gridSize / 2) * cellSize;
        float x1 = (endX - gridSize / 2 + 1) * cellSize;
        float z1 = (endZ - gridSize / 2 + 1) * cellSize;

        // Top
        AddQuad(verts, indices, ref offset,
            (x0, cloudY + cloudH, z0), (x1, cloudY + cloudH, z0),
            (x1, cloudY + cloudH, z1), (x0, cloudY + cloudH, z1));
        // Bottom
        AddQuad(verts, indices, ref offset,
            (x0, cloudY, z1), (x1, cloudY, z1),
            (x1, cloudY, z0), (x0, cloudY, z0));
      }
    }

    _indexCount = indices.Count;

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

    Console.WriteLine($"[CloudRenderer] Generated clouds, {_indexCount / 3} triangles");
  }

  private static void AddQuad(List<float> verts, List<uint> indices, ref uint offset,
      (float x, float y, float z) v0,
      (float x, float y, float z) v1,
      (float x, float y, float z) v2,
      (float x, float y, float z) v3)
  {
    verts.AddRange([v0.x, v0.y, v0.z]);
    verts.AddRange([v1.x, v1.y, v1.z]);
    verts.AddRange([v2.x, v2.y, v2.z]);
    verts.AddRange([v3.x, v3.y, v3.z]);
    indices.AddRange([offset, offset + 1, offset + 2, offset, offset + 2, offset + 3]);
    offset += 4;
  }

  public void Update(float deltaTime)
  {
    _offset += CloudSpeed * deltaTime;
  }

  public void Draw(Matrix4 view, Matrix4 projection, Vector3 playerPos, float skyLight)
  {
    if (_indexCount == 0) return;

    float brightness = Math.Clamp(skyLight * 0.5f + 0.5f, 0.5f, 1.0f);
    var cloudColor = new Vector4(brightness, brightness, brightness, 0.4f);

    float tileSize = 60f * 4f; // 240 Blöcke
    float baseX = MathF.Floor(playerPos.X / tileSize) * tileSize;
    float baseZ = MathF.Floor(playerPos.Z / tileSize) * tileSize;

    GL.UseProgram(_shader);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uView"), false, ref view);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uProjection"), false, ref projection);
    GL.Uniform4(GL.GetUniformLocation(_shader, "uCloudColor"),
        cloudColor.X, cloudColor.Y, cloudColor.Z, cloudColor.W);

    GL.Disable(EnableCap.CullFace);
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    GL.DepthMask(false);
    GL.BindVertexArray(_vao);

    // 3x3 Kacheln um Player
    for (int tx = -1; tx <= 1; tx++)
    {
      for (int tz = -1; tz <= 1; tz++)
      {
        var model = Matrix4.CreateTranslation(
            baseX + tx * tileSize + _offset,
            0f,
            baseZ + tz * tileSize);

        GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uModel"), false, ref model);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount,
            DrawElementsType.UnsignedInt, 0);
      }
    }

    GL.DepthMask(true);
    GL.Disable(EnableCap.Blend);
    GL.Enable(EnableCap.CullFace);
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