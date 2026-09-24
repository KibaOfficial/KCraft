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

          // Stairs — eigene Geometrie, kein Face-Loop
          if (def.IsStairs)
          {
            var facing = (BlockFacing)chunk.GetMetadata(x, y, z);
            ShapeMeshBuilder.AddStairFaces(
              _facesByTexture,
              x,
              y,
              z,
              facing,
              def.TextureTop,
              def.TextureSide,
              def.TextureBottom);
            continue; // ← überspringt den foreach
          }

          if (def.IsSlope)
          {
            var facing = (BlockFacing)chunk.GetMetadata(x, y, z);
            ShapeMeshBuilder.AddSlopeFaces(_facesByTexture, x, y, z, facing,
                def.TextureTop, def.TextureSide, def.TextureBottom);
            continue;
          }

          foreach (FaceDirection face in Enum.GetValues<FaceDirection>())
          {
            if (def.IsFluid)
            {
              WaterMeshBuilder.AddFace(
                waterFacesByTexture,
                chunk,
                getWorldFluid,
                chunkX,
                chunkZ,
                x,
                y,
                z,
                face);
              continue;
            }

            if (!FaceVisibility.IsVisible(chunk, x, y, z, face, getWorldBlock, chunkX, chunkZ)) continue;


            // CTM Blöcke
            if (def.UsesCTM && !string.IsNullOrEmpty(def.CTMTexture))
            {
              int tileIndex = ConnectedTextureResolver.GetTileIndex(
                chunk,
                getWorldBlock,
                chunkX,
                chunkZ,
                x,
                y,
                z,
                face,
                block);
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
      globalOffset += (uint)(verts.Count / 6);
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
      waterGlobalOffset += (uint)(verts.Count / 6);
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
    float b = FaceBrightness(face);

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

    verts.AddRange([v0.Item1, v0.Item2, v0.Item3, 0f, 0f, b]);
    verts.AddRange([v1.Item1, v1.Item2, v1.Item3, 1f, 0f, b]);
    verts.AddRange([v2.Item1, v2.Item2, v2.Item3, 1f, 1f, b]);
    verts.AddRange([v3.Item1, v3.Item2, v3.Item3, 0f, 1f, b]);
    indices.AddRange([offset, offset + 2, offset + 1, offset, offset + 3, offset + 2]);
    offset += 4;
  }

  private static float FaceBrightness(FaceDirection face) => face switch
  {
    FaceDirection.Up => 1.00f,
    FaceDirection.North => 0.80f,
    FaceDirection.South => 0.80f,
    FaceDirection.East => 0.60f,
    FaceDirection.West => 0.60f,
    FaceDirection.Down => 0.50f,
    _ => 1.0f
  };

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
    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
    GL.EnableVertexAttribArray(1);
    GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 6 * sizeof(float), 5 * sizeof(float));
    GL.EnableVertexAttribArray(2);
  }

  // ── Dispose ───────────────────────────────────────────────────────────
  public void Dispose()
  {
    if (_vao != 0) { GL.DeleteVertexArray(_vao); GL.DeleteBuffer(_vbo); GL.DeleteBuffer(_ebo); }
    if (_waterVao != 0) { GL.DeleteVertexArray(_waterVao); GL.DeleteBuffer(_waterVbo); GL.DeleteBuffer(_waterEbo); }
  }
}
