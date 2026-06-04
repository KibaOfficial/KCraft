// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class FrustumCuller
{
  private readonly Vector4[] _planes = new Vector4[6];

  // Frustum aus View-Projection Matrix extrahieren
  public void Update(Matrix4 viewProjection)
  {
    // Left
    _planes[0] = new Vector4(
        viewProjection.M14 + viewProjection.M11,
        viewProjection.M24 + viewProjection.M21,
        viewProjection.M34 + viewProjection.M31,
        viewProjection.M44 + viewProjection.M41);

    // Right
    _planes[1] = new Vector4(
        viewProjection.M14 - viewProjection.M11,
        viewProjection.M24 - viewProjection.M21,
        viewProjection.M34 - viewProjection.M31,
        viewProjection.M44 - viewProjection.M41);

    // Bottom
    _planes[2] = new Vector4(
        viewProjection.M14 + viewProjection.M12,
        viewProjection.M24 + viewProjection.M22,
        viewProjection.M34 + viewProjection.M32,
        viewProjection.M44 + viewProjection.M42);

    // Top
    _planes[3] = new Vector4(
        viewProjection.M14 - viewProjection.M12,
        viewProjection.M24 - viewProjection.M22,
        viewProjection.M34 - viewProjection.M32,
        viewProjection.M44 - viewProjection.M42);

    // Near
    _planes[4] = new Vector4(
        viewProjection.M14 + viewProjection.M13,
        viewProjection.M24 + viewProjection.M23,
        viewProjection.M34 + viewProjection.M33,
        viewProjection.M44 + viewProjection.M43);

    // Far
    _planes[5] = new Vector4(
        viewProjection.M14 - viewProjection.M13,
        viewProjection.M24 - viewProjection.M23,
        viewProjection.M34 - viewProjection.M33,
        viewProjection.M44 - viewProjection.M43);

    // Normalisieren
    for (int i = 0; i < 6; i++)
    {
      float len = _planes[i].Xyz.Length;
      _planes[i] /= len;
    }
  }

  // AABB gegen Frustum testen
  public bool IsVisible(Vector3 min, Vector3 max)
  {
    foreach (var plane in _planes)
    {
      // Positiver Vertex — maximaler Abstand in Plane-Richtung
      var positive = new Vector3(
          plane.X >= 0 ? max.X : min.X,
          plane.Y >= 0 ? max.Y : min.Y,
          plane.Z >= 0 ? max.Z : min.Z);

      if (Vector3.Dot(plane.Xyz, positive) + plane.W < 0)
        return false; // außerhalb
    }
    return true;
  }

  // Chunk AABB prüfen
  public bool IsChunkVisible(int cx, int cz)
  {
    var min = new Vector3(cx * 16f - 1f, -1f, cz * 16f - 1f);
    var max = new Vector3(cx * 16f + 17f, 257f, cz * 16f + 17f);
    return IsVisible(min, max);
  }
}