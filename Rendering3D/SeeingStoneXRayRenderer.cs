using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public readonly record struct XRayHighlightSpec(Vector4 Color, float Scale, float ChromaTolerance);

public sealed class SeeingStoneXRayRenderer : IDisposable
{
    private ShaderProgram? _shader;
    private GpuMesh? _billboardMesh;
    private Texture2D? _beldamTex;
    private Texture2D? _eyeTex;
    private bool _disposed;

    public static bool IsActiveFor(RenderSnapshot snapshot) =>
        snapshot.ActiveWorld == WorldDimension.OtherWorld;

    public static XRayHighlightSpec? GetHighlightSpec(EntityRenderState entity, float elapsedTime)
    {
        var pulse = 0.75f + 0.25f * MathF.Sin(elapsedTime * 5.0f + entity.EntityId);
        return entity.Type switch
        {
            EntityType.Guard => new XRayHighlightSpec(
                entity.StateTag == "AlertChase"
                    ? new Vector4(1.00f, 0.18f, 0.24f, 0.78f * pulse)
                    : new Vector4(0.92f, 0.30f, 0.58f, 0.68f * pulse),
                1.45f,
                0.13f),
            EntityType.GhostEye => new XRayHighlightSpec(
                new Vector4(0.18f, 0.98f, 0.68f, 0.82f * pulse),
                0.65f,
                0.10f),
            _ => null
        };
    }

    public void Initialize()
    {
        _shader = ShaderLoader.Load("xray_vertex.glsl", "xray_fragment.glsl");
        _billboardMesh = new GpuMesh(PrimitiveMeshBuilder.CreateBillboardQuad(2.4f, 1.8f));
        _beldamTex = TextureLoader.Load("beldam_enemy.jpg");
        _eyeTex = TextureLoader.Load("ui_ghost_eye.jpg");
    }

    public void Render(RenderSnapshot snapshot, TacticalCamera3D camera, float aspectRatio, float elapsedTime)
    {
        if (_shader is null || !IsActiveFor(snapshot)) return;

        var billboardYaw = MathF.Atan2(camera.Offset.X, camera.Offset.Z);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);
        GL.DepthFunc(DepthFunction.Greater);

        _shader.Use();
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix(aspectRatio));
        _shader.SetFloat("uTime", elapsedTime);
        _shader.SetInt("uMainTex", 0);

        foreach (var entity in snapshot.Entities)
        {
            var spec = GetHighlightSpec(entity, elapsedTime);
            if (spec is null) continue;

            var y = entity.Type == EntityType.GhostEye
                ? 0.62f + MathF.Sin(elapsedTime * 3.0f + entity.EntityId) * 0.08f
                : 0.02f;

            var tex = entity.Type == EntityType.Guard ? _beldamTex! : _eyeTex!;
            tex.Bind(TextureUnit.Texture0);

            var model = Matrix4.CreateScale(spec.Value.Scale)
                * Matrix4.CreateRotationY(billboardYaw)
                * Matrix4.CreateTranslation(entity.Position.X, y, entity.Position.Y);

            _shader.SetMatrix4("uModel", model);
            _shader.SetVector4("uXRayColor", spec.Value.Color);
            _shader.SetFloat("uChromaTolerance", spec.Value.ChromaTolerance);
            _billboardMesh!.Draw();
        }

        GL.DepthFunc(DepthFunction.Less);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _billboardMesh?.Dispose();
        _beldamTex?.Dispose();
        _eyeTex?.Dispose();
        _shader?.Dispose();
        _disposed = true;
    }
}
