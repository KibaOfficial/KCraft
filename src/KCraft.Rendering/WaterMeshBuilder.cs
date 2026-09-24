// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using KCraft.World;

namespace KCraft.Rendering;

public static class WaterMeshBuilder
{
  private const float SourceWaterHeight = 0.875f;

  public static void AddFace(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    Chunk chunk,
    Func<int, int, int, (Block block, byte level)?>? getWorldFluid,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z,
    FaceDirection face)
  {
    float x0 = x, y0 = y, z0 = z;
    float x1 = x + 1f, z1 = z + 1f;

    byte level = chunk.GetFluidLevel(x, y, z);

    if (face == FaceDirection.Down)
      return;

    var above = GetFluidAt(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      x,
      y + 1,
      z);

    if (above is { block: Block.Water })
      return;

    float h00 = GetCornerWaterHeight(
      chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 0, 0);

    float h10 = GetCornerWaterHeight(
      chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 1, 0);

    float h11 = GetCornerWaterHeight(
      chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 1, 1);

    float h01 = GetCornerWaterHeight(
      chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 0, 1);

    string texName =
      level == 0 && face == FaceDirection.Up
        ? "water_still"
        : "water_flow";

    if (!facesByTexture.TryGetValue(texName, out var group))
    {
      group = (new List<float>(), new List<uint>(), 0);
      facesByTexture[texName] = group;
    }

    var verts = group.verts;
    var indices = group.indices;
    var offset = group.offset;

    float northBottom = GetSideBottom(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      x,
      y,
      z,
      FaceDirection.North,
      MathF.Max(h00, h10));

    float southBottom = GetSideBottom(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      x,
      y,
      z,
      FaceDirection.South,
      MathF.Max(h01, h11));

    float eastBottom = GetSideBottom(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      x,
      y,
      z,
      FaceDirection.East,
      MathF.Max(h10, h11));

    float westBottom = GetSideBottom(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      x,
      y,
      z,
      FaceDirection.West,
      MathF.Max(h00, h01));

    var (v0, v1, v2, v3) = face switch
    {
      FaceDirection.Up =>
        ((x0, y + h00, z0),
         (x1, y + h10, z0),
         (x1, y + h11, z1),
         (x0, y + h01, z1)),

      FaceDirection.North =>
        ((x0, y + northBottom, z0),
         (x1, y + northBottom, z0),
         (x1, y + h10, z0),
         (x0, y + h00, z0)),

      FaceDirection.South =>
        ((x1, y + southBottom, z1),
         (x0, y + southBottom, z1),
         (x0, y + h01, z1),
         (x1, y + h11, z1)),

      FaceDirection.East =>
        ((x1, y + eastBottom, z0),
         (x1, y + eastBottom, z1),
         (x1, y + h11, z1),
         (x1, y + h10, z0)),

      FaceDirection.West =>
        ((x0, y + westBottom, z1),
         (x0, y + westBottom, z0),
         (x0, y + h00, z0),
         (x0, y + h01, z1)),

      _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    if (face != FaceDirection.Up)
    {
      var neighbor = GetNeighborForFace(
        chunk,
        getWorldFluid,
        chunkX,
        chunkZ,
        x,
        y,
        z,
        face);

      if (neighbor is { block: Block.Water })
      {
        float neighborHeight = GetWaterHeight(neighbor.Value.level);

        float faceHeight = face switch
        {
          FaceDirection.North => MathF.Max(h00, h10),
          FaceDirection.South => MathF.Max(h01, h11),
          FaceDirection.East => MathF.Max(h10, h11),
          FaceDirection.West => MathF.Max(h00, h01),
          _ => 0f
        };

        if (neighborHeight >= faceHeight - 0.001f)
          return;
      }
    }

    float b = face == FaceDirection.Up ? 1.0f : 0.7f;

    verts.AddRange([
      v0.Item1, v0.Item2, v0.Item3, 0f, 0f, b
    ]);

    verts.AddRange([
      v1.Item1, v1.Item2, v1.Item3, 1f, 0f, b
    ]);

    verts.AddRange([
      v2.Item1, v2.Item2, v2.Item3, 1f, 1f, b
    ]);

    verts.AddRange([
      v3.Item1, v3.Item2, v3.Item3, 0f, 1f, b
    ]);

    indices.AddRange([
      offset,
      offset + 2,
      offset + 1,
      offset,
      offset + 3,
      offset + 2
    ]);

    offset += 4;

    facesByTexture[texName] = (verts, indices, offset);
  }

  private static (Block block, byte level)? GetNeighborForFace(
    Chunk chunk,
    Func<int, int, int, (Block block, byte level)?>? getWorldFluid,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z,
    FaceDirection face)
  {
    var (nx, ny, nz) = face switch
    {
      FaceDirection.North => (x, y, z - 1),
      FaceDirection.South => (x, y, z + 1),
      FaceDirection.East => (x + 1, y, z),
      FaceDirection.West => (x - 1, y, z),
      FaceDirection.Up => (x, y + 1, z),
      FaceDirection.Down => (x, y - 1, z),
      _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    return GetFluidAt(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      nx,
      ny,
      nz);
  }

  private static float GetSideBottom(
    Chunk chunk,
    Func<int, int, int, (Block block, byte level)?>? getWorldFluid,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z,
    FaceDirection face,
    float faceHeight)
  {
    var neighbor = GetNeighborForFace(
      chunk,
      getWorldFluid,
      chunkX,
      chunkZ,
      x,
      y,
      z,
      face);

    if (neighbor is not { block: Block.Water })
      return 0f;

    return MathF.Min(
      faceHeight,
      GetWaterHeight(neighbor.Value.level));
  }

  private static float GetCornerWaterHeight(
    Chunk chunk,
    Func<int, int, int, (Block block, byte level)?>? getWorldFluid,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z,
    int cornerX,
    int cornerZ)
  {
    int sx = cornerX == 0 ? -1 : 1;
    int sz = cornerZ == 0 ? -1 : 1;

    float total = 0f;
    int count = 0;

    AddSample(x, z);
    AddSample(x + sx, z);
    AddSample(x, z + sz);
    AddSample(x + sx, z + sz);

    return count == 0
      ? SourceWaterHeight
      : total / count;

    void AddSample(int sampleX, int sampleZ)
    {
      var sample = GetFluidAt(
        chunk,
        getWorldFluid,
        chunkX,
        chunkZ,
        sampleX,
        y,
        sampleZ);

      if (sample is not { block: Block.Water })
        return;

      var above = GetFluidAt(
        chunk,
        getWorldFluid,
        chunkX,
        chunkZ,
        sampleX,
        y + 1,
        sampleZ);

      if (above is { block: Block.Water })
      {
        total += 1f;
        count++;
        return;
      }

      total += GetWaterHeight(sample.Value.level);
      count++;
    }
  }

  private static (Block block, byte level)? GetFluidAt(
    Chunk chunk,
    Func<int, int, int, (Block block, byte level)?>? getWorldFluid,
    int chunkX,
    int chunkZ,
    int x,
    int y,
    int z)
  {
    if (chunk.IsInside(x, y, z))
    {
      return (
        chunk.GetBlock(x, y, z),
        chunk.GetFluidLevel(x, y, z));
    }

    if (getWorldFluid == null)
      return null;

    int wx = chunkX * Chunk.Width + x;
    int wz = chunkZ * Chunk.Depth + z;

    return getWorldFluid(wx, y, wz);
  }

  private static float GetWaterHeight(byte level)
    => level == 0
      ? SourceWaterHeight
      : MathF.Max(0.125f, 1.0f - level / 8.0f);
}