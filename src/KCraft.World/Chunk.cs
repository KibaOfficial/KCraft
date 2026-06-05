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
  private readonly byte[] _fluidLevels = new byte[Volume]; // 0=Source, 1-7=Flowing, 255=kein Fluid
  private readonly byte[] _metadata = new byte[Volume];

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

  // ── Fluid Level ───────────────────────────────────────────────────────
  public byte GetFluidLevel(int x, int y, int z)
  {
    ValidateCoordinates(x, y, z);
    return _fluidLevels[GetIndex(x, y, z)];
  }

  public void SetFluidLevel(int x, int y, int z, byte level)
  {
    ValidateCoordinates(x, y, z);
    _fluidLevels[GetIndex(x, y, z)] = level;
  }

  // ── Serialization ─────────────────────────────────────────────────────
  public ReadOnlySpan<byte> GetRawBlocks() => _blocks;
  public ReadOnlySpan<byte> GetRawFluidLevels() => _fluidLevels;

  public void LoadRawBlocks(byte[] data)
  {
    if (data.Length != Volume)
      throw new ArgumentException($"Expected {Volume} bytes, got {data.Length}");
    Buffer.BlockCopy(data, 0, _blocks, 0, Volume);
  }

  public void LoadRawFluidLevels(byte[] data)
  {
    if (data.Length != Volume)
      throw new ArgumentException($"Expected {Volume} bytes, got {data.Length}");
    Buffer.BlockCopy(data, 0, _fluidLevels, 0, Volume);
  }

  public bool IsInside(int x, int y, int z)
      => x >= 0 && x < Width
      && y >= 0 && y < Height
      && z >= 0 && z < Depth;

  // Metadata Access
  public byte GetMetadata(int x, int y, int z)
  {
    ValidateCoordinates(x, y, z);
    return _metadata[GetIndex(x, y, z)];
  }

  public void SetMetadata(int x, int y, int z, byte value)
  {
    ValidateCoordinates(x, y, z);
    _metadata[GetIndex(x, y, z)] = value;
  }

  // Serialization
  public ReadOnlySpan<byte> GetRawMetadata() => _metadata;

  public void LoadRawMetadata(byte[] data)
  {
    if (data.Length != Volume)
      throw new ArgumentException($"Expected {Volume} bytes, got {data.Length}");
    Buffer.BlockCopy(data, 0, _metadata, 0, Volume);
  }
}