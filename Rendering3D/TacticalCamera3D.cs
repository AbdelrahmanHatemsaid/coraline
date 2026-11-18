using OpenTK.Mathematics;

namespace Coraline.Rendering;

public sealed class TacticalCamera3D
{
    public Vector3 Target { get; set; } = new(12f, 0f, 8f);
    public Vector3 Offset { get; set; } = new(-10f, 16f, 10f);
    public float OrthographicSize { get; set; } = 14.0f;

    public Matrix4 GetViewMatrix()
    {
        var eye = Target + Offset;
        return Matrix4.LookAt(eye, Target, Vector3.UnitY);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        var halfHeight = OrthographicSize;
        var halfWidth = halfHeight * aspectRatio;
        return Matrix4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, 0.1f, 100.0f);
    }
}
