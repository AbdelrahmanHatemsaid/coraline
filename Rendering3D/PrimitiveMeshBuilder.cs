using OpenTK.Mathematics;

namespace Coraline.OpenGLCore;

public sealed record PrimitiveMeshData(float[] Vertices, int VertexCount)
{
    public const int FloatsPerVertex = 8; // Position (3) + Normal (3) + TexCoord (2)
}

public static class PrimitiveMeshBuilder
{
    public static PrimitiveMeshData CreateCube(float size = 1.0f)
    {
        var h = size * 0.5f;
        var verts = new List<float>(36 * PrimitiveMeshData.FloatsPerVertex);

        AddQuad(verts, new(-h, -h,  h), new( h, -h,  h), new( h,  h,  h), new(-h,  h,  h), Vector3.UnitZ);  // Front
        AddQuad(verts, new( h, -h, -h), new(-h, -h, -h), new(-h,  h, -h), new( h,  h, -h), -Vector3.UnitZ); // Back
        AddQuad(verts, new(-h, -h, -h), new(-h, -h,  h), new(-h,  h,  h), new(-h,  h, -h), -Vector3.UnitX); // Left
        AddQuad(verts, new( h, -h,  h), new( h, -h, -h), new( h,  h, -h), new( h,  h,  h), Vector3.UnitX);  // Right
        AddQuad(verts, new(-h,  h,  h), new( h,  h,  h), new( h,  h, -h), new(-h,  h, -h), Vector3.UnitY);  // Top
        AddQuad(verts, new(-h, -h, -h), new( h, -h, -h), new( h, -h,  h), new(-h, -h,  h), -Vector3.UnitY); // Bottom

        return Build(verts);
    }

    public static PrimitiveMeshData CreatePlane(float width = 1.0f, float depth = 1.0f)
    {
        var hw = width * 0.5f;
        var hd = depth * 0.5f;
        var verts = new List<float>(6 * PrimitiveMeshData.FloatsPerVertex);
        AddQuad(verts, new(-hw, 0f, hd), new(hw, 0f, hd), new(hw, 0f, -hd), new(-hw, 0f, -hd), Vector3.UnitY);
        return Build(verts);
    }

    public static PrimitiveMeshData CreateBillboardQuad(float width = 1.0f, float height = 1.0f)
    {
        var hw = width * 0.5f;
        var verts = new List<float>(6 * PrimitiveMeshData.FloatsPerVertex);
        AddQuad(verts, new(-hw, 0f, 0f), new(hw, 0f, 0f), new(hw, height, 0f), new(-hw, height, 0f), Vector3.UnitZ);
        return Build(verts);
    }

    public static PrimitiveMeshData CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 16)
    {
        if (segments < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(segments), "Cylinder requires at least 3 segments.");
        }

        var halfH = height * 0.5f;
        var verts = new List<float>(segments * 12 * PrimitiveMeshData.FloatsPerVertex);

        for (var i = 0; i < segments; i++)
        {
            var a0 = MathHelper.TwoPi * i / segments;
            var a1 = MathHelper.TwoPi * (i + 1) / segments;
            var (c0, s0) = (MathF.Cos(a0), MathF.Sin(a0));
            var (c1, s1) = (MathF.Cos(a1), MathF.Sin(a1));
            var u0 = i / (float)segments;
            var u1 = (i + 1) / (float)segments;

            var b0 = new Vector3(c0 * radius, -halfH, s0 * radius);
            var b1 = new Vector3(c1 * radius, -halfH, s1 * radius);
            var t0 = new Vector3(c0 * radius, halfH, s0 * radius);
            var t1 = new Vector3(c1 * radius, halfH, s1 * radius);
            var n0 = new Vector3(c0, 0f, s0);
            var n1 = new Vector3(c1, 0f, s1);

            // Side wall (2 triangles)
            AddVertex(verts, b0, n0, u0, 0f);
            AddVertex(verts, b1, n1, u1, 0f);
            AddVertex(verts, t1, n1, u1, 1f);

            AddVertex(verts, b0, n0, u0, 0f);
            AddVertex(verts, t1, n1, u1, 1f);
            AddVertex(verts, t0, n0, u0, 1f);

            // Top cap triangle
            AddVertex(verts, new Vector3(0f, halfH, 0f), Vector3.UnitY, 0.5f, 0.5f);
            AddVertex(verts, t0, Vector3.UnitY, 0.5f + c0 * 0.5f, 0.5f + s0 * 0.5f);
            AddVertex(verts, t1, Vector3.UnitY, 0.5f + c1 * 0.5f, 0.5f + s1 * 0.5f);

            // Bottom cap triangle
            AddVertex(verts, new Vector3(0f, -halfH, 0f), -Vector3.UnitY, 0.5f, 0.5f);
            AddVertex(verts, b1, -Vector3.UnitY, 0.5f + c1 * 0.5f, 0.5f + s1 * 0.5f);
            AddVertex(verts, b0, -Vector3.UnitY, 0.5f + c0 * 0.5f, 0.5f + s0 * 0.5f);
        }

        return Build(verts);
    }

    private static void AddQuad(List<float> verts, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
    {
        AddVertex(verts, p0, normal, 0f, 0f);
        AddVertex(verts, p1, normal, 1f, 0f);
        AddVertex(verts, p2, normal, 1f, 1f);

        AddVertex(verts, p0, normal, 0f, 0f);
        AddVertex(verts, p2, normal, 1f, 1f);
        AddVertex(verts, p3, normal, 0f, 1f);
    }

    private static void AddVertex(List<float> verts, Vector3 pos, Vector3 normal, float u, float v)
    {
        verts.Add(pos.X);
        verts.Add(pos.Y);
        verts.Add(pos.Z);
        verts.Add(normal.X);
        verts.Add(normal.Y);
        verts.Add(normal.Z);
        verts.Add(u);
        verts.Add(v);
    }

    private static PrimitiveMeshData Build(List<float> verts) =>
        new(verts.ToArray(), verts.Count / PrimitiveMeshData.FloatsPerVertex);
}
