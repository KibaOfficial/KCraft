// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;

namespace KCraft.Rendering.Shaders;

public sealed class WorldShader : IDisposable
{
  public int Handle { get; }

  public int ModelLocation { get; }
  public int ViewLocation { get; }
  public int ProjectionLocation { get; }
  public int AmbientLocation { get; }
  public int AlphaLocation { get; }
  public int TextureLocation { get; }
  public int TintLocation { get; }

  private const string VertexShaderSource = """
    #version 410 core

    layout(location = 0) in vec3 aPosition;
    layout(location = 1) in vec2 aTexCoord;
    layout(location = 2) in float aBrightness;

    out vec2 vTexCoord;
    out float vBrightness;

    uniform mat4 uModel;
    uniform mat4 uView;
    uniform mat4 uProjection;

    void main()
    {
      vTexCoord = aTexCoord;
      vBrightness = aBrightness;

      gl_Position =
        uProjection *
        uView *
        uModel *
        vec4(aPosition, 1.0);
    }
    """;

  private const string FragmentShaderSource = """
    #version 410 core

    in vec2 vTexCoord;
    in float vBrightness;

    out vec4 FragColor;

    uniform sampler2D uTexture;
    uniform vec3 uTint;
    uniform float uAmbient;
    uniform float uAlpha;

    void main()
    {
      vec4 color = texture(uTexture, vTexCoord);

      if (color.a < 0.1)
        discard;

      FragColor = vec4(
        color.rgb * uTint * uAmbient * vBrightness,
        color.a * uAlpha
      );
    }
    """;

  public WorldShader()
  {
    int vertexShader = CompileShader(ShaderType.VertexShader, VertexShaderSource);

    int fragmentShader = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);

    try
    {
      Handle = GL.CreateProgram();

      GL.AttachShader(Handle, vertexShader);
      GL.AttachShader(Handle, fragmentShader);
      GL.LinkProgram(Handle);

      GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linked);

      if (linked == 0)
      {
        throw new InvalidOperationException($"Shader link error:{GL.GetProgramInfoLog(Handle)}");
      }
    }
    finally
    {
      GL.DeleteShader(vertexShader);
      GL.DeleteShader(fragmentShader);
    }

    ModelLocation = GetUniformLocation("uModel");
    ViewLocation = GetUniformLocation("uView");
    ProjectionLocation = GetUniformLocation("uProjection");
    AmbientLocation = GetUniformLocation("uAmbient");
    AlphaLocation = GetUniformLocation("uAlpha");
    TextureLocation = GetUniformLocation("uTexture");
    TintLocation = GetUniformLocation("uTint");
  }

  public void Use()
  {
    GL.UseProgram(Handle);
  }

  public void Dispose()
  {
    GL.DeleteProgram(Handle);
  }

  private int GetUniformLocation(string name)
  {
    int location = GL.GetUniformLocation(Handle, name);

    if (location == -1)
      throw new InvalidOperationException(
        $"Uniform '{name}' was not found in world shader.");

    return location;
  }

  private static int CompileShader(
    ShaderType type,
    string source)
  {
    int shader = GL.CreateShader(type);

    GL.ShaderSource(shader, source);
    GL.CompileShader(shader);

    GL.GetShader(
      shader,
      ShaderParameter.CompileStatus,
      out int success);

    if (success == 0)
    {
      string log = GL.GetShaderInfoLog(shader);
      GL.DeleteShader(shader);

      throw new InvalidOperationException(
        $"Shader compile error ({type}): {log}");
    }

    return shader;
  }
}