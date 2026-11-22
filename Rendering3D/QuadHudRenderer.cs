using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public readonly record struct HudLayoutState(
    float DetectionFillRatio,
    int LitGhostEyeSlots,
    bool IsSeeingStoneActive,
    bool ShowButtonEyesOverlay,
    bool ShowVictoryOverlay
);

public sealed class QuadHudRenderer : IDisposable
{
    private const float AssetAspect = 16.0f / 9.0f;
    private ShaderProgram? _shader;
    private Texture2D? _seeingStoneTex;
    private Texture2D? _ghostEyeTex;
    private Texture2D? _detectionIconTex;
    private int _vao;
    private int _vbo;
    private bool _disposed;

    public static HudLayoutState ComputeLayout(RenderSnapshot snapshot) => new(
        Math.Clamp(snapshot.DetectionPercent, 0.0f, 100.0f) / 100.0f,
        Math.Clamp(snapshot.CollectedEyes, 0, 3),
        snapshot.ActiveWorld == WorldDimension.OtherWorld,
        snapshot.IsGameOver,
        snapshot.IsGameWon);

    public void Initialize()
    {
        var shaderDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
        var texDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Textures");
        _shader = new ShaderProgram(Path.Combine(shaderDir, "hud_vertex.glsl"), Path.Combine(shaderDir, "hud_fragment.glsl"));
        _seeingStoneTex = new Texture2D(Path.Combine(texDir, "ui_seeing_stone.jpg"));
        _ghostEyeTex = new Texture2D(Path.Combine(texDir, "ui_ghost_eye.jpg"));
        _detectionIconTex = new Texture2D(Path.Combine(texDir, "ui_detection_meter.jpg"));

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 6 * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

        const int stride = 4 * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.BindVertexArray(0);
    }

    public void Render(RenderSnapshot snapshot, float aspectRatio)
    {
        if (_shader is null) return;

        var layout = ComputeLayout(snapshot);
        var safeAspect = aspectRatio > 0.001f ? aspectRatio : AssetAspect;

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        var texLoc = GL.GetUniformLocation(_shader.Handle, "uHudTexture");
        if (texLoc != -1) GL.Uniform1(texLoc, 0);

        DrawSeeingStoneWidget(layout, safeAspect);
        DrawDetectionMeterWidget(layout, safeAspect);
        DrawGhostEyeSlots(layout, safeAspect);

        if (layout.ShowButtonEyesOverlay) DrawButtonEyesGameOver(safeAspect);
        else if (layout.ShowVictoryOverlay) DrawVictoryOverlay(safeAspect);

        GL.BindVertexArray(0);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        _seeingStoneTex?.Dispose();
        _ghostEyeTex?.Dispose();
        _detectionIconTex?.Dispose();
        _shader?.Dispose();
        _vbo = 0;
        _vao = 0;
        _disposed = true;
    }

    private void DrawSeeingStoneWidget(HudLayoutState layout, float aspect)
    {
        var iconH = 0.24f;
        var iconW = iconH * AssetAspect / aspect;
        var borderCol = layout.IsSeeingStoneActive ? new Vector4(0.18f, 0.92f, 0.62f, 0.85f) : new Vector4(0.32f, 0.30f, 0.38f, 0.65f);

        DrawSolidQuad(-0.96f, -0.94f, iconW + 0.03f, iconH + 0.03f, new Vector4(0.06f, 0.07f, 0.11f, 0.78f));
        DrawSolidQuad(-0.96f, -0.94f, iconW + 0.03f, 0.012f, borderCol);

        var tint = layout.IsSeeingStoneActive ? Vector4.One : new Vector4(0.65f, 0.65f, 0.72f, 0.90f);
        DrawTexturedQuad(-0.945f, -0.925f, iconW, iconH, _seeingStoneTex!, tint, 0.10f, layout.IsSeeingStoneActive ? 0.0f : 0.65f);
    }

