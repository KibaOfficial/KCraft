// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.World;

public sealed class WorldSaveData
{
  public string WorldName { get; set; } = "New World";
  public int Seed { get; set; } = 42;
  public float PlayerX { get; set; } = 8f;
  public float PlayerY { get; set; } = 80f;
  public float PlayerZ { get; set; } = -10f;
  public float CameraYaw { get; set; } = 0f;
  public float CameraPitch { get; set; } = 0f;
  public int GameMode { get; set; } = 0; // 0=Survival, 1=Creative, 2=Spectator
  public long TotalTicks { get; set; } = 6000;
  public DateTime LastPlayed { get; set; } = DateTime.Now;
}