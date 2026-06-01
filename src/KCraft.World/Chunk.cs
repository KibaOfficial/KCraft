// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World;

public sealed class Chunk
{
  public const int Width = 16;
  public const int Height = 256;
  public const int Depth = 16;

  public const int Volume = Width * Height * Depth;

  private readonly byte[] _blocks = new byte[Volume];
  public ReadOnlySpan<byte> GetRawBlocks() => _blocks;

  private static void ValidateCoordinates(int x, int y, int z)
  {
    if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
    if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));
    if (z < 0 || z >= Depth) throw new ArgumentOutOfRangeException(nameof(z));
  }

  private static int GetIndex(int x, int y, int z)
    => (y * Depth + z) * Width + x;

  public Block GetBlock(int x, int y, int z)
  {
    ValidateCoordinates(x, y, z);
    return (Block)_blocks[GetIndex(x, y, z)];
  }

  public void SetBlock(int x, int y, int z, Block block)
  {
    ValidateCoordinates(x, y, z);
    _blocks[GetIndex(x, y, z)] = (byte)block;
  }

  public void LoadRawBlocks(byte[] data)
  {
    if (data.Length != Volume)
      throw new ArgumentException($"Expected {Volume} bytes, got {data.Length}");
    Buffer.BlockCopy(data, 0, _blocks, 0, Volume);
  }

  public bool IsInside(int x, int y, int z)
    => x >= 0 && x < Width
    && y >= 0 && y < Height
    && z >= 0 && z < Depth;
}