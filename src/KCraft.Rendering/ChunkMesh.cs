// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;

namespace KCraft.Rendering;

public sealed class ChunkMesh : IDisposable
{
  // ── Solid Geometry ────────────────────────────────────────────────────
  private int _vao, _vbo, _ebo;
  private readonly List<(string texName, int startIndex, int count)> _subMeshes = new();

  // ── Water Geometry ────────────────────────────────────────────────────
  private int _waterVao, _waterVbo, _waterEbo;
  private int _waterIndexCount;
  private bool _hasWater;
  private readonly List<(string texName, int startIndex, int count)> _waterSubMeshes = new();

  // ── Water Tint (MC Plains: #3F76E4) ──────────────────────────────────
  private static readonly (float r, float g, float b) WaterTint =
      (0x3F / 255f, 0x76 / 255f, 0xE4 / 255f);
  private const float SourceWaterHeight = 0.875f;

  private readonly Dictionary<string, (List<float> verts, List<uint> indices, uint offset)>
      _facesByTexture = new();

  // ── Build ─────────────────────────────────────────────────────────────
  public void Build(Chunk chunk, Func<int, int, int, Block?>? getWorldBlock = null,
      int chunkX = 0, int chunkZ = 0,
      Func<int, int, int, (Block block, byte level)?>? getWorldFluid = null)
  {
    _facesByTexture.Clear();
    var waterFacesByTexture = new Dictionary<string, (List<float> verts, List<uint> indices, uint offset)>();

    for (int x = 0; x < Chunk.Width; x++)
      for (int y = 0; y < Chunk.Height; y++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
          var block = chunk.GetBlock(x, y, z);
          if (block == Block.Air) continue;

          var def = BlockRegistry.Definitions.TryGetValue(block, out var d)
              ? d : new BlockDefinition();

          foreach (FaceDirection face in Enum.GetValues<FaceDirection>())
          {
            if (def.IsFluid)
            {
              AddWaterFace(waterFacesByTexture, chunk, getWorldFluid, chunkX, chunkZ, x, y, z, face);
              continue;
            }

            if (!FaceVisibility.IsVisible(chunk, x, y, z, face, getWorldBlock, chunkX, chunkZ)) continue;


            // CTM Blöcke
            if (def.UsesCTM && !string.IsNullOrEmpty(def.CTMTexture))
            {
              int tileIndex = GetCTMTileIndex(chunk, getWorldBlock, chunkX, chunkZ, x, y, z, face, block);
              string ctmTexName = $"CTM:{def.CTMTexture}:{tileIndex}";

              if (!_facesByTexture.TryGetValue(ctmTexName, out var ctmGroup))
              {
                ctmGroup = (new List<float>(), new List<uint>(), 0);
                _facesByTexture[ctmTexName] = ctmGroup;
              }

              var cv = ctmGroup.verts;
              var ci = ctmGroup.indices;
              var co = ctmGroup.offset;
              AddFace(cv, ci, ref co, x, y, z, face);
              _facesByTexture[ctmTexName] = (cv, ci, co);
              continue; // ← wichtig, nicht weiter zu normalem texName
            }

            string texName = face switch
            {
              FaceDirection.Up => def.TextureTop,
              FaceDirection.Down => def.TextureBottom,
              _ => def.TextureSide,
            };

            if (!_facesByTexture.TryGetValue(texName, out var group))
            {
              group = (new List<float>(), new List<uint>(), 0);
              _facesByTexture[texName] = group;
            }

            var verts = group.verts;
            var inds = group.indices;
            var offset = group.offset;
            AddFace(verts, inds, ref offset, x, y, z, face);
            _facesByTexture[texName] = (verts, inds, offset);
          }
        }

    // Solid zusammenmergen + Upload
    var allVerts = new List<float>();
    var allIndices = new List<uint>();
    uint globalOffset = 0;

    _subMeshes.Clear();
    foreach (var (texName, (verts, inds, _)) in _facesByTexture)
    {
      int startIndex = allIndices.Count;
      allVerts.AddRange(verts);
      foreach (var i in inds)
        allIndices.Add(i + globalOffset);
      globalOffset += (uint)(verts.Count / 5);
      _subMeshes.Add((texName, startIndex, inds.Count));
    }
    UploadSolid(allVerts, allIndices);

    var allWaterVerts = new List<float>();
    var allWaterIndices = new List<uint>();
    uint waterGlobalOffset = 0;

    _waterSubMeshes.Clear();
    foreach (var (texName, (verts, inds, _)) in waterFacesByTexture)
    {
      int startIndex = allWaterIndices.Count;
      allWaterVerts.AddRange(verts);
      foreach (var i in inds)
        allWaterIndices.Add(i + waterGlobalOffset);
      waterGlobalOffset += (uint)(verts.Count / 5);
      _waterSubMeshes.Add((texName, startIndex, inds.Count));
    }

    _hasWater = allWaterVerts.Count > 0;
    _waterIndexCount = allWaterIndices.Count;
    if (_hasWater) UploadWater(allWaterVerts, allWaterIndices);
  }

