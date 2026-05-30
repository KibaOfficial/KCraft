// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.World.Generation;

public interface IWorldGenerator
{
  /// <summary>
  /// Generates the given chunk.
  /// </summary>
  void Generate(Chunk chunk);
}