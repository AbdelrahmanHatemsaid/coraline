using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class DebugRenderer : IDisposable
{
    private ShaderProgram? _shader;
    private int _quadVao;
    private int _quadVbo;
    private int _lineVao;
    private int _lineVbo;
    private bool _disposed;

    public void Initialize()
    {
        var vertPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", "debug_vertex.glsl");
        var fragPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", "debug_fragment.glsl");
        _shader = new ShaderProgram(vertPath, fragPath);

        (_quadVao, _quadVbo) = CreateDynamicMeshBuffer(6);
        (_lineVao, _lineVbo) = CreateDynamicMeshBuffer(2);
    }

    public void Render(RenderSnapshot snapshot, Matrix4 viewMatrix, Matrix4 projectionMatrix, bool include2DOverlay = true)
    {
        if (_shader is null) return;

        _shader.Use();
        _shader.SetMatrix4("uView", viewMatrix);
        _shader.SetMatrix4("uProjection", projectionMatrix);

        Draw3DTiles(snapshot);
        Draw3DEntities(snapshot);
        Draw3DDebugLines(snapshot);
        if (include2DOverlay) Draw2DOverlay(snapshot);

        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_quadVbo != 0) GL.DeleteBuffer(_quadVbo);
        if (_quadVao != 0) GL.DeleteVertexArray(_quadVao);
        if (_lineVbo != 0) GL.DeleteBuffer(_lineVbo);
        if (_lineVao != 0) GL.DeleteVertexArray(_lineVao);
        _shader?.Dispose();
        _disposed = true;
    }

    private static (int Vao, int Vbo) CreateDynamicMeshBuffer(int vertexCount)
    {
        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertexCount * 3 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.BindVertexArray(0);
        return (vao, vbo);
    }

    private void Draw3DTiles(RenderSnapshot snapshot)
    {
        var isOther = snapshot.ActiveWorld == WorldDimension.OtherWorld;
        foreach (var tile in snapshot.Tiles)
        {
            var y = tile.IsWalkable ? 0.0f : 0.35f;
            DrawHorizontalQuad(tile.X + 0.04f, tile.Y + 0.04f, 0.92f, 0.92f, y, GetTileColor(tile, isOther));
        }
    }

    private void Draw3DEntities(RenderSnapshot snapshot)
    {
        foreach (var entity in snapshot.Entities)
        {
            if (entity.VisibleIn != snapshot.ActiveWorld) continue;

            var half = entity.Type == EntityType.GhostEye ? 0.25f : 0.36f;
            DrawHorizontalQuad(
                entity.Position.X - half,
                entity.Position.Y - half,
                half * 2f,
                half * 2f,
                0.18f,
                GetEntityColor(entity));

            if (entity.Type is EntityType.Player or EntityType.Guard)
            {
                var dir = new Vector2(MathF.Cos(entity.RotationRad), MathF.Sin(entity.RotationRad));
                Draw3DLine(entity.Position, entity.Position + dir * 0.75f, 0.22f, Vector4.One);
            }
        }
    }

    private void Draw3DDebugLines(RenderSnapshot snapshot)
    {
        var lineColor = snapshot.DetectionPercent > 0f
            ? new Vector4(1.0f, 0.25f, 0.25f, 1.0f)
            : new Vector4(1.0f, 0.75f, 0.20f, 1.0f);

        foreach (var (start, end) in snapshot.DebugLines)
        {
            Draw3DLine(start, end, 0.24f, lineColor);
        }
    }

    private void Draw2DOverlay(RenderSnapshot snapshot)
    {
        _shader!.SetMatrix4("uView", Matrix4.Identity);
        _shader.SetMatrix4("uProjection", Matrix4.Identity);
        GL.Disable(EnableCap.DepthTest);

        var worldColor = snapshot.ActiveWorld == WorldDimension.OtherWorld
            ? new Vector4(0.35f, 0.85f, 0.95f, 1.0f)
            : new Vector4(0.95f, 0.75f, 0.25f, 1.0f);
        DrawNdcQuad(-0.95f, 0.86f, 0.16f, 0.08f, worldColor);

        DrawNdcQuad(-0.40f, 0.86f, 0.80f, 0.07f, new Vector4(0.18f, 0.18f, 0.22f, 1.0f));
        var pct = Math.Clamp(snapshot.DetectionPercent / 100.0f, 0.0f, 1.0f);
        if (pct > 0.001f)
        {
            var barColor = Vector4.Lerp(new Vector4(0.95f, 0.75f, 0.15f, 1f), new Vector4(0.95f, 0.15f, 0.20f, 1f), pct);
            DrawNdcQuad(-0.40f, 0.86f, 0.80f * pct, 0.07f, barColor);
        }

        for (var i = 0; i < 3; i++)
        {
            var slotColor = i < snapshot.CollectedEyes
                ? new Vector4(0.20f, 0.95f, 0.55f, 1.0f)
                : new Vector4(0.25f, 0.25f, 0.30f, 1.0f);
            DrawNdcQuad(0.65f + i * 0.10f, 0.86f, 0.07f, 0.08f, slotColor);
        }

        GL.Enable(EnableCap.DepthTest);
    }

    private void DrawHorizontalQuad(float x, float z, float width, float depth, float y, Vector4 color)
    {
        float[] verts = [x, y, z, x + width, y, z, x + width, y, z + depth, x, y, z, x + width, y, z + depth, x, y, z + depth];
        UploadAndDraw(_quadVao, _quadVbo, verts, PrimitiveType.Triangles, 6, color);
    }

    private void DrawNdcQuad(float x, float y, float width, float height, Vector4 color)
    {
        float[] verts = [x, y, 0f, x + width, y, 0f, x + width, y + height, 0f, x, y, 0f, x + width, y + height, 0f, x, y + height, 0f];
        UploadAndDraw(_quadVao, _quadVbo, verts, PrimitiveType.Triangles, 6, color);
    }

    private void Draw3DLine(Vector2 start, Vector2 end, float y, Vector4 color)
    {
        float[] verts = [start.X, y, start.Y, end.X, y, end.Y];
        UploadAndDraw(_lineVao, _lineVbo, verts, PrimitiveType.Lines, 2, color);
    }

    private void UploadAndDraw(int vao, int vbo, float[] verts, PrimitiveType mode, int count, Vector4 color)
    {
        _shader!.SetVector4("uColor", color);
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);
        GL.DrawArrays(mode, 0, count);
    }

    private static Vector4 GetTileColor(TileRenderState tile, bool isOther)
    {
        if (tile.DiffersBetweenWorlds)
            return tile.IsWalkable ? new Vector4(0.25f, 0.72f, 0.78f, 1f) : new Vector4(0.55f, 0.22f, 0.35f, 1f);
        if (!tile.IsWalkable)
            return isOther ? new Vector4(0.24f, 0.18f, 0.36f, 1f) : new Vector4(0.36f, 0.32f, 0.28f, 1f);
        return isOther ? new Vector4(0.12f, 0.14f, 0.24f, 1f) : new Vector4(0.20f, 0.22f, 0.24f, 1f);
    }

    private static Vector4 GetEntityColor(EntityRenderState entity) => entity.Type switch
    {
        EntityType.Player => new Vector4(1.00f, 0.85f, 0.15f, 1.0f),
        EntityType.Guard => entity.StateTag == "AlertChase" ? new Vector4(1f, 0.15f, 0.20f, 1f) : new Vector4(0.92f, 0.38f, 0.20f, 1f),
        EntityType.GhostEye => new Vector4(0.20f, 0.95f, 0.55f, 1.0f),
        EntityType.SmallDoor => new Vector4(0.68f, 0.42f, 0.92f, 1.0f),
        _ => new Vector4(0.80f, 0.80f, 0.80f, 1.0f)
    };
}
