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

  public Texture2D Get(string name)
  {
    if (_cache.TryGetValue(name, out var tex))
      return tex;

    var path = Path.Combine(_basePath, $"{name}.png");
    var loaded = new Texture2D(path);
    _cache[name] = loaded;
    return loaded;
  }

  public void Dispose()
  {
    foreach (var tex in _cache.Values)
      tex.Dispose();
    _cache.Clear();
  }
}