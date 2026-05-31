// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using OpenTK.Mathematics;

namespace KCraft.World;

public sealed class WorldTicker
{
  private const float SecondsPerTick = 1f / WorldTime.TicksPerSecond; // 0.05s

  private float _accumulator = 0f;
  private int _ticksThisSecond = 0;
  private float _tpsTimer = 0f;

  public WorldTime Time { get; } = new();
  public float CurrentTps { get; private set; } = 20f;
  public Player? Player { get; set; }
  private Func<int, int, int, Block?>? _getBlock;
  public float Accumulator => _accumulator;
  public Vector3 PlayerPrevPosition { get; private set; }

  // Wird jeden Frame aufgerufen mit deltaTime in Sekunden
  // Gibt zurück wie viele Ticks gefeuert wurden
  public int Update(float deltaTime)
  {
    _accumulator += deltaTime;
    _tpsTimer += deltaTime;

    int ticks = 0;
    while (_accumulator >= SecondsPerTick)
    {
      _accumulator -= SecondsPerTick;
      Tick();
      ticks++;
      _ticksThisSecond++;
    }

    // TPS messen
    if (_tpsTimer >= 1f)
    {
      CurrentTps = _ticksThisSecond;
      _ticksThisSecond = 0;
      _tpsTimer -= 1f;
    }

    return ticks;
  }

  private void Tick()
  {
    PlayerPrevPosition = Player?.Position ?? Vector3.Zero;
    // 1. Weltzeit
    Time.Tick();

    // 2. Weather → TODO
    // 3. Block Ticks → TODO
    // 4. Fluid Ticks → TODO
    // 5. Random Ticks → TODO
    // 6. Entity Tick
    Player?.Tick(_getBlock!);
  }
  public void SetGetBlock(Func<int, int, int, Block?> getBlock)
      => _getBlock = getBlock;

  
}