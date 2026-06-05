// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using System.Text.Json;
using OpenTK.Mathematics;

namespace KCraft.World;

public static class WorldSaveManager
{
  private static string WorldsPath => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "KCraft", "worlds");

  public static string GetWorldPath(string worldName)
      => Path.Combine(WorldsPath, worldName);

  public static string[] GetWorldNames()
  {
    if (!Directory.Exists(WorldsPath)) return [];
    return Directory.GetDirectories(WorldsPath)
        .Select(Path.GetFileName)
        .Where(n => n != null)
        .ToArray()!;
  }

  public static void Save(string worldName, WorldSaveData data,
      IEnumerable<(Chunk chunk, int cx, int cz)> chunks)
  {
    var worldPath = GetWorldPath(worldName);
    var chunksPath = Path.Combine(worldPath, "chunks");
    Directory.CreateDirectory(chunksPath);

    // world.json
    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(worldPath, "world.json"), json);

    // Chunks
    foreach (var (chunk, cx, cz) in chunks)
    {
      var path = Path.Combine(chunksPath, $"{cx}_{cz}.kchunk");
      File.WriteAllBytes(path, chunk.GetRawBlocks().ToArray());

      // Metadata speichern
      var metaPath = Path.Combine(chunksPath, $"{cx}_{cz}.kmeta");
      File.WriteAllBytes(metaPath, chunk.GetRawMetadata().ToArray());
    }
  }

  public static (WorldSaveData? data, Dictionary<(int cx, int cz), byte[]> chunks, Dictionary<(int cx, int cz), byte[]> metadata) Load(string worldName)
  {
    var worldPath = GetWorldPath(worldName);
    var jsonPath = Path.Combine(worldPath, "world.json");

    if (!File.Exists(jsonPath))
      return (null, [], []);  // ← drei Werte!

    var json = File.ReadAllText(jsonPath);
    var data = JsonSerializer.Deserialize<WorldSaveData>(json);

    var chunks = new Dictionary<(int, int), byte[]>();
    var metadata = new Dictionary<(int, int), byte[]>();
    var chunksPath = Path.Combine(worldPath, "chunks");

    if (Directory.Exists(chunksPath))
    {
      foreach (var file in Directory.GetFiles(chunksPath, "*.kchunk"))
      {
        var name = Path.GetFileNameWithoutExtension(file);
        var parts = name.Split('_');
        if (parts.Length != 2) continue;
        if (!int.TryParse(parts[0], out int cx)) continue;
        if (!int.TryParse(parts[1], out int cz)) continue;
        chunks[(cx, cz)] = File.ReadAllBytes(file);

        // Metadata laden wenn vorhanden
        var metaFile = Path.ChangeExtension(file, ".kmeta");
        if (File.Exists(metaFile))
          metadata[(cx, cz)] = File.ReadAllBytes(metaFile);
      }
    }

    return (data, chunks, metadata);
  }

  public static void Delete(string worldName)
  {
    var path = GetWorldPath(worldName);
    if (Directory.Exists(path))
      Directory.Delete(path, recursive: true);
  }

  public static bool WorldExists(string worldName)
      => File.Exists(Path.Combine(GetWorldPath(worldName), "world.json"));
}