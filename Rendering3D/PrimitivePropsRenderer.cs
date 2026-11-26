using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class PrimitivePropsRenderer : IDisposable
{
    private ShaderProgram? _shader;
    private ShaderProgram? _outlineShader;
    private GpuMesh? _cubeMesh;
    private GpuMesh? _cylinderMesh;
    private bool _disposed;

    public void Initialize()
    {
        _shader = ShaderLoader.Load("primitive_vertex.glsl", "primitive_fragment.glsl");
        _outlineShader = ShaderLoader.Load("prop_outline_vertex.glsl", "prop_outline_fragment.glsl");
        _cubeMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCube(1.0f));
        _cylinderMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCylinder(0.5f, 1.0f, 16));
    }

    public void Render(RenderSnapshot snapshot, TacticalCamera3D camera, float aspectRatio)
    {
        if (_shader is null) return;

        var parts = GatherAllProps(snapshot);
        var view = camera.GetViewMatrix();
        var projection = camera.GetProjectionMatrix(aspectRatio);

        _shader.Use();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);
        _shader.SetVector3("uLightDir", new Vector3(-0.45f, 0.85f, 0.35f));
        _shader.SetBool("uUseTexture", false);
        _shader.SetFloat("uChromaTolerance", 0.0f);

        foreach (var part in parts)
        {
            DrawPart(part);
        }

        DrawPropOutlines(parts, view, projection, snapshot.ActiveWorld);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cubeMesh?.Dispose();
        _cylinderMesh?.Dispose();
        _outlineShader?.Dispose();
        _shader?.Dispose();
        _disposed = true;
    }

    private static List<PropPart> GatherAllProps(RenderSnapshot snapshot)
    {
        var parts = new List<PropPart>(PropPlacementLayout.BuildRoomFurniture(snapshot.ActiveWorld));

        foreach (var entity in snapshot.Entities)
        {
            if (entity.Type == EntityType.SmallDoor)
            {
                parts.AddRange(PropPlacementLayout.BuildSmallDoorArch(entity.Position, snapshot.CollectedEyes >= 3, snapshot.ActiveWorld));
            }
            else if (entity.Type == EntityType.GhostEye && entity.VisibleIn == snapshot.ActiveWorld)
            {
                var baseCol = snapshot.ActiveWorld == WorldDimension.OtherWorld
                    ? new Vector4(0.24f, 0.28f, 0.42f, 1f)
                    : new Vector4(0.45f, 0.45f, 0.48f, 1f);
                parts.Add(new(new(entity.Position.X, 0.06f, entity.Position.Y), new(0.84f, 0.12f, 0.84f), 0f, PropShape.Cylinder, baseCol));
                parts.Add(new(new(entity.Position.X, 0.56f, entity.Position.Y), new(0.76f, 0.06f, 0.76f), 0f, PropShape.Cylinder, new(0.20f, 0.94f, 0.64f, 1f)));
            }
        }

        if (snapshot.ActiveWorld == WorldDimension.OtherWorld)
        {
            foreach (var webPos in PropPlacementLayout.WebTrapPositions)
            {
                parts.AddRange(PropPlacementLayout.BuildWebTrap(webPos));
            }
        }

        return parts;
    }

    private void DrawPart(PropPart part)
    {
        _shader!.SetMatrix4("uModel", CreateModelMatrix(part));
        _shader.SetVector4("uBaseColor", part.Color);
        var mesh = part.Shape == PropShape.Cube ? _cubeMesh! : _cylinderMesh!;
        mesh.Draw();
    }

    private void DrawPropOutlines(List<PropPart> parts, Matrix4 view, Matrix4 projection, WorldDimension world)
    {
        if (_outlineShader is null) return;

        var inkColor = world == WorldDimension.OtherWorld
            ? new Vector4(0.05f, 0.03f, 0.11f, 1f)
            : new Vector4(0.08f, 0.06f, 0.08f, 1f);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Front);
        GL.DepthFunc(DepthFunction.Lequal);

        _outlineShader.Use();
        _outlineShader.SetMatrix4("uView", view);
        _outlineShader.SetMatrix4("uProjection", projection);
        _outlineShader.SetVector4("uOutlineColor", inkColor);
        _outlineShader.SetFloat("uOutlineThickness", 0.028f);

        foreach (var part in parts)
        {
            if (!part.CastOutline || part.Shape != PropShape.Cube) continue;
            _outlineShader.SetMatrix4("uModel", CreateModelMatrix(part));
            _cubeMesh!.Draw();
        }

        GL.DepthFunc(DepthFunction.Less);
        GL.CullFace(CullFaceMode.Back);
        GL.Disable(EnableCap.CullFace);
    }

    private static Matrix4 CreateModelMatrix(PropPart part) =>
        Matrix4.CreateScale(part.Scale)
        * Matrix4.CreateRotationY(part.RotationYRad)
        * Matrix4.CreateTranslation(part.Position);
}
