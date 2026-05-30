// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World;

public sealed class Chunk
{
  public const int Size = 16;

  public BlockType[] Blocks { get; } = 
    new BlockType[Size * Size * Size];
}