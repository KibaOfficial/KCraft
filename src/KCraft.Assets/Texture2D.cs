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

  // Vertikales Spritesheet (Animationen wie water_still)
  public Texture2D(string path, bool flipVertically = true, int frameIndex = 0)
  {
    StbImage.stbi_set_flip_vertically_on_load(flipVertically ? 1 : 0);

    using var stream = File.OpenRead(path);
    var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

    int frameSize = image.Width;
    int frameCount = image.Height / frameSize;
    frameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);

    Width = frameSize;
    Height = frameSize;

    int actualFrame = flipVertically ? (frameCount - 1 - frameIndex) : frameIndex;
    int byteOffset = actualFrame * frameSize * frameSize * 4;
    var frameData = image.Data.AsSpan(byteOffset, frameSize * frameSize * 4).ToArray();

    Handle = Upload(frameData, Width, Height);
  }

  // Grid Spritesheet (CTM — tileX/tileY in einem NxM Grid)
  public Texture2D(string path, int tileX, int tileY, int tilesPerRow, bool flipVertically = false)
  {
    StbImage.stbi_set_flip_vertically_on_load(0); // CTM nicht flippen

    using var stream = File.OpenRead(path);
    var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

    int tileW = image.Width / tilesPerRow;
    int tileH = tileW; // quadratisch

    Width = tileW;
    Height = tileH;

    // Tile aus Grid extrahieren
    var tileData = new byte[tileW * tileH * 4];
    int srcY = tileY * tileH;
    int srcX = tileX * tileW;

    for (int row = 0; row < tileH; row++)
    {
      int srcOffset = ((srcY + row) * image.Width + srcX) * 4;
      int destOffset = row * tileW * 4;
      image.Data.AsSpan(srcOffset, tileW * 4).CopyTo(tileData.AsSpan(destOffset));
    }

    // Vertikal flippen für OpenGL
    var flipped = new byte[tileData.Length];
    for (int row = 0; row < tileH; row++)
    {
      int src = row * tileW * 4;
      int dest = (tileH - 1 - row) * tileW * 4;
      tileData.AsSpan(src, tileW * 4).CopyTo(flipped.AsSpan(dest));
    }

    Handle = Upload(flipped, Width, Height);
  }

  private static int Upload(byte[] data, int width, int height)
  {
    int handle = GL.GenTexture();
    GL.BindTexture(TextureTarget.Texture2D, handle);

    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
        width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

    GL.BindTexture(TextureTarget.Texture2D, 0);
    return handle;
  }

  public void Bind(TextureUnit unit = TextureUnit.Texture0)
  {
    GL.ActiveTexture(unit);
    GL.BindTexture(TextureTarget.Texture2D, Handle);
  }

  public void Dispose() => GL.DeleteTexture(Handle);
}