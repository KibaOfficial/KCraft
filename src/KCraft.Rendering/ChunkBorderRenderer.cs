// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class ChunkBorderRenderer : IDisposable
{
    private int _vao, _vbo, _shader;
    public bool Visible { get; set; } = false;

    private const string VertSrc = """
        #version 410 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec4 aColor;
        out vec4 vColor;
        uniform mat4 uView;
        uniform mat4 uProjection;
        void main()
        {
          gl_Position = uProjection * uView * vec4(aPos, 1.0);
          vColor = aColor;
        }
        """;

    private const string FragSrc = """
        #version 410 core
        in vec4 vColor;
        out vec4 FragColor;
        void main()
        {
          FragColor = vColor;
        }
        """;

    public ChunkBorderRenderer()
    {
        int vert = Compile(ShaderType.VertexShader,   VertSrc);
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
        // 7 floats: x y z r g b a
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    public void Draw(Camera camera, Matrix4 view, Matrix4 projection)
    {
        if (!Visible) return;

        var pos = camera.Position;
        int cx = (int)MathF.Floor(pos.X / 16) * 16;
        int cz = (int)MathF.Floor(pos.Z / 16) * 16;

        float x0 = cx,      z0 = cz;
        float x1 = cx + 16, z1 = cz + 16;
        float yMin = 0,      yMax = 256;

        // Farbe: Gelb wie in Minecraft
        var col = new Vector4(1.0f, 1.0f, 0.0f, 0.8f);
        // Andere Kanten leicht anders für Tiefeneffekt
        var colFade = new Vector4(1.0f, 0.8f, 0.0f, 0.5f);

        var verts = new List<float>();

        // 4 vertikale Eckenlinien
        AddLine(verts, x0, yMin, z0, x0, yMax, z0, col);
        AddLine(verts, x1, yMin, z0, x1, yMax, z0, col);
        AddLine(verts, x0, yMin, z1, x0, yMax, z1, col);
        AddLine(verts, x1, yMin, z1, x1, yMax, z1, col);

        // Oben: 4 horizontale Linien
        AddLine(verts, x0, yMax, z0, x1, yMax, z0, colFade);
        AddLine(verts, x1, yMax, z0, x1, yMax, z1, colFade);
        AddLine(verts, x1, yMax, z1, x0, yMax, z1, colFade);
        AddLine(verts, x0, yMax, z1, x0, yMax, z0, colFade);

        // Unten: 4 horizontale Linien
        AddLine(verts, x0, yMin, z0, x1, yMin, z0, colFade);
        AddLine(verts, x1, yMin, z0, x1, yMin, z1, colFade);
        AddLine(verts, x1, yMin, z1, x0, yMin, z1, colFade);
        AddLine(verts, x0, yMin, z1, x0, yMin, z0, colFade);

        var data = verts.ToArray();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);

        GL.UseProgram(_shader);
        GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uView"),       false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uProjection"), false, ref projection);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.LineWidth(2.0f);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, data.Length / 7);

        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
    }

    private static void AddLine(List<float> v,
        float x0, float y0, float z0,
        float x1, float y1, float z1,
        Vector4 c)
    {
        v.AddRange([x0, y0, z0, c.X, c.Y, c.Z, c.W]);
        v.AddRange([x1, y1, z1, c.X, c.Y, c.Z, c.W]);
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