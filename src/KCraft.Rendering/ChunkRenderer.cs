// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Assets;
using KCraft.Rendering.Shaders;
using KCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace KCraft.Rendering;

public sealed class ChunkRenderer
{
  private readonly WorldShader _shader;
  private readonly FrustumCuller _frustum = new();

  private readonly List<(ChunkMesh mesh, Chunk chunk, Vector3i chunkPos)>
    _visibleChunks = [];

  public int VisibleChunkCount => _visibleChunks.Count;

  public ChunkRenderer(WorldShader shader)
  {
    _shader = shader;
  }

  public void Draw(
    WorldManager world,
    TextureManager textureManager,
    WorldTime worldTime,
    Matrix4 view,
    Matrix4 projection)
  {
    _shader.Use();

    GL.UniformMatrix4(
      _shader.ViewLocation,
      false,
      ref view);

    GL.UniformMatrix4(
      _shader.ProjectionLocation,
      false,
      ref projection);

    float skyLight = worldTime.SkyLight;

    float ambient = Math.Clamp(
      skyLight * (1.0f - 0.267f) + 0.267f,
      0.267f,
      1.0f);

    GL.Uniform1(
      _shader.AmbientLocation,
      ambient);

    CollectVisibleChunks(world, view, projection);

    DrawSolidChunks(textureManager);
    DrawWaterChunks(textureManager);
  }

  private void CollectVisibleChunks(
    WorldManager world,
    Matrix4 view,
    Matrix4 projection)
  {
    _frustum.Update(view * projection);

    _visibleChunks.Clear();

    foreach (var chunk in world.ChunkMeshes)
    {
      if (_frustum.IsChunkVisible(
          chunk.chunkPos.X,
          chunk.chunkPos.Z))
      {
        _visibleChunks.Add(chunk);
      }
    }
  }

  private void DrawSolidChunks(TextureManager textureManager)
  {
    GL.Uniform1(
      _shader.AlphaLocation,
      1.0f);

    foreach (var (mesh, _, chunkPos) in _visibleChunks)
    {
      var model = Matrix4.CreateTranslation(
        new Vector3(
          chunkPos.X * Chunk.Width,
          0,
          chunkPos.Z * Chunk.Depth));

      GL.UniformMatrix4(
        _shader.ModelLocation,
        false,
        ref model);

      mesh.Draw(
        textureManager,
        _shader.TextureLocation,
        _shader.TintLocation);
    }
  }

  private void DrawWaterChunks(TextureManager textureManager)
  {
    GL.Enable(EnableCap.Blend);

    GL.BlendFunc(
      BlendingFactor.SrcAlpha,
      BlendingFactor.OneMinusSrcAlpha);

    GL.DepthMask(false);

    GL.Uniform1(
      _shader.AlphaLocation,
      0.8f);

    foreach (var (mesh, _, chunkPos) in _visibleChunks)
    {
      var model = Matrix4.CreateTranslation(
        new Vector3(
          chunkPos.X * Chunk.Width,
          0,
          chunkPos.Z * Chunk.Depth));

      GL.UniformMatrix4(
        _shader.ModelLocation,
        false,
        ref model);

      mesh.DrawWater(
        textureManager,
        _shader.TextureLocation,
        _shader.TintLocation);
    }

    GL.DepthMask(true);
    GL.Disable(EnableCap.Blend);

    GL.Uniform1(
      _shader.AlphaLocation,
      1.0f);
  }
}