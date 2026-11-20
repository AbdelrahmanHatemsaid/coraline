using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class TacticalCamera3D
{
    public const float MinOrthographicSize = 6.0f;
    public const float MaxOrthographicSize = 18.0f;

    public Vector3 Target { get; set; } = new(10f, 0f, 7f);
    public Vector3 Offset { get; set; } = new(-10f, 16f, 10f);
    public float OrthographicSize { get; private set; } = 10.0f;
    public float FollowSpeed { get; set; } = 6.0f;

    public Vector3 Position => Target + Offset;

    public Vector3 Forward => Vector3.Normalize(Target - Position);

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));

    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    public void FollowTarget(Vector2 worldPos, float deltaTime)
    {
        var desired = new Vector3(
            Math.Clamp(worldPos.X, 5.0f, 19.0f),
            0.0f,
            Math.Clamp(worldPos.Y, 4.0f, 12.0f));
        var t = 1.0f - MathF.Exp(-FollowSpeed * MathF.Max(0.0f, deltaTime));
        Target = Vector3.Lerp(Target, desired, t);
    }

    public void Zoom(float delta)
    {
        OrthographicSize = Math.Clamp(
            OrthographicSize + delta,
            MinOrthographicSize,
            MaxOrthographicSize);
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Target, Vector3.UnitY);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        var safeAspect = aspectRatio > 0.001f ? aspectRatio : 16.0f / 9.0f;
        var halfHeight = OrthographicSize;
        var halfWidth = halfHeight * safeAspect;
        return Matrix4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, 0.1f, 120.0f);
    }
}
