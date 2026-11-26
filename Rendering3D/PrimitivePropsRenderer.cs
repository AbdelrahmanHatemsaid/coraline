using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class PrimitivePropsRenderer : IDisposable
{
    private ShaderProgram? _shader;
    private GpuMesh? _cubeMesh;
    private GpuMesh? _cylinderMesh;
    private bool _disposed;

    public void Initialize()
    {
        _shader = ShaderLoader.Load("primitive_vertex.glsl", "primitive_fragment.glsl");
        _cubeMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCube(1.0f));
        _cylinderMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCylinder(0.5f, 1.0f, 16));
    }

    public void Render(RenderSnapshot snapshot, TacticalCamera3D camera, float aspectRatio)
    {
        if (_shader is null) return;

        _shader.Use();
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix(aspectRatio));
        _shader.SetVector3("uLightDir", new Vector3(-0.45f, 0.85f, 0.35f));
        _shader.SetBool("uUseTexture", false);
        _shader.SetFloat("uChromaTolerance", 0.0f);

        DrawParts(PropPlacementLayout.BuildRoomFurniture(snapshot.ActiveWorld));

        foreach (var entity in snapshot.Entities)
        {
            if (entity.Type == EntityType.SmallDoor)
            {
                DrawParts(PropPlacementLayout.BuildSmallDoorArch(entity.Position, snapshot.CollectedEyes >= 3, snapshot.ActiveWorld));
            }
            else if (entity.Type == EntityType.GhostEye && entity.VisibleIn == snapshot.ActiveWorld)
            {
                DrawPedestalAccents(entity.Position, snapshot.ActiveWorld);
            }
        }

        if (snapshot.ActiveWorld == WorldDimension.OtherWorld)
        {
            foreach (var webPos in PropPlacementLayout.WebTrapPositions)
            {
                DrawParts(PropPlacementLayout.BuildWebTrap(webPos));
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cubeMesh?.Dispose();
        _cylinderMesh?.Dispose();
        _shader?.Dispose();
        _disposed = true;
    }

    private void DrawPedestalAccents(Vector2 pos, WorldDimension world)
    {
        var baseCol = world == WorldDimension.OtherWorld
            ? new Vector4(0.24f, 0.28f, 0.42f, 1f)
            : new Vector4(0.45f, 0.45f, 0.48f, 1f);
        var ringCol = new Vector4(0.20f, 0.94f, 0.64f, 1f);

        DrawPart(new PropPart(new(pos.X, 0.06f, pos.Y), new(0.84f, 0.12f, 0.84f), 0f, PropShape.Cylinder, baseCol));
        DrawPart(new PropPart(new(pos.X, 0.56f, pos.Y), new(0.76f, 0.06f, 0.76f), 0f, PropShape.Cylinder, ringCol));
    }

    private void DrawParts(IEnumerable<PropPart> parts)
    {
        foreach (var part in parts)
        {
            DrawPart(part);
        }
    }

    private void DrawPart(PropPart part)
    {
        var model = Matrix4.CreateScale(part.Scale)
            * Matrix4.CreateRotationY(part.RotationYRad)
            * Matrix4.CreateTranslation(part.Position);

        _shader!.SetMatrix4("uModel", model);
        _shader.SetVector4("uBaseColor", part.Color);
        var mesh = part.Shape == PropShape.Cube ? _cubeMesh! : _cylinderMesh!;
        mesh.Draw();
    }
}
