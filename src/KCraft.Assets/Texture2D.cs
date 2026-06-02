// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace KCraft.Assets;

public sealed class Texture2D : IDisposable
{
  public int Handle { get; }
  public int Width { get; }
  public int Height { get; }

  public Texture2D(string path, bool flipVertically = true, int frameIndex = 0)
  {
    StbImage.stbi_set_flip_vertically_on_load(flipVertically ? 1 : 0);

    using var stream = File.OpenRead(path);
    var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

    // Frame-Größe = Breite (quadratisch)
    int frameSize = image.Width;
    int frameCount = image.Height / frameSize;
    frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);

    Width = frameSize;
    Height = frameSize;

    int actualFrame = flipVertically ? (frameCount - 1 - frameIndex) : frameIndex;
    int byteOffset = actualFrame * frameSize * frameSize * 4;
    var frameData = image.Data.AsSpan(byteOffset, frameSize * frameSize * 4).ToArray();

    Handle = GL.GenTexture();
    GL.BindTexture(TextureTarget.Texture2D, Handle);

    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
        Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, frameData);

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