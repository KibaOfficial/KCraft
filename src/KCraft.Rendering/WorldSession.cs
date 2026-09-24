// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;

namespace KCraft.Rendering;

public sealed class WorldSession
{
  private Dictionary<(int cx, int cz), byte[]>? _pendingChunks;
  private Dictionary<(int cx, int cz), byte[]>? _pendingMetadata;

  public string CurrentWorldName { get; private set; } = "default";

  public WorldSaveData? LoadSave(
    string name,
    out Dictionary<(int cx, int cz), byte[]> chunks,
    out Dictionary<(int cx, int cz), byte[]> metadata)
  {
    CurrentWorldName = name;

    var result = WorldSaveManager.Load(name);

    chunks = result.chunks;
    metadata = result.metadata;

    return result.data;
  }

  public void SetPendingChunks(
    Dictionary<(int cx, int cz), byte[]> chunks,
    Dictionary<(int cx, int cz), byte[]> metadata)
  {
    _pendingChunks = chunks;
    _pendingMetadata = metadata;
  }

  public void ApplyPendingChunks(WorldManager world)
  {
    if (_pendingChunks == null)
      return;

    ApplyChunkData(
      world,
      _pendingChunks,
      _pendingMetadata);

    _pendingChunks = null;
    _pendingMetadata = null;
  }

  public void ApplyChunkData(
    WorldManager world,
    Dictionary<(int cx, int cz), byte[]> chunks,
    Dictionary<(int cx, int cz), byte[]>? metadata)
  {
    foreach (var ((cx, cz), rawData) in chunks)
    {
      for (int i = 0; i < world.ChunkMeshes.Count; i++)
      {
        var (_, chunk, chunkPos) = world.ChunkMeshes[i];

        if (chunkPos.X != cx || chunkPos.Z != cz)
          continue;

        chunk.LoadRawBlocks(rawData);

        if (metadata != null &&
            metadata.TryGetValue((cx, cz), out var metaData))
        {
          chunk.LoadRawMetadata(metaData);
        }

        break;
      }
    }

    RebuildMeshes(world);
  }

  public void Save(
    WorldManager world,
    WorldTicker ticker,
    Camera camera,
    PlayerInventory inventory,
    GameMode gameMode)
  {
    if (ticker.Player == null)
      return;

    var data = new WorldSaveData
    {
      WorldName = CurrentWorldName,
      Seed = world.Seed,
      PlayerX = ticker.Player.Position.X,
      PlayerY = ticker.Player.Position.Y,
      PlayerZ = ticker.Player.Position.Z,
      CameraYaw = camera.Yaw,
      CameraPitch = camera.Pitch,
      GameMode = (int)gameMode,
      TotalTicks = ticker.Time.TotalTicks,
      LastPlayed = DateTime.Now,
      InventorySlots = inventory.GetRawSlots(),
      SelectedHotbarSlot = inventory.SelectedHotbarSlot,
    };

    var chunks = world.ChunkMeshes
      .Select(c => (
        c.chunk,
        c.chunkPos.X,
        c.chunkPos.Z));

    WorldSaveManager.Save(
      CurrentWorldName,
      data,
      chunks);
  }

  private static void RebuildMeshes(WorldManager world)
  {
    for (int i = 0; i < world.ChunkMeshes.Count; i++)
    {
      var (mesh, chunk, chunkPos) = world.ChunkMeshes[i];

      var newMesh = new ChunkMesh();

      newMesh.Build(
        chunk,
        world.GetBlock,
        chunkPos.X,
        chunkPos.Z,
        world.GetWorldFluid);

      mesh.Dispose();

      world.ChunkMeshes[i] = (
        newMesh,
        chunk,
        chunkPos);
    }
  }

  public void SetCurrentWorld(string name)
  {
    CurrentWorldName = name;
  }
}