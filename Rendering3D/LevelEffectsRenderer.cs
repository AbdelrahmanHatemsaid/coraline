using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class LevelEffectsRenderer : IDisposable
{
    private const int ConeRayCount = 24;
    private const int RingSegments = 24;
    private const int MaxBufferFloats = 256 * 3;
    private const float GuardVisionRange = 6.0f;
    private static readonly float GuardFovRad = MathHelper.DegreesToRadians(75.0f);

    private sealed class NoiseRipple
    {
        public Vector2 Origin { get; init; }
        public float MaxRadius { get; init; }
        public string SourceTag { get; init; } = string.Empty;
        public float Elapsed { get; set; }
        public float Duration { get; init; }
    }

    private readonly GameEventBus _eventBus;
    private readonly List<NoiseRipple> _ripples = [];
    private ShaderProgram? _shader;
    private int _vao;
    private int _vbo;
    private float _elapsedTime;
    private float _sprintRippleCooldown;
    private bool _disposed;

    public int ActiveRippleCount => _ripples.Count;

    public LevelEffectsRenderer(GameEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Initialize()
    {
        _shader = ShaderLoader.Load("debug_vertex.glsl", "debug_fragment.glsl");
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, MaxBufferFloats * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.BindVertexArray(0);
    }

    public void Update(float deltaTime)
    {
        _elapsedTime += deltaTime;
        if (_sprintRippleCooldown > 0f) _sprintRippleCooldown -= deltaTime;

        for (var i = _ripples.Count - 1; i >= 0; i--)
        {
            _ripples[i].Elapsed += deltaTime;
            if (_ripples[i].Elapsed >= _ripples[i].Duration)
            {
                _ripples.RemoveAt(i);
            }
        }
    }

    public void Render(RenderSnapshot snapshot, Matrix4 viewMatrix, Matrix4 projectionMatrix)
    {
        if (_shader is null) return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);

        _shader.Use();
        _shader.SetMatrix4("uView", viewMatrix);
        _shader.SetMatrix4("uProjection", projectionMatrix);

        DrawGuardVisionCones(snapshot);
        DrawNoiseRipples();

        GL.BindVertexArray(0);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _ripples.Clear();
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        _shader?.Dispose();
        _vbo = 0;
        _vao = 0;
    }

    private void OnNoiseEmitted(NoiseEmittedEvent e)
    {
        if (_disposed) return;
        if (e.Radius <= 0.1f && e.SourceTag != "Alert") return;
        if (e.SourceTag == "Sprint")
        {
            if (_sprintRippleCooldown > 0f) return;
            _sprintRippleCooldown = 0.25f;
        }

        var duration = e.SourceTag == "Distraction" ? 1.0f : 0.55f;
        var radius = MathF.Max(1.6f, MathF.Min(e.Radius * 0.55f, 4.5f));
        _ripples.Add(new NoiseRipple
        {
            Origin = e.Origin,
            MaxRadius = radius,
            SourceTag = e.SourceTag,
            Duration = duration
        });
    }

    private void DrawGuardVisionCones(RenderSnapshot snapshot)
    {
        if (snapshot.ActiveWorld != WorldDimension.OtherWorld) return;

        foreach (var entity in snapshot.Entities)
        {
            if (entity.Type != EntityType.Guard) continue;

            var verts = VisionConeMeshBuilder.BuildConeFanVertices(
                entity.Position,
                entity.RotationRad,
                GuardFovRad,
                GuardVisionRange,
                ConeRayCount,
                snapshot.Tiles);

            var color = GetConeColor(entity.StateTag, snapshot.DetectionPercent);
            DrawTriangles(verts, color);
        }
    }

    private void DrawNoiseRipples()
    {
        foreach (var ripple in _ripples)
        {
            var progress = Math.Clamp(ripple.Elapsed / ripple.Duration, 0.0f, 1.0f);
            var outer = 0.25f + progress * ripple.MaxRadius;
            var inner = MathF.Max(0.10f, outer - 0.16f);
            var alpha = (1.0f - progress) * 0.65f;

            var color = ripple.SourceTag switch
            {
                "Distraction" => new Vector4(0.30f, 0.92f, 0.95f, alpha),
                "Alert" => new Vector4(1.00f, 0.20f, 0.25f, alpha),
                _ => new Vector4(0.95f, 0.85f, 0.45f, alpha)
            };

            var verts = VisionConeMeshBuilder.BuildRingVertices(ripple.Origin, inner, outer, RingSegments);
            DrawTriangles(verts, color);
        }
    }

    private void DrawTriangles(float[] verts, Vector4 color)
    {
        if (verts.Length == 0 || verts.Length > MaxBufferFloats) return;

        _shader!.SetVector4("uColor", color);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);
        GL.DrawArrays(PrimitiveType.Triangles, 0, verts.Length / 3);
    }

    private Vector4 GetConeColor(string stateTag, float detectionPercent)
    {
        if (stateTag == "AlertChase")
        {
            var pulse = 0.36f + 0.14f * MathF.Sin(_elapsedTime * 10.0f);
            return new Vector4(0.98f, 0.14f, 0.20f, pulse);
        }

        if (stateTag == "Investigating" || detectionPercent > 5.0f)
        {
            return new Vector4(1.00f, 0.58f, 0.15f, 0.36f);
        }

        return new Vector4(0.95f, 0.76f, 0.24f, 0.26f);
    }
}