  // ── Draw Solid ────────────────────────────────────────────────────────
  public void Draw(TextureManager textures, int uTexLocation, int uTintLocation)
  {
    if (_subMeshes.Count == 0) return;
    GL.BindVertexArray(_vao);
    foreach (var (texName, startIndex, count) in _subMeshes)
    {
      // CTM Tile
      if (texName.StartsWith("CTM:"))
      {
        var parts = texName.Split(':');
        var ctmName = parts[1]; // "glass_ctm"
        var tileIndex = int.Parse(parts[2]); // "0"
        textures.GetCTMTile(ctmName, tileIndex).Bind();
      }
      else
      {
        textures.Get(texName).Bind();
      }

      GL.Uniform1(uTexLocation, 0);

      if (texName == "grass_block_top")
        GL.Uniform3(uTintLocation, 0.48f, 0.74f, 0.36f);
      else if (texName == "oak_leaves")
        GL.Uniform3(uTintLocation, 0.38f, 0.62f, 0.25f);
      else
        GL.Uniform3(uTintLocation, 1.0f, 1.0f, 1.0f);

      GL.DrawElements(PrimitiveType.Triangles, count,
          DrawElementsType.UnsignedInt, startIndex * sizeof(uint));
    }
  }

  // ── Draw Water ────────────────────────────────────────────────────────
  public void DrawWater(TextureManager textures, int uTexLocation, int uTintLocation)
  {
    if (!_hasWater || _waterIndexCount == 0) return;
    GL.Uniform3(uTintLocation, WaterTint.r, WaterTint.g, WaterTint.b);
    GL.BindVertexArray(_waterVao);
    foreach (var (texName, startIndex, count) in _waterSubMeshes)
    {
      textures.Get(texName, 0).Bind();
      GL.Uniform1(uTexLocation, 0);
      GL.DrawElements(PrimitiveType.Triangles, count,
          DrawElementsType.UnsignedInt, startIndex * sizeof(uint));
    }
  }

  // ── Solid Face ────────────────────────────────────────────────────────
  private static void AddFace(List<float> verts, List<uint> indices,
      ref uint offset, int x, int y, int z, FaceDirection face)
  {
    float x0 = x, y0 = y, z0 = z;
    float x1 = x + 1f, y1 = y + 1f, z1 = z + 1f;

    var (v0, v1, v2, v3) = face switch
    {
      FaceDirection.North => ((x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0)),
      FaceDirection.South => ((x1, y0, z1), (x0, y0, z1), (x0, y1, z1), (x1, y1, z1)),
      FaceDirection.East => ((x1, y0, z0), (x1, y0, z1), (x1, y1, z1), (x1, y1, z0)),
      FaceDirection.West => ((x0, y0, z1), (x0, y0, z0), (x0, y1, z0), (x0, y1, z1)),
      FaceDirection.Up => ((x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)),
      FaceDirection.Down => ((x0, y0, z1), (x1, y0, z1), (x1, y0, z0), (x0, y0, z0)),
      _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    verts.AddRange([v0.Item1, v0.Item2, v0.Item3, 0f, 0f]);
    verts.AddRange([v1.Item1, v1.Item2, v1.Item3, 1f, 0f]);
    verts.AddRange([v2.Item1, v2.Item2, v2.Item3, 1f, 1f]);
    verts.AddRange([v3.Item1, v3.Item2, v3.Item3, 0f, 1f]);
    indices.AddRange([offset, offset + 2, offset + 1, offset, offset + 3, offset + 2]);
    offset += 4;
  }

  // ── Water Face ────────────────────────────────────────────────────────
  private static void AddWaterFace(
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
    if (face == FaceDirection.Down) return;

    var above = GetFluidAt(chunk, getWorldFluid, chunkX, chunkZ, x, y + 1, z);
    if (above is { block: Block.Water }) return;

    float h00 = GetCornerWaterHeight(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 0, 0);
    float h10 = GetCornerWaterHeight(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 1, 0);
    float h11 = GetCornerWaterHeight(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 1, 1);
    float h01 = GetCornerWaterHeight(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, 0, 1);
    string texName = level == 0 && face == FaceDirection.Up ? "water_still" : "water_flow";

    if (!facesByTexture.TryGetValue(texName, out var group))
    {
      group = (new List<float>(), new List<uint>(), 0);
      facesByTexture[texName] = group;
    }

    var verts = group.verts;
    var indices = group.indices;
    var offset = group.offset;

    float northBottom = GetSideBottom(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, FaceDirection.North, MathF.Max(h00, h10));
    float southBottom = GetSideBottom(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, FaceDirection.South, MathF.Max(h01, h11));
    float eastBottom = GetSideBottom(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, FaceDirection.East, MathF.Max(h10, h11));
    float westBottom = GetSideBottom(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, FaceDirection.West, MathF.Max(h00, h01));

    var (v0, v1, v2, v3) = face switch
    {
      FaceDirection.Up => ((x0, y + h00, z0), (x1, y + h10, z0), (x1, y + h11, z1), (x0, y + h01, z1)),
      FaceDirection.North => ((x0, y + northBottom, z0), (x1, y + northBottom, z0), (x1, y + h10, z0), (x0, y + h00, z0)),
      FaceDirection.South => ((x1, y + southBottom, z1), (x0, y + southBottom, z1), (x0, y + h01, z1), (x1, y + h11, z1)),
      FaceDirection.East => ((x1, y + eastBottom, z0), (x1, y + eastBottom, z1), (x1, y + h11, z1), (x1, y + h10, z0)),
      FaceDirection.West => ((x0, y + westBottom, z1), (x0, y + westBottom, z0), (x0, y + h00, z0), (x0, y + h01, z1)),
      _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    if (face != FaceDirection.Up)
    {
      var neighbor = GetNeighborForFace(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, face);
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

    verts.AddRange([v0.Item1, v0.Item2, v0.Item3, 0f, 0f]);
    verts.AddRange([v1.Item1, v1.Item2, v1.Item3, 1f, 0f]);
    verts.AddRange([v2.Item1, v2.Item2, v2.Item3, 1f, 1f]);
    verts.AddRange([v3.Item1, v3.Item2, v3.Item3, 0f, 1f]);
    indices.AddRange([offset, offset + 2, offset + 1, offset, offset + 3, offset + 2]);
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

    return GetFluidAt(chunk, getWorldFluid, chunkX, chunkZ, nx, ny, nz);
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
    var neighbor = GetNeighborForFace(chunk, getWorldFluid, chunkX, chunkZ, x, y, z, face);
    if (neighbor is not { block: Block.Water })
      return 0f;

    return MathF.Min(faceHeight, GetWaterHeight(neighbor.Value.level));
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

    return count == 0 ? SourceWaterHeight : total / count;

    void AddSample(int sampleX, int sampleZ)
    {
      var sample = GetFluidAt(chunk, getWorldFluid, chunkX, chunkZ, sampleX, y, sampleZ);
      if (sample is not { block: Block.Water }) return;

      var above = GetFluidAt(chunk, getWorldFluid, chunkX, chunkZ, sampleX, y + 1, sampleZ);
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
      return (chunk.GetBlock(x, y, z), chunk.GetFluidLevel(x, y, z));

    if (getWorldFluid == null) return null;

    int wx = chunkX * Chunk.Width + x;
    int wz = chunkZ * Chunk.Depth + z;
    return getWorldFluid(wx, y, wz);
  }

  private static float GetWaterHeight(byte level)
    => level == 0 ? SourceWaterHeight : MathF.Max(0.125f, 1.0f - level / 8.0f);

  // ── Upload ────────────────────────────────────────────────────────────
  private void UploadSolid(List<float> vertices, List<uint> indices)
  {
    if (_vao == 0) { _vao = GL.GenVertexArray(); _vbo = GL.GenBuffer(); _ebo = GL.GenBuffer(); }
    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
        vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.DynamicDraw);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
    GL.BufferData(BufferTarget.ElementArrayBuffer,
        indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.DynamicDraw);
    SetupAttribs();
    GL.BindVertexArray(0);
  }

  private void UploadWater(List<float> vertices, List<uint> indices)
  {
    if (_waterVao == 0) { _waterVao = GL.GenVertexArray(); _waterVbo = GL.GenBuffer(); _waterEbo = GL.GenBuffer(); }
    GL.BindVertexArray(_waterVao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _waterVbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
        vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.DynamicDraw);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _waterEbo);
    GL.BufferData(BufferTarget.ElementArrayBuffer,
        indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.DynamicDraw);
    SetupAttribs();
    GL.BindVertexArray(0);
  }

  private static void SetupAttribs()
  {
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
    GL.EnableVertexAttribArray(1);
  }

  // ── Dispose ───────────────────────────────────────────────────────────
  public void Dispose()
  {
    if (_vao != 0) { GL.DeleteVertexArray(_vao); GL.DeleteBuffer(_vbo); GL.DeleteBuffer(_ebo); }
    if (_waterVao != 0) { GL.DeleteVertexArray(_waterVao); GL.DeleteBuffer(_waterVbo); GL.DeleteBuffer(_waterEbo); }
  }

  // ── Connected Texture Method (CTM) ───────────────────────────────────────────────────────────
  private static int GetCTMTileIndex(Chunk chunk, Func<int, int, int, Block?>? getWorldBlock,
    int chunkX, int chunkZ, int x, int y, int z, FaceDirection face, Block block)
  {
    bool left, right, up, down;

    switch (face)
    {
      case FaceDirection.North:
        left = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        right = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        up = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;
      case FaceDirection.South:
        left = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        right = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        up = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;
      case FaceDirection.East:
        left = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        right = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        up = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;
      case FaceDirection.West:
        left = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        right = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        up = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y + 1, z, block);
        down = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y - 1, z, block);
        break;
      case FaceDirection.Up:
        left = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        right = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        up = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        down = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        break;
      default: // Down
        left = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x - 1, y, z, block);
        right = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x + 1, y, z, block);
        up = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z - 1, block);
        down = GetNeighbor(chunk, getWorldBlock, chunkX, chunkZ, x, y, z + 1, block);
        break;
    }

    int index = 0;
    if (left) index |= 1;
    if (right) index |= 2;
    if (up) index |= 4;
    if (down) index |= 8;
    return index;
  }

  private static bool GetNeighbor(Chunk chunk, Func<int, int, int, Block?>? getWorldBlock,
      int chunkX, int chunkZ, int x, int y, int z, Block sameBlock)
  {
    if (chunk.IsInside(x, y, z))
      return chunk.GetBlock(x, y, z) == sameBlock;

    if (getWorldBlock == null) return false;
    int wx = chunkX * Chunk.Width + x;
    int wz = chunkZ * Chunk.Depth + z;
    return getWorldBlock(wx, y, wz) == sameBlock;
  }
}
