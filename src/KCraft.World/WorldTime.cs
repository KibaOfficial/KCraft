// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.World;

public sealed class WorldTime
{
  public const int TicksPerDay = 24000;
  public const int TicksPerSecond = 20;

  // Tageszeit Konstanten
  public const int Dawn = 0;      // Sonnenaufgang
  public const int Noon = 6000;   // Sonnenhöchststand
  public const int Dusk = 12000;  // Sonnenuntergang
  public const int Midnight = 18000;  // Mitternacht

  // Gesamte Weltzeit (wächst unbegrenzt, nie reset)
  public long TotalTicks { get; private set; } = 6000; // Startzeit: Mittag
  // public long TotalTicks { get; private set; } = 11500; // Startzeit: kurz vor Sonnenuntergang

  // Tageszeit (0-23999)
  public int DayTime => (int)(TotalTicks % TicksPerDay);

  // 0.0 = Sonnenaufgang, 1.0 = Nächster Sonnenaufgang
  public float DayProgress => DayTime / (float)TicksPerDay;

  // Sonnenwinkel in Grad (0° = Aufgang, 180° = Untergang)
  public float SunAngle => 90f - (DayTime / 12000f) * 180f;

  // Ist es Tag? (Ticks 0-12999)
  public bool IsDay => DayTime < 13000;

  // Wie hell ist es? 0.0 (Nacht) bis 1.0 (Mittag)
  public float SkyLight
  {
    get
    {
      // Dämmerung: Ticks 22000-24000 und 0-2000
      if (DayTime < 2000)
        return DayTime / 2000f;
      if (DayTime < 10000)
        return 1.0f;
      if (DayTime < 12000)
        return 1f - (DayTime - 10000) / 2000f;
      if (DayTime < 22000)
        return 0.05f; // Nacht: minimales Mondlicht
      return (DayTime - 22000) / 2000f;
    }
  }

  public int Day => (int)(TotalTicks / TicksPerDay);

  public void Tick() => TotalTicks++;

  public string TimeString
  {
    get
    {
      // MC Zeit → echte Uhrzeit (0 Ticks = 6:00 Uhr)
      int minutes = (int)((DayTime / (float)TicksPerDay) * 1440 + 360) % 1440;
      return $"{minutes / 60:D2}:{minutes % 60:D2}";
    }
  }
}