// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace KCraft.Assets;

public sealed class Texture2D : IDisposable
{
  public int Handle { get; }
  public int Width  { get; }
  public int Height { get; }

  public Texture2D(string path)
  {
    StbImage.stbi_set_flip_vertically_on_load(1); // OpenGL erwartet Y-flip

    using var stream = File.OpenRead(path);
    var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

    Width  = image.Width;
    Height = image.Height;

    Handle = GL.GenTexture();
    GL.BindTexture(TextureTarget.Texture2D, Handle);

    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
      Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

    // Nearest-Neighbor = Minecraft-Look, kein Blur
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

    GL.BindTexture(TextureTarget.Texture2D, 0);
  }

  public void Bind(TextureUnit unit = TextureUnit.Texture0)
  {
    GL.ActiveTexture(unit);
    GL.BindTexture(TextureTarget.Texture2D, Handle);
  }

  public void Dispose()
  {
    GL.DeleteTexture(Handle);
  }
}