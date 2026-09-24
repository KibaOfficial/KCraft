// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;
using KCraft.World;

namespace KCraft.Rendering;

public static class ShapeMeshBuilder
{
  public static void AddStairFaces(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    int x,
    int y,
    int z,
    BlockFacing facing,
    string texTop,
    string texSide,
    string texBottom)
  {
    // Bottom half slab: full 1x0.5x1
    AddBox(
      facesByTexture,
      x,
      y,
      z,
      0f,
      0f,
      0f,
      1f,
      0.5f,
      1f,
      texTop,
      texSide,
      texBottom,
      facing);

    // Upper back half depending on facing
    switch (facing)
    {
      case BlockFacing.North:
        AddBox(
          facesByTexture,
          x,
          y,
          z,
          0f,
          0.5f,
          0f,
          1f,
          1f,
          0.5f,
          texTop,
          texSide,
          texBottom,
          facing);
        break;

      case BlockFacing.South:
        AddBox(
          facesByTexture,
          x,
          y,
          z,
          0f,
          0.5f,
          0.5f,
          1f,
          1f,
          1f,
          texTop,
          texSide,
          texBottom,
          facing);
        break;

      case BlockFacing.West:
        AddBox(
          facesByTexture,
          x,
          y,
          z,
          0f,
          0.5f,
          0f,
          0.5f,
          1f,
          1f,
          texTop,
          texSide,
          texBottom,
          facing);
        break;

      case BlockFacing.East:
        AddBox(
          facesByTexture,
          x,
          y,
          z,
          0.5f,
          0.5f,
          0f,
          1f,
          1f,
          1f,
          texTop,
          texSide,
          texBottom,
          facing);
        break;
    }
  }

  public static void AddSlopeFaces(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    int x,
    int y,
    int z,
    BlockFacing facing,
    string texTop,
    string texSide,
    string texBottom)
  {
    // Lokaler Basis-Keil:
    // North = hohe Seite bei z=0, niedrige Seite bei z=1
    var p000 = RotateSlopePoint(0f, 0f, 0f, facing);
    var p100 = RotateSlopePoint(1f, 0f, 0f, facing);
    var p101 = RotateSlopePoint(1f, 0f, 1f, facing);
    var p001 = RotateSlopePoint(0f, 0f, 1f, facing);

    var p010 = RotateSlopePoint(0f, 1f, 0f, facing);
    var p110 = RotateSlopePoint(1f, 1f, 0f, facing);

    (float x, float y, float z) W((float x, float y, float z) p)
      => (x + p.x, y + p.y, z + p.z);

    // Bottom
    AddRawFace(
      facesByTexture,
      texBottom,
      FaceDirection.Down,
      W(p001),
      W(p101),
      W(p100),
      W(p000));

    // High vertical back face
    AddRawFace(
      facesByTexture,
      texSide,
      FaceDirection.North,
      W(p000),
      W(p100),
      W(p110),
      W(p010));

    // Left triangle
    AddTriFace(
      facesByTexture,
      texSide,
      W(p010),
      W(p000),
      W(p001));

    // Right triangle
    AddTriFace(
      facesByTexture,
      texSide,
      W(p100),
      W(p110),
      W(p101));

    // Sloped top
    AddRawFace(
      facesByTexture,
      texTop,
      FaceDirection.Up,
      W(p010),
      W(p110),
      W(p101),
      W(p001));
  }

  private static void AddBox(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    int bx,
    int by,
    int bz,
    float x0,
    float y0,
    float z0,
    float x1,
    float y1,
    float z1,
    string texTop,
    string texSide,
    string texBottom,
    BlockFacing stairFacing)
  {
    float ax0 = bx + x0;
    float ay0 = by + y0;
    float az0 = bz + z0;

    float ax1 = bx + x1;
    float ay1 = by + y1;
    float az1 = bz + z1;

    // Down: X/Z
    AddBoxFace(
      facesByTexture,
      texBottom,
      FaceDirection.Down,
      (ax0, ay0, az1),
      (ax1, ay0, az1),
      (ax1, ay0, az0),
      (ax0, ay0, az0),
      stairFacing);

    // Up: X/Z
    AddBoxFace(
      facesByTexture,
      texTop,
      FaceDirection.Up,
      (ax0, ay1, az0),
      (ax1, ay1, az0),
      (ax1, ay1, az1),
      (ax0, ay1, az1),
      stairFacing);

    // North
    AddBoxFace(
      facesByTexture,
      texSide,
      FaceDirection.North,
      (ax0, ay0, az0),
      (ax1, ay0, az0),
      (ax1, ay1, az0),
      (ax0, ay1, az0),
      stairFacing);

    // South
    AddBoxFace(
      facesByTexture,
      texSide,
      FaceDirection.South,
      (ax1, ay0, az1),
      (ax0, ay0, az1),
      (ax0, ay1, az1),
      (ax1, ay1, az1),
      stairFacing);

    // West
    AddBoxFace(
      facesByTexture,
      texSide,
      FaceDirection.West,
      (ax0, ay0, az1),
      (ax0, ay0, az0),
      (ax0, ay1, az0),
      (ax0, ay1, az1),
      stairFacing);

    // East
    AddBoxFace(
      facesByTexture,
      texSide,
      FaceDirection.East,
      (ax1, ay0, az0),
      (ax1, ay0, az1),
      (ax1, ay1, az1),
      (ax1, ay1, az0),
      stairFacing);
  }

  private static void AddBoxFace(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    string texName,
    FaceDirection face,
    (float x, float y, float z) v0,
    (float x, float y, float z) v1,
    (float x, float y, float z) v2,
    (float x, float y, float z) v3,
    BlockFacing stairFacing)
  {
    if (!facesByTexture.TryGetValue(texName, out var group))
    {
      group = (new List<float>(), new List<uint>(), 0);
      facesByTexture[texName] = group;
    }

    var verts = group.verts;
    var indices = group.indices;
    var offset = group.offset;

    var uv0 = GetBoxUv(face, v0, stairFacing);
    var uv1 = GetBoxUv(face, v1, stairFacing);
    var uv2 = GetBoxUv(face, v2, stairFacing);
    var uv3 = GetBoxUv(face, v3, stairFacing);

    float b = FaceBrightness(face);

    verts.AddRange([v0.x, v0.y, v0.z, uv0.u, uv0.v, b]);
    verts.AddRange([v1.x, v1.y, v1.z, uv1.u, uv1.v, b]);
    verts.AddRange([v2.x, v2.y, v2.z, uv2.u, uv2.v, b]);
    verts.AddRange([v3.x, v3.y, v3.z, uv3.u, uv3.v, b]);

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

  private static (float u, float v) GetBoxUv(
    FaceDirection face,
    (float x, float y, float z) p,
    BlockFacing stairFacing)
  {
    if (face is FaceDirection.Up or FaceDirection.Down)
    {
      return stairFacing switch
      {
        BlockFacing.North => (p.x, p.z),
        BlockFacing.West => (p.z, p.x),
        BlockFacing.South => (1f - p.x, 1f - p.z),
        BlockFacing.East => (1f - p.z, 1f - p.x),
        _ => (p.x, p.z)
      };
    }

    return face switch
    {
      FaceDirection.North or FaceDirection.South => (p.x, p.y),
      FaceDirection.East or FaceDirection.West => (p.z, p.y),
      _ => (p.x, p.y),
    };
  }

  private static void AddTriFace(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    string texName,
    (float x, float y, float z) v0,
    (float x, float y, float z) v1,
    (float x, float y, float z) v2)
  {
    if (!facesByTexture.TryGetValue(texName, out var group))
    {
      group = (new List<float>(), new List<uint>(), 0);
      facesByTexture[texName] = group;
    }

    var verts = group.verts;
    var indices = group.indices;
    var offset = group.offset;

    float b = 0.8f;

    var uv0 = GetTriSideUv(v0);
    var uv1 = GetTriSideUv(v1);
    var uv2 = GetTriSideUv(v2);

    verts.AddRange([v0.x, v0.y, v0.z, uv0.u, uv0.v, b]);
    verts.AddRange([v1.x, v1.y, v1.z, uv1.u, uv1.v, b]);
    verts.AddRange([v2.x, v2.y, v2.z, uv2.u, uv2.v, b]);

    indices.AddRange([offset, offset + 1, offset + 2]);
    offset += 3;

    facesByTexture[texName] = (verts, indices, offset);
  }

  private static (float u, float v) GetTriSideUv(
    (float x, float y, float z) p)
  {
    // Nimm X oder Z als horizontale Achse, je nachdem welche stärker variiert.
    // Für Slope-Seiten reicht als erster Fix:
    return (p.z + p.x, p.y);
  }

  private static void AddRawFace(
    Dictionary<string, (List<float> verts, List<uint> indices, uint offset)> facesByTexture,
    string texName,
    FaceDirection face,
    (float x, float y, float z) v0,
    (float x, float y, float z) v1,
    (float x, float y, float z) v2,
    (float x, float y, float z) v3)
  {
    if (!facesByTexture.TryGetValue(texName, out var group))
    {
      group = (new List<float>(), new List<uint>(), 0);
      facesByTexture[texName] = group;
    }

    var verts = group.verts;
    var indices = group.indices;
    var offset = group.offset;

    float b = FaceBrightness(face);

    verts.AddRange([v0.x, v0.y, v0.z, 0f, 0f, b]);
    verts.AddRange([v1.x, v1.y, v1.z, 1f, 0f, b]);
    verts.AddRange([v2.x, v2.y, v2.z, 1f, 1f, b]);
    verts.AddRange([v3.x, v3.y, v3.z, 0f, 1f, b]);

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

  private static (float x, float y, float z) RotateSlopePoint(
    float x,
    float y,
    float z,
    BlockFacing facing)
  {
    return facing switch
    {
      BlockFacing.North => (x, y, z),
      BlockFacing.South => (1f - x, y, 1f - z),
      BlockFacing.East => (1f - z, y, x),
      BlockFacing.West => (z, y, 1f - x),
      _ => (x, y, z),
    };
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
}