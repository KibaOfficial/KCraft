// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Rendering;

public static class UiScale
{
  public static float Scale { get; set; } = 2f;

  public static void CycleUp() => Scale = Scale >= 4f ? 1f : Scale + 1f;
  public static void CycleDown() => Scale = Scale <= 1f ? 4f : Scale - 1f;
}