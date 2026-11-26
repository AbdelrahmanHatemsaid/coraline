using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public readonly record struct OutlineRenderStyle(Vector4 Color, float WallThickness, float PropThickness);

public sealed class ComicOutlineRenderer : IDisposable
{
    private ShaderProgram? _shader;
    private GpuMesh? _cubeMesh;
    private GpuMesh? _cylinderMesh;
    private bool _disposed;

    public static OutlineRenderStyle GetStyle(WorldDimension activeWorld) =>
        activeWorld == WorldDimension.OtherWorld
            ? new OutlineRenderStyle(new Vector4(0.05f, 0.03f, 0.11f, 1.0f), 0.048f, 0.038f)
            : new OutlineRenderStyle(new Vector4(0.08f, 0.06f, 0.09f, 1.0f), 0.044f, 0.034f);

    public void Initialize()
    {
        _shader = ShaderLoader.Load("outline_vertex.glsl", "outline_fragment.glsl");
        _cubeMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCube(1.0f));
        _cylinderMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCylinder(0.32f, 0.55f, 16));
    }

    public void Render(RenderSnapshot snapshot, TacticalCamera3D camera, float aspectRatio)
    {
        if (_shader is null) return;

        var style = GetStyle(snapshot.ActiveWorld);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Front);
        GL.DepthFunc(DepthFunction.Lequal);

        _shader.Use();
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix(aspectRatio));
        _shader.SetVector4("uOutlineColor", style.Color);

        DrawWallOutlines(snapshot, style.WallThickness);
        DrawPropOutlines(snapshot, style.PropThickness);

        GL.DepthFunc(DepthFunction.Less);
        GL.CullFace(CullFaceMode.Back);
        GL.Disable(EnableCap.CullFace);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cubeMesh?.Dispose();
        _cylinderMesh?.Dispose();
        _shader?.Dispose();
        _disposed = true;
    }

    private void DrawWallOutlines(RenderSnapshot snapshot, float thickness)
    {
        _shader!.SetFloat("uOutlineThickness", thickness);
        foreach (var tile in snapshot.Tiles)
        {
            if (tile.IsWalkable) continue;

            var wallHeight = tile.DiffersBetweenWorlds ? 1.15f : 1.55f;
            var model = Matrix4.CreateScale(0.98f, wallHeight, 0.98f)
                * Matrix4.CreateTranslation(tile.X + 0.5f, wallHeight * 0.5f, tile.Y + 0.5f);
            _shader.SetMatrix4("uModel", model);
            _cubeMesh!.Draw();
        }
    }

    private void DrawPropOutlines(RenderSnapshot snapshot, float thickness)
    {
        _shader!.SetFloat("uOutlineThickness", thickness);
        foreach (var entity in snapshot.Entities)
        {
            if (entity.VisibleIn != snapshot.ActiveWorld) continue;
            if (entity.Type != EntityType.GhostEye) continue;

            var model = Matrix4.CreateTranslation(entity.Position.X, 0.28f, entity.Position.Y);
            _shader.SetMatrix4("uModel", model);
            _cylinderMesh!.Draw();
        }
    }
}
