// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World;

public sealed class WaterSimulator
{
  public const byte SourceLevel = 0;
  public const byte MaxLevel = 7; // Level 8 = verschwindet

  private readonly Func<int, int, int, (Block block, byte level)?> _getWorldFluid;
  private readonly Action<int, int, int, Block, byte> _setWorldFluid;
  private readonly Action<int, int, int> _markDirty;

  // Scheduled Ticks — (wx, wy, wz, tickAt)
  private readonly Queue<(int wx, int wy, int wz)> _pendingUpdates = new();
  private readonly HashSet<(int wx, int wy, int wz)> _scheduledSet = [];

  public WaterSimulator(
      Func<int, int, int, (Block block, byte level)?> getWorldFluid,
      Action<int, int, int, Block, byte> setWorldFluid,
      Action<int, int, int> markDirty)
  {
    _getWorldFluid = getWorldFluid;
    _setWorldFluid = setWorldFluid;
    _markDirty = markDirty;
  }

  // Block-Update schedulen (z.B. wenn Wasser platziert wird)
  public void ScheduleUpdate(int wx, int wy, int wz)
  {
    if (_scheduledSet.Add((wx, wy, wz)))
      _pendingUpdates.Enqueue((wx, wy, wz));
  }

  // Alle 5 Ticks aufrufen
  public void Tick()
  {
    // Max 64 Updates pro Tick um Lag zu vermeiden
    int processed = 0;
    var nextRound = new List<(int wx, int wy, int wz)>();

    while (_pendingUpdates.Count > 0 && processed < 64)
    {
      var (wx, wy, wz) = _pendingUpdates.Dequeue();
      _scheduledSet.Remove((wx, wy, wz));
      processed++;

      var current = _getWorldFluid(wx, wy, wz);
      if (current == null) continue;
      var (block, level) = current.Value;
      if (block != Block.Water) continue;

      // ── Prio 1: nach unten fließen ────────────────────────────
      var below = _getWorldFluid(wx, wy - 1, wz);
      if (below != null && below.Value.block == Block.Air)
      {
        _setWorldFluid(wx, wy - 1, wz, Block.Water, SourceLevel);
        nextRound.Add((wx, wy - 1, wz));
        continue; // runter hat Priorität
      }

      // ── Prio 2: horizontal mit Level + 1 ─────────────────────
      if (level >= MaxLevel) continue; // zu weit weg → kein Spread

      byte nextLevel = (byte)(level + 1);
      int[] dx = [-1, 1, 0, 0];
      int[] dz = [0, 0, -1, 1];

      for (int i = 0; i < 4; i++)
      {
        int nx = wx + dx[i];
        int nz = wz + dz[i];
        var neighbor = _getWorldFluid(nx, wy, nz);
        if (neighbor == null) continue;
        var (nBlock, nLevel) = neighbor.Value;

        if (nBlock == Block.Air)
        {
          _setWorldFluid(nx, wy, nz, Block.Water, nextLevel);
          nextRound.Add((nx, wy, nz));
        }
        else if (nBlock == Block.Water && nLevel > nextLevel)
        {
          // Stärkeres Wasser überschreibt schwächeres
          _setWorldFluid(nx, wy, nz, Block.Water, nextLevel);
          nextRound.Add((nx, wy, nz));
        }
      }
    }

    // ── Decay: Flowing Wasser ohne Source-Nachbar entfernen ──────
    // (vereinfacht — nur wenn kein direkter Source-Nachbar)
    foreach (var (wx, wy, wz) in nextRound)
      ScheduleUpdate(wx, wy, wz);
  }
}