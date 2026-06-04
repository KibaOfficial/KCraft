// Copyright (c) 2026 KibaOfficial
// All rights reserved.

namespace KCraft.Assets;

public sealed class TextureManager : IDisposable
{
  private readonly Dictionary<string, Texture2D> _cache = new();
  private readonly string _basePath;

  public TextureManager(string basePath)
  {
    _basePath = basePath;
  }

  public Texture2D Get(string name, int frameIndex = 0)
  {
    string key = $"{name}#{frameIndex}";
    if (_cache.TryGetValue(key, out var tex))
      return tex;

    var path = Path.Combine(_basePath, $"{name}.png");
    var loaded = new Texture2D(path, frameIndex: frameIndex);
    _cache[key] = loaded;
    return loaded;
  }

  public Texture2D GetCTMTile(string ctmTextureName, int tileIndex)
  {
    string key = $"{ctmTextureName}#{tileIndex}";
    if (_cache.TryGetValue(key, out var tex)) return tex;

    var path = Path.Combine(_basePath, $"{ctmTextureName}.png");
    var loaded = new Texture2D(path, tileX: tileIndex % 4, tileY: tileIndex / 4, tilesPerRow: 4);
    _cache[key] = loaded;
    return loaded;
  }

  public void Dispose()
  {
    foreach (var tex in _cache.Values)
      tex.Dispose();
    _cache.Clear();
  }
}
