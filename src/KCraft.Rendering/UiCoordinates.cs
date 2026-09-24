// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering;

public static class UiCoordinates
{
  public static Vector2 ToScreen(Vector2 ui)
    => ui * UiScale.Scale;

  public static float ToScreen(float value)
    => value * UiScale.Scale;

  public static Vector2 ToUi(Vector2 screen)
    => screen / UiScale.Scale;

  public static float ToUi(float value)
    => value / UiScale.Scale;
}