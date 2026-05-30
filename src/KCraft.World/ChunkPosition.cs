// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.World;

public readonly record struct ChunkPosition(int X, int Z)
{
  public override string ToString() => $"ChunkPosition({X}, {Z})";
}