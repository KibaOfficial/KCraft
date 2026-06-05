// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using KCraft.World;
using KCraft.World.Generation;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class WorldManager : IDisposable
{
  // ── Config ────────────────────────────────────────────────────────────
  public const int RenderRadius = 8;
  public const int LoadRadius = 10;
  public const int UnloadRadius = 13;
  public int Seed { get; private set; } = 42;

  // ── Data ──────────────────────────────────────────────────────────────
  public List<(ChunkMesh mesh, Chunk chunk, Vector3i chunkPos)> ChunkMeshes { get; } = [];
  public int ChunkCount => ChunkMeshes.Count;

  private readonly NoiseWorldGenerator _generator;
  private readonly HashSet<(int cx, int cz)> _loadedSet = [];
  private readonly Dictionary<(int cx, int cz), Chunk> _chunkLookup = new();
  private readonly HashSet<(int cx, int cz)> _dirtyChunks = [];
  private readonly HashSet<(int wx, int wy, int wz)> _activeWater = [];
  private readonly WaterSimulator _waterSim;
  private int _waterTickCounter = 0;

  private bool IsLoaded(int cx, int cz) => _loadedSet.Contains((cx, cz));

  // ── Init ──────────────────────────────────────────────────────────────
  public WorldManager(int seed = 42, bool lazyLoad = false)
  {
    Seed = seed;
    _generator = new NoiseWorldGenerator(seed: seed);
    _waterSim = new WaterSimulator(GetWorldFluid, SetWorldFluid, MarkDirty);

    if (!lazyLoad)
    {
      for (int cx = -RenderRadius; cx <= RenderRadius; cx++)
        for (int cz = -RenderRadius; cz <= RenderRadius; cz++)
          LoadChunk(cx, cz);
    }
  }

  // ── Dynamic Chunk Loading ─────────────────────────────────────────────
  public void UpdateChunks(Vector3 playerPos, int loadRadius = LoadRadius, int unloadRadius = UnloadRadius)
  {
    int pcx = (int)MathF.Floor(playerPos.X / Chunk.Width);
    int pcz = (int)MathF.Floor(playerPos.Z / Chunk.Depth);

    // Unload
    for (int i = ChunkMeshes.Count - 1; i >= 0; i--)
    {
      var (mesh, _, pos) = ChunkMeshes[i];
      if (Math.Abs(pos.X - pcx) > unloadRadius || Math.Abs(pos.Z - pcz) > unloadRadius)
      {
        if (_chunkLookup.TryGetValue((pos.X, pos.Z), out var unloadChunk))
        {
          for (int x = 0; x < Chunk.Width; x++)
            for (int y = 0; y < Chunk.Height; y++)
              for (int z = 0; z < Chunk.Depth; z++)
              {
                if (unloadChunk.GetBlock(x, y, z) == Block.Water)
                  _activeWater.Remove((pos.X * Chunk.Width + x, y, pos.Z * Chunk.Depth + z));
              }
        }

        mesh.Dispose();
        ChunkMeshes.RemoveAt(i);
        _chunkLookup.Remove((pos.X, pos.Z));
        _loadedSet.Remove((pos.X, pos.Z));
      }
    }

    // Load — Spiral von innen nach außen, 1 Chunk pro Frame
    for (int radius = 0; radius <= loadRadius; radius++)
      for (int cx = pcx - radius; cx <= pcx + radius; cx++)
        for (int cz = pcz - radius; cz <= pcz + radius; cz++)
        {
          if (Math.Abs(cx - pcx) != radius && Math.Abs(cz - pcz) != radius) continue;
          if (Math.Abs(cx - pcx) > loadRadius || Math.Abs(cz - pcz) > loadRadius) continue;
          if (IsLoaded(cx, cz)) continue;
          LoadChunk(cx, cz);
          return;
        }
  }

  private void LoadChunk(int cx, int cz)
  {
    var chunk = new Chunk();
    _generator.Generate(chunk, cx, cz);

    for (int x = 0; x < Chunk.Width; x++)
      for (int y = 0; y < Chunk.Height; y++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
          if (chunk.GetBlock(x, y, z) == Block.Water)
            _activeWater.Add((cx * Chunk.Width + x, y, cz * Chunk.Depth + z));
        }

    _chunkLookup[(cx, cz)] = chunk;
    _loadedSet.Add((cx, cz));

    var mesh = new ChunkMesh();
    mesh.Build(chunk, GetBlock, cx, cz, GetWorldFluid);
    ChunkMeshes.Add((mesh, chunk, new Vector3i(cx, 0, cz)));

    // Nur Nachbarn neu bauen die Wasser an der Grenze haben
    RebuildNeighborsIfNeeded(cx, cz, chunk);
  }

  private void RebuildNeighborsIfNeeded(int cx, int cz, Chunk newChunk)
  {
    // Prüfe ob neue Chunk Wasser an den Grenzen hat
    bool hasWaterNorth = false, hasWaterSouth = false,
         hasWaterEast = false, hasWaterWest = false;

    for (int y = 0; y < Chunk.Height; y++)
    {
      for (int x = 0; x < Chunk.Width; x++)
      {
        if (newChunk.GetBlock(x, y, 0) == Block.Water) hasWaterNorth = true;
        if (newChunk.GetBlock(x, y, Chunk.Depth - 1) == Block.Water) hasWaterSouth = true;
      }
      for (int z = 0; z < Chunk.Depth; z++)
      {
        if (newChunk.GetBlock(0, y, z) == Block.Water) hasWaterWest = true;
        if (newChunk.GetBlock(Chunk.Width - 1, y, z) == Block.Water) hasWaterEast = true;
      }
      if (hasWaterNorth && hasWaterSouth && hasWaterEast && hasWaterWest) break;
    }

    int[] dx = [-1, 1, 0, 0];
    int[] dz = [0, 0, -1, 1];
    bool[] needed = [hasWaterWest, hasWaterEast, hasWaterNorth, hasWaterSouth];

    for (int i = 0; i < 4; i++)
    {
      if (!needed[i]) continue;
      int nx = cx + dx[i];
      int nz = cz + dz[i];
      if (!_chunkLookup.TryGetValue((nx, nz), out var neighborChunk)) continue;

      for (int j = 0; j < ChunkMeshes.Count; j++)
      {
        var (mesh, _, pos) = ChunkMeshes[j];
        if (pos.X != nx || pos.Z != nz) continue;
        var newMesh = new ChunkMesh();
        newMesh.Build(neighborChunk, GetBlock, nx, nz, GetWorldFluid);
        mesh.Dispose();
        ChunkMeshes[j] = (newMesh, neighborChunk, pos);
        break;
      }
    }
  }

  public void RebuildNeighbors(int cx, int cz)
  {
    int[] dx = [-1, 1, 0, 0];
    int[] dz = [0, 0, -1, 1];

    for (int i = 0; i < 4; i++)
    {
      int nx = cx + dx[i];
      int nz = cz + dz[i];
      if (!_chunkLookup.TryGetValue((nx, nz), out var neighborChunk)) continue;

      for (int j = 0; j < ChunkMeshes.Count; j++)
      {
        var (mesh, _, pos) = ChunkMeshes[j];
        if (pos.X != nx || pos.Z != nz) continue;
        var newMesh = new ChunkMesh();
        newMesh.Build(neighborChunk, GetBlock, nx, nz, GetWorldFluid);
        mesh.Dispose();
        ChunkMeshes[j] = (newMesh, neighborChunk, pos);
        break;
      }
    }
  }

  // ── Block Access ──────────────────────────────────────────────────────
  public Block? GetBlock(int wx, int wy, int wz)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);
    if (!_chunkLookup.TryGetValue((cx, cz), out var chunk)) return null;
    int lx = wx - cx * Chunk.Width;
    int lz = wz - cz * Chunk.Depth;
    if (!chunk.IsInside(lx, wy, lz)) return null;
    var block = chunk.GetBlock(lx, wy, lz);
    return block == Block.Air ? null : block;
  }

  public Block? GetSolidBlock(int wx, int wy, int wz)
  {
    var block = GetBlock(wx, wy, wz);
    if (block == null) return null;

    return BlockRegistry.Get(block.Value).IsSolid ? block : null;
  }

  public bool BreakBlock(Vector3i worldPos)
  {
    int cx = (int)MathF.Floor(worldPos.X / (float)Chunk.Width);
    int cz = (int)MathF.Floor(worldPos.Z / (float)Chunk.Depth);

    for (int i = 0; i < ChunkMeshes.Count; i++)
    {
      var (mesh, chunk, chunkPos) = ChunkMeshes[i];
      if (chunkPos.X != cx || chunkPos.Z != cz) continue;
      int lx = worldPos.X - cx * Chunk.Width;
      int lz = worldPos.Z - cz * Chunk.Depth;
      if (!chunk.IsInside(lx, worldPos.Y, lz)) return false;
      bool brokeWater = chunk.GetBlock(lx, worldPos.Y, lz) == Block.Water;
      chunk.SetBlock(lx, worldPos.Y, lz, Block.Air);
      if (brokeWater)
        chunk.SetFluidLevel(lx, worldPos.Y, lz, 255);
      _activeWater.Remove((worldPos.X, worldPos.Y, worldPos.Z));
      ScheduleWaterAround(worldPos);
      var newMesh = new ChunkMesh();
      newMesh.Build(chunk, GetBlock, cx, cz, GetWorldFluid);
      mesh.Dispose();
      ChunkMeshes[i] = (newMesh, chunk, chunkPos);
      return true;
    }
    return false;
  }

  public bool PlaceBlock(Vector3i worldPos, Block block, byte metadata = 0)
  {
    int cx = (int)MathF.Floor(worldPos.X / (float)Chunk.Width);
    int cz = (int)MathF.Floor(worldPos.Z / (float)Chunk.Depth);

    for (int i = 0; i < ChunkMeshes.Count; i++)
    {
      var (mesh, chunk, chunkPos) = ChunkMeshes[i];
      if (chunkPos.X != cx || chunkPos.Z != cz) continue;
      int lx = worldPos.X - cx * Chunk.Width;
      int lz = worldPos.Z - cz * Chunk.Depth;
      if (!chunk.IsInside(lx, worldPos.Y, lz)) return false;
      if (chunk.GetBlock(lx, worldPos.Y, lz) != Block.Air) return false;
      chunk.SetBlock(lx, worldPos.Y, lz, block);
      chunk.SetMetadata(lx, worldPos.Y, lz, metadata);
      if (block == Block.Water)
      {
        chunk.SetFluidLevel(lx, worldPos.Y, lz, 0);
        _activeWater.Add((worldPos.X, worldPos.Y, worldPos.Z));
        _waterSim.ScheduleUpdate(worldPos.X, worldPos.Y, worldPos.Z);
      }
      ScheduleWaterAround(worldPos);
      var newMesh = new ChunkMesh();
      newMesh.Build(chunk, GetBlock, cx, cz, GetWorldFluid);
      mesh.Dispose();
      ChunkMeshes[i] = (newMesh, chunk, chunkPos);
      return true;
    }
    return false;
  }

  // ── Fluid Access ──────────────────────────────────────────────────────
  public (Block block, byte level)? GetWorldFluid(int wx, int wy, int wz)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);
    if (!_chunkLookup.TryGetValue((cx, cz), out var chunk)) return null;
    int lx = wx - cx * Chunk.Width;
    int lz = wz - cz * Chunk.Depth;
    if (!chunk.IsInside(lx, wy, lz)) return null;
    return (chunk.GetBlock(lx, wy, lz), chunk.GetFluidLevel(lx, wy, lz));
  }

  public void SetWorldFluid(int wx, int wy, int wz, Block block, byte level)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);
    if (!_chunkLookup.TryGetValue((cx, cz), out var chunk)) return;
    int lx = wx - cx * Chunk.Width;
    int lz = wz - cz * Chunk.Depth;
    if (!chunk.IsInside(lx, wy, lz)) return;
    chunk.SetBlock(lx, wy, lz, block);
    chunk.SetFluidLevel(lx, wy, lz, level);
    _dirtyChunks.Add((cx, cz));
    if (block == Block.Water)
      _activeWater.Add((wx, wy, wz));
    else
      _activeWater.Remove((wx, wy, wz));
  }

  public void MarkDirty(int wx, int wy, int wz)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);
    _dirtyChunks.Add((cx, cz));
  }

  private void ScheduleWaterAround(Vector3i worldPos)
  {
    _waterSim.ScheduleNeighbors(worldPos.X, worldPos.Y, worldPos.Z);
    MarkDirty(worldPos.X, worldPos.Y, worldPos.Z);
    MarkDirty(worldPos.X - 1, worldPos.Y, worldPos.Z);
    MarkDirty(worldPos.X + 1, worldPos.Y, worldPos.Z);
    MarkDirty(worldPos.X, worldPos.Y, worldPos.Z - 1);
    MarkDirty(worldPos.X, worldPos.Y, worldPos.Z + 1);
  }

  // ── Water Tick ────────────────────────────────────────────────────────
  public void WaterTick()
  {
    _waterTickCounter++;
    if (_waterTickCounter < 5) return;
    _waterTickCounter = 0;

    _waterSim.Tick();
    RebuildDirtyChunks();
  }

  private void RebuildDirtyChunks()
  {
    int rebuilt = 0;
    var remaining = new List<(int cx, int cz)>();

    foreach (var (cx, cz) in _dirtyChunks)
    {
      if (rebuilt >= 2)
      {
        remaining.Add((cx, cz));
        continue;
      }

      if (!_chunkLookup.TryGetValue((cx, cz), out var chunk)) continue;
      for (int i = 0; i < ChunkMeshes.Count; i++)
      {
        var (mesh, _, pos) = ChunkMeshes[i];
        if (pos.X != cx || pos.Z != cz) continue;
        var newMesh = new ChunkMesh();
        newMesh.Build(chunk, GetBlock, cx, cz, GetWorldFluid);
        mesh.Dispose();
        ChunkMeshes[i] = (newMesh, chunk, pos);
        rebuilt++;
        break;
      }
    }

    _dirtyChunks.Clear();
    foreach (var r in remaining)
      _dirtyChunks.Add(r);
  }

  public void Dispose()
  {
    foreach (var (mesh, _, _) in ChunkMeshes)
      mesh.Dispose();
    _loadedSet.Clear();
    _chunkLookup.Clear();
    _activeWater.Clear();
    ChunkMeshes.Clear();
  }

  public byte GetMetadata(int wx, int wy, int wz)
  {
    int cx = (int)MathF.Floor(wx / (float)Chunk.Width);
    int cz = (int)MathF.Floor(wz / (float)Chunk.Depth);
    int lx = wx - cx * Chunk.Width;
    int lz = wz - cz * Chunk.Depth;

    if (!_chunkLookup.TryGetValue((cx, cz), out var chunk)) return 0;
    if (!chunk.IsInside(lx, wy, lz)) return 0;
    return chunk.GetMetadata(lx, wy, lz);
  }
}
