// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Core;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class SkyRenderer : IDisposable
{
  private int _vao, _vbo, _shader;

  private const string VertSrc = """
    #version 410 core
    layout(location = 0) in vec2 aPos;
    out vec3 vDir;
    uniform mat4 uInvView;
    uniform float uAspect;
    uniform float uFovTan;
    void main()
    {
        vec3 dir = vec3(aPos.x * uAspect * uFovTan, aPos.y * uFovTan, -1.0);
        vDir = mat3(uInvView) * dir;
        gl_Position = vec4(aPos, 0.999, 1.0);
    }
    """;

  private const string FragSrc = """
    #version 410 core
    in vec3 vDir;
    out vec4 FragColor;
    uniform vec3 uSkyTop;
    uniform vec3 uSkyHorizon;
    uniform vec3 uSunColor;
    uniform vec3 uSunDir;
    uniform vec3 uMoonDir;
    uniform float uNightFactor;
    void main()
    {
        vec3 dir = normalize(vDir);
        float t = clamp(dir.y * 2.0, 0.0, 1.0);
        vec3 sky = mix(uSkyHorizon, uSkyTop, t);

        float sunDot = dot(dir, normalize(uSunDir));
        float sun = smoothstep(0.9995, 0.9999, sunDot);
        sky = mix(sky, uSunColor, sun);

        float moonDot = dot(dir, normalize(uMoonDir));
        float moon = smoothstep(0.9985, 0.9990, moonDot);
        sky = mix(sky, vec3(0.95, 0.95, 1.0), moon * uNightFactor);

        FragColor = vec4(sky, 1.0);
    }
    """;

  private static readonly float[] Quad =
  [
      -1f, -1f,
       1f, -1f,
       1f,  1f,
      -1f, -1f,
       1f,  1f,
      -1f,  1f,
  ];

  public SkyRenderer()
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
    GL.BufferData(BufferTarget.ArrayBuffer, Quad.Length * sizeof(float), Quad, BufferUsageHint.StaticDraw);
    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.BindVertexArray(0);
  }

  public void Draw(WorldTime time, Matrix4 view, Matrix4 projection, Camera camera, float aspect)
  {
    float angle = MathHelper.DegreesToRadians(time.SunAngle);
    var sunDir = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f);
    var moonDir = -sunDir;

    float light = time.SkyLight;
    int dayTime = time.DayTime;
    float nightFactor = 1f - Math.Clamp(light * 2f, 0f, 1f);

    float duskFactor = 0f;
    if (dayTime >= 10000 && dayTime < 12000) duskFactor = (dayTime - 10000) / 2000f;
    else if (dayTime >= 12000 && dayTime < 14000) duskFactor = 1f - (dayTime - 12000) / 2000f;
    else if (dayTime >= 22000 && dayTime < 24000) duskFactor = (dayTime - 22000) / 2000f;
    else if (dayTime < 2000) duskFactor = 1f - dayTime / 2000f;

    var skyTop = Lerp(Lerp(NightTop, DayTop, light), DuskTop, duskFactor);
    var skyHorizon = Lerp(Lerp(NightHorizon, DayHorizon, light), DuskHorizon, duskFactor);
    var sunColor = Lerp(time.IsDay ? SunDay : MoonNight, SunDusk, duskFactor);

    float fovTan = MathF.Tan(MathHelper.DegreesToRadians(GameSettings.Fov) / 2f);

    // View ohne Translation invertieren → World-Space Richtungen
    var viewNoTrans = view;
    viewNoTrans.Row3 = new Vector4(0, 0, 0, 1);
    var invView = Matrix4.Invert(viewNoTrans);

    GL.UseProgram(_shader);
    GL.UniformMatrix4(GL.GetUniformLocation(_shader, "uInvView"), false, ref invView);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uAspect"), aspect);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uFovTan"), fovTan);
    GL.Uniform3(GL.GetUniformLocation(_shader, "uSkyTop"), skyTop.X, skyTop.Y, skyTop.Z);
    GL.Uniform3(GL.GetUniformLocation(_shader, "uSkyHorizon"), skyHorizon.X, skyHorizon.Y, skyHorizon.Z);
    GL.Uniform3(GL.GetUniformLocation(_shader, "uSunColor"), sunColor.X, sunColor.Y, sunColor.Z);
    GL.Uniform3(GL.GetUniformLocation(_shader, "uSunDir"), sunDir.X, sunDir.Y, sunDir.Z);
    GL.Uniform3(GL.GetUniformLocation(_shader, "uMoonDir"), moonDir.X, moonDir.Y, moonDir.Z);
    GL.Uniform1(GL.GetUniformLocation(_shader, "uNightFactor"), nightFactor);

    GL.Disable(EnableCap.DepthTest);
    GL.Disable(EnableCap.CullFace);
    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    GL.Enable(EnableCap.CullFace);
    GL.Enable(EnableCap.DepthTest);
  }

  private static Vector3 DayTop = new(0.30f, 0.55f, 0.90f);
  private static Vector3 DayHorizon = new(0.65f, 0.82f, 0.98f);
  private static Vector3 DuskTop = new(0.08f, 0.10f, 0.25f);
  private static Vector3 DuskHorizon = new(0.90f, 0.45f, 0.08f);
  private static Vector3 NightTop = new(0.01f, 0.01f, 0.06f);
  private static Vector3 NightHorizon = new(0.04f, 0.04f, 0.10f);
  private static Vector3 SunDay = new(1.0f, 0.97f, 0.85f);
  private static Vector3 SunDusk = new(1.0f, 0.50f, 0.10f);
  private static Vector3 MoonNight = new(0.85f, 0.88f, 0.95f);

  private static Vector3 Lerp(Vector3 a, Vector3 b, float t)
      => a + (b - a) * Math.Clamp(t, 0f, 1f);

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