    private void DrawDetectionMeterWidget(HudLayoutState layout, float aspect)
    {
        const float barW = 0.64f, barX = -0.32f, barY = 0.84f, barH = 0.065f;
        DrawSolidQuad(barX - 0.012f, barY - 0.012f, barW + 0.024f, barH + 0.024f, new Vector4(0.07f, 0.06f, 0.09f, 0.88f));
        DrawSolidQuad(barX, barY, barW, barH, new Vector4(0.16f, 0.15f, 0.20f, 0.95f));

        if (layout.DetectionFillRatio > 0.001f)
        {
            var fillCol = Vector4.Lerp(new Vector4(0.95f, 0.76f, 0.18f, 1f), new Vector4(0.92f, 0.12f, 0.16f, 1f), layout.DetectionFillRatio);
            DrawSolidQuad(barX, barY, barW * layout.DetectionFillRatio, barH, fillCol);
        }

        var emblemH = 0.22f;
        var emblemW = emblemH * AssetAspect / aspect;
        var iconTint = Vector4.Lerp(new Vector4(0.92f, 0.92f, 0.95f, 1.0f), new Vector4(1.0f, 0.48f, 0.48f, 1.0f), layout.DetectionFillRatio);
        DrawTexturedQuad(barX - emblemW - 0.01f, barY - 0.075f, emblemW, emblemH, _detectionIconTex!, iconTint, 0.045f, 0.0f);
    }

    private void DrawGhostEyeSlots(HudLayoutState layout, float aspect)
    {
        var slotH = 0.15f;
        var slotW = slotH * AssetAspect / aspect;
        var spacing = slotW + 0.02f;
        var startX = 0.95f - spacing * 3.0f;

        for (var i = 0; i < 3; i++)
        {
            var collected = i < layout.LitGhostEyeSlots;
            var x = startX + i * spacing;
            var frameCol = collected ? new Vector4(0.18f, 0.88f, 0.62f, 0.50f) : new Vector4(0.10f, 0.11f, 0.15f, 0.78f);
            DrawSolidQuad(x - 0.008f, 0.785f, slotW + 0.016f, slotH + 0.016f, frameCol);

            var tint = collected ? Vector4.One : new Vector4(0.50f, 0.54f, 0.60f, 0.60f);
            DrawTexturedQuad(x, 0.793f, slotW, slotH, _ghostEyeTex!, tint, 0.055f, collected ? 0.0f : 0.80f);
        }
    }

    private void DrawButtonEyesGameOver(float aspect)
    {
        DrawSolidQuad(-1.0f, -1.0f, 2.0f, 2.0f, new Vector4(0.03f, 0.01f, 0.02f, 0.85f));
        var eyeH = 0.58f;
        var eyeW = eyeH * AssetAspect / aspect;
        var crimson = new Vector4(1.0f, 0.48f, 0.48f, 1.0f);
        DrawTexturedQuad(-eyeW - 0.04f, -eyeH * 0.5f, eyeW, eyeH, _detectionIconTex!, crimson, 0.045f, 0.0f);
        DrawTexturedQuad(0.04f, -eyeH * 0.5f, eyeW, eyeH, _detectionIconTex!, crimson, 0.045f, 0.0f);
    }

    private void DrawVictoryOverlay(float aspect)
    {
        DrawSolidQuad(-1.0f, -1.0f, 2.0f, 2.0f, new Vector4(0.03f, 0.08f, 0.09f, 0.76f));
        var stoneH = 0.52f;
        var stoneW = stoneH * AssetAspect / aspect;
        DrawTexturedQuad(-stoneW * 0.5f, -stoneH * 0.5f, stoneW, stoneH, _seeingStoneTex!, Vector4.One, 0.10f, 0.0f);
    }

    private void DrawSolidQuad(float x, float y, float w, float h, Vector4 color)
    {
        _shader!.SetFloat("uUseTexture", 0.0f);
        _shader.SetFloat("uChromaTolerance", 0.0f);
        _shader.SetFloat("uDesaturate", 0.0f);
        _shader.SetVector4("uColor", color);
        DrawQuadVertices(x, y, w, h);
    }

    private void DrawTexturedQuad(float x, float y, float w, float h, Texture2D tex, Vector4 tint, float chroma, float desat)
    {
        tex.Bind(TextureUnit.Texture0);
        _shader!.SetFloat("uUseTexture", 1.0f);
        _shader.SetFloat("uChromaTolerance", chroma);
        _shader.SetFloat("uDesaturate", desat);
        _shader.SetVector4("uColor", tint);
        DrawQuadVertices(x, y, w, h);
    }

    private void DrawQuadVertices(float x, float y, float w, float h)
    {
        float[] verts = [x, y, 0f, 0f, x + w, y, 1f, 0f, x + w, y + h, 1f, 1f, x, y, 0f, 0f, x + w, y + h, 1f, 1f, x, y + h, 0f, 1f];
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }
}
