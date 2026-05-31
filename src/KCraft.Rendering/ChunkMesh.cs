// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Blocks;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;

namespace KCraft.Rendering;

public sealed class ChunkMesh : IDisposable
{
  private int _vao, _vbo, _ebo;
  private int _indexCount;
  private readonly Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> _facesByTexture = new();
  private readonly List<(string texName, int startIndex, int count)> _subMeshes = new();


  public void Build(Chunk chunk)
  {
    _facesByTexture.Clear();
    
    for (int x = 0; x < Chunk.Width;  x++)
    for (int y = 0; y < Chunk.Height; y++)
    for (int z = 0; z < Chunk.Depth;  z++)
    {
      var block = chunk.GetBlock(x, y, z);
      if (block == Block.Air) continue;

      var def = BlockRegistry.Definitions.TryGetValue(block, out var d) ? d : new BlockDefinition();

      foreach (FaceDirection face in Enum.GetValues<FaceDirection>())
      {
        if (!FaceVisibility.IsVisible(chunk, x, y, z, face)) continue;

        string texName = face switch
        {
          FaceDirection.Up    => def.TextureTop,
          FaceDirection.Down => def.TextureBottom,
          _                    => def.TextureSide,
        };

        if (!_facesByTexture.TryGetValue(texName, out var group))
        {
          group = (new List<float>(), new List<uint>(), 0);
          _facesByTexture[texName] = group;
        }

        var verts  = group.verts;
        var inds   = group.indices;
        var offset = group.offset;
        AddFace(verts, inds, ref offset, x, y, z, face);
        _facesByTexture[texName] = (verts, inds, offset);
      }
    }

    // Alles zusammenmergen für Upload
    var allVerts   = new List<float>();
    var allIndices = new List<uint>();
    uint globalOffset = 0;

    _subMeshes.Clear();
    foreach (var (texName, (verts, inds, _)) in _facesByTexture)
    {
      int startIndex = allIndices.Count;
      allVerts.AddRange(verts);
      // Indices mit globalem Offset anpassen
      foreach (var i in inds)
        allIndices.Add(i + globalOffset);
      globalOffset += (uint)(verts.Count / 5);
      _subMeshes.Add((texName, startIndex, inds.Count));
    }

    Upload(allVerts, allIndices);
    _indexCount = allIndices.Count;
  }
  public void Draw(TextureManager textures, int uTexLocation, int uTintLocation)
  {
    if (_subMeshes.Count == 0) return;
    GL.BindVertexArray(_vao);
    foreach (var (texName, startIndex, count) in _subMeshes)
    {
      textures.Get(texName).Bind();
      GL.Uniform1(uTexLocation, 0);

      // Grass Top bekommt grünen Tint
      if (texName == "grass_block_top")
        GL.Uniform3(uTintLocation, 0.48f, 0.74f, 0.36f);
      else
        GL.Uniform3(uTintLocation, 1.0f, 1.0f, 1.0f);

      GL.DrawElements(PrimitiveType.Triangles, count,
        DrawElementsType.UnsignedInt, startIndex * sizeof(uint));
    }
  }

  private static void AddFace(List<float> verts, List<uint> indices,
    ref uint offset, int x, int y, int z, FaceDirection face)
  {
    var (v0, v1, v2, v3) = GetFaceVertices(x, y, z, face);

    verts.AddRange([v0.x, v0.y, v0.z, 0.0f, 0.0f]);
    verts.AddRange([v1.x, v1.y, v1.z, 1.0f, 0.0f]);
    verts.AddRange([v2.x, v2.y, v2.z, 1.0f, 1.0f]);
    verts.AddRange([v3.x, v3.y, v3.z, 0.0f, 1.0f]);

    indices.AddRange([offset, offset+2, offset+1, offset, offset+3, offset+2]);
    offset += 4;
  }

  private static ((float x,float y,float z) v0, (float x,float y,float z) v1,
                   (float x,float y,float z) v2, (float x,float y,float z) v3)
    GetFaceVertices(int x, int y, int z, FaceDirection face)
  {
    float x0 = x, y0 = y, z0 = z;
    float x1 = x+1, y1 = y+1, z1 = z+1;

    return face switch
    {
      FaceDirection.North => ((x0,y0,z0), (x1,y0,z0), (x1,y1,z0), (x0,y1,z0)),
      FaceDirection.South => ((x1,y0,z1), (x0,y0,z1), (x0,y1,z1), (x1,y1,z1)),
      FaceDirection.East  => ((x1,y0,z0), (x1,y0,z1), (x1,y1,z1), (x1,y1,z0)),
      FaceDirection.West  => ((x0,y0,z1), (x0,y0,z0), (x0,y1,z0), (x0,y1,z1)),
      FaceDirection.Up    => ((x0,y1,z0), (x1,y1,z0), (x1,y1,z1), (x0,y1,z1)),
      FaceDirection.Down  => ((x0,y0,z1), (x1,y0,z1), (x1,y0,z0), (x0,y0,z0)),
      _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
    };
  }

  private void Upload(List<float> vertices, List<uint> indices)
  {
    if (_vao == 0)
    {
      _vao = GL.GenVertexArray();
      _vbo = GL.GenBuffer();
      _ebo = GL.GenBuffer();
    }

    GL.BindVertexArray(_vao);

    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
      vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.DynamicDraw);

    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
    GL.BufferData(BufferTarget.ElementArrayBuffer,
      indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.DynamicDraw);

    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
    GL.EnableVertexAttribArray(1);

    GL.BindVertexArray(0);
  }

  public void Dispose()
  {
    GL.DeleteVertexArray(_vao);
    GL.DeleteBuffer(_vbo);
    GL.DeleteBuffer(_ebo);
  }
}