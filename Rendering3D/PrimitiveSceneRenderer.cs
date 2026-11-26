using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class PrimitiveSceneRenderer : IDisposable
{
    private ShaderProgram? _shader;
    private GpuMesh? _cubeMesh;
    private GpuMesh? _planeMesh;
    private GpuMesh? _cylinderMesh;
    private GpuMesh? _billboardMesh;
    private Texture2D? _coralineTex;
    private Texture2D? _beldamTex;
    private Texture2D? _doorTex;
    private Texture2D? _eyeTex;
    private bool _disposed;

    public void Initialize()
    {
        _shader = ShaderLoader.Load("primitive_vertex.glsl", "primitive_fragment.glsl");
        _cubeMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCube(1.0f));
        _planeMesh = new GpuMesh(PrimitiveMeshBuilder.CreatePlane(1.0f, 1.0f));
        _cylinderMesh = new GpuMesh(PrimitiveMeshBuilder.CreateCylinder(0.34f, 0.55f, 16));
        _billboardMesh = new GpuMesh(PrimitiveMeshBuilder.CreateBillboardQuad(2.4f, 1.8f));

        _coralineTex = TextureLoader.Load("coraline_player.jpg");
        _beldamTex = TextureLoader.Load("beldam_enemy.jpg");
        _doorTex = TextureLoader.Load("small_door.jpg");
        _eyeTex = TextureLoader.Load("ui_ghost_eye.jpg");
    }

    public void Render(RenderSnapshot snapshot, TacticalCamera3D camera, float aspectRatio, float elapsedTime = 0f)
    {
        if (_shader is null) return;

        _shader.Use();
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix(aspectRatio));
        _shader.SetVector3("uLightDir", new Vector3(-0.45f, 0.85f, 0.35f));
        _shader.SetInt("uMainTex", 0);

        DrawTiles(snapshot);
        DrawEntities(snapshot, camera, elapsedTime);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cubeMesh?.Dispose();
        _planeMesh?.Dispose();
        _cylinderMesh?.Dispose();
        _billboardMesh?.Dispose();
        _coralineTex?.Dispose();
        _beldamTex?.Dispose();
        _doorTex?.Dispose();
        _eyeTex?.Dispose();
        _shader?.Dispose();
        _disposed = true;
    }

    private void DrawTiles(RenderSnapshot snapshot)
    {
        var isOther = snapshot.ActiveWorld == WorldDimension.OtherWorld;
        _shader!.SetBool("uUseTexture", false);
        _shader.SetFloat("uChromaTolerance", 0.0f);

        foreach (var tile in snapshot.Tiles)
        {
            var cx = tile.X + 0.5f;
            var cz = tile.Y + 0.5f;

            if (tile.IsWalkable)
            {
                DrawMesh(_planeMesh!, Matrix4.CreateTranslation(cx, 0.0f, cz), GetFloorColor(tile, isOther));
            }
            else
            {
                var wallH = tile.DiffersBetweenWorlds ? 1.15f : 1.55f;
                var wallCol = GetWallColor(tile, isOther);
                DrawMesh(_cubeMesh!, Matrix4.CreateScale(0.98f, wallH, 0.98f) * Matrix4.CreateTranslation(cx, wallH * 0.5f, cz), wallCol);

                var capCol = new Vector4(wallCol.X * 0.72f, wallCol.Y * 0.72f, wallCol.Z * 0.78f, 1.0f);
                DrawMesh(_cubeMesh!, Matrix4.CreateScale(1.02f, 0.12f, 1.02f) * Matrix4.CreateTranslation(cx, wallH + 0.06f, cz), capCol);
            }
        }
    }

    private void DrawEntities(RenderSnapshot snapshot, TacticalCamera3D camera, float elapsedTime)
    {
        var billboardYaw = MathF.Atan2(camera.Offset.X, camera.Offset.Z);

        foreach (var entity in snapshot.Entities)
        {
            if (entity.VisibleIn != snapshot.ActiveWorld) continue;

            var pos = new Vector3(entity.Position.X, 0.02f, entity.Position.Y);
            switch (entity.Type)
            {
                case EntityType.Player:
                    DrawBillboard(pos, billboardYaw, 1.15f, _coralineTex!, Vector4.One, 0.08f);
                    break;
                case EntityType.Guard:
                    var tint = entity.StateTag == "AlertChase" ? new Vector4(1.0f, 0.45f, 0.45f, 1.0f) : Vector4.One;
                    DrawBillboard(pos, billboardYaw, 1.45f, _beldamTex!, tint, 0.13f);
                    break;
                case EntityType.SmallDoor:
                    DrawBillboard(pos, billboardYaw, 1.20f, _doorTex!, Vector4.One, 0.09f);
                    break;
                case EntityType.GhostEye:
                    var bob = MathF.Sin(elapsedTime * 3.0f + entity.EntityId) * 0.08f;
                    _shader!.SetBool("uUseTexture", false);
                    _shader.SetFloat("uChromaTolerance", 0.0f);
                    DrawMesh(_cylinderMesh!, Matrix4.CreateTranslation(pos.X, 0.28f, pos.Z), new Vector4(0.35f, 0.40f, 0.52f, 1.0f));
                    DrawBillboard(new Vector3(pos.X, 0.62f + bob, pos.Z), billboardYaw, 0.65f, _eyeTex!, Vector4.One, 0.10f);
                    break;
            }
        }
    }

    private static Vector4 GetFloorColor(TileRenderState tile, bool isOther)
    {
        if (tile.DiffersBetweenWorlds)
        {
            return new Vector4(0.24f, 0.78f, 0.82f, 1.0f);
        }

        var checker = (tile.X + tile.Y) % 2 == 0;
        if (tile.X < 8)
        {
            // Drawing Room: warm mahogany floorboards (or eerie violet-wood in OtherWorld)
            var plank = tile.Y % 2 == 0;
            return isOther
                ? (plank ? new Vector4(0.14f, 0.13f, 0.26f, 1f) : new Vector4(0.11f, 0.10f, 0.22f, 1f))
                : (plank ? new Vector4(0.32f, 0.23f, 0.18f, 1f) : new Vector4(0.27f, 0.19f, 0.15f, 1f));
        }

        if (tile.X < 16)
        {
            // Kitchen: gothic checkerboard tiles
            return isOther
                ? (checker ? new Vector4(0.18f, 0.20f, 0.34f, 1f) : new Vector4(0.09f, 0.10f, 0.20f, 1f))
                : (checker ? new Vector4(0.38f, 0.37f, 0.34f, 1f) : new Vector4(0.20f, 0.20f, 0.22f, 1f));
        }

        // Bedroom: woven rug center over dark timber
        var inRug = tile.X is >= 18 and <= 21 && tile.Y is >= 4 and <= 11;
        if (inRug)
        {
            return isOther ? new Vector4(0.26f, 0.15f, 0.36f, 1f) : new Vector4(0.42f, 0.18f, 0.22f, 1f);
        }

        return isOther
            ? (checker ? new Vector4(0.13f, 0.14f, 0.25f, 1f) : new Vector4(0.10f, 0.12f, 0.21f, 1f))
            : (checker ? new Vector4(0.24f, 0.20f, 0.18f, 1f) : new Vector4(0.21f, 0.17f, 0.16f, 1f));
    }

    private static Vector4 GetWallColor(TileRenderState tile, bool isOther)
    {
        if (tile.DiffersBetweenWorlds) return new Vector4(0.62f, 0.24f, 0.38f, 1.0f);
        return isOther ? new Vector4(0.29f, 0.21f, 0.45f, 1.0f) : new Vector4(0.45f, 0.39f, 0.33f, 1.0f);
    }

    private void DrawBillboard(Vector3 position, float yaw, float scale, Texture2D texture, Vector4 tint, float chromaTolerance)
    {
        texture.Bind(TextureUnit.Texture0);
        _shader!.SetBool("uUseTexture", true);
        _shader.SetFloat("uChromaTolerance", chromaTolerance);
        DrawMesh(_billboardMesh!, Matrix4.CreateScale(scale) * Matrix4.CreateRotationY(yaw) * Matrix4.CreateTranslation(position), tint);
    }

    private void DrawMesh(GpuMesh mesh, Matrix4 model, Vector4 baseColor)
    {
        _shader!.SetMatrix4("uModel", model);
        _shader.SetVector4("uBaseColor", baseColor);
        mesh.Draw();
    }
}
