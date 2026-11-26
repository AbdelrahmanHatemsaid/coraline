using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public static class VisionConeMeshBuilder
{
    public static float[] BuildConeFanVertices(
        Vector2 origin,
        float facingRad,
        float fovRad,
        float range,
        int rayCount,
        IReadOnlyList<TileRenderState> tiles,
        int gridWidth = 24,
        int gridHeight = 16)
    {
        if (rayCount < 1 || range <= 0f || fovRad <= 0f) return [];

        var walkableGrid = BuildWalkableLookup(tiles, gridWidth, gridHeight);
        var halfFov = fovRad * 0.5f;
        var endpoints = new Vector2[rayCount + 1];

        for (var i = 0; i <= rayCount; i++)
        {
            var angle = facingRad - halfFov + (fovRad * i / rayCount);
            endpoints[i] = CastRayAgainstTiles(origin, angle, range, walkableGrid, gridWidth, gridHeight);
        }

        var verts = new float[rayCount * 3 * 3];
        const float y = 0.03f;
        var idx = 0;

        for (var i = 0; i < rayCount; i++)
        {
            var p1 = endpoints[i];
            var p2 = endpoints[i + 1];
            idx = WriteVertex(verts, idx, origin.X, y, origin.Y);
            idx = WriteVertex(verts, idx, p1.X, y, p1.Y);
            idx = WriteVertex(verts, idx, p2.X, y, p2.Y);
        }

        return verts;
    }

    public static float[] BuildRingVertices(Vector2 center, float innerRadius, float outerRadius, int segments = 24)
    {
        if (segments < 3 || outerRadius <= innerRadius) return [];

        var verts = new float[segments * 6 * 3];
        const float y = 0.045f;
        var idx = 0;

        for (var i = 0; i < segments; i++)
        {
            var a0 = MathHelper.TwoPi * i / segments;
            var a1 = MathHelper.TwoPi * (i + 1) / segments;
            var (c0, s0) = (MathF.Cos(a0), MathF.Sin(a0));
            var (c1, s1) = (MathF.Cos(a1), MathF.Sin(a1));

            var in0 = new Vector2(center.X + c0 * innerRadius, center.Y + s0 * innerRadius);
            var out0 = new Vector2(center.X + c0 * outerRadius, center.Y + s0 * outerRadius);
            var in1 = new Vector2(center.X + c1 * innerRadius, center.Y + s1 * innerRadius);
            var out1 = new Vector2(center.X + c1 * outerRadius, center.Y + s1 * outerRadius);

            idx = WriteVertex(verts, idx, in0.X, y, in0.Y);
            idx = WriteVertex(verts, idx, out0.X, y, out0.Y);
            idx = WriteVertex(verts, idx, out1.X, y, out1.Y);

            idx = WriteVertex(verts, idx, in0.X, y, in0.Y);
            idx = WriteVertex(verts, idx, out1.X, y, out1.Y);
            idx = WriteVertex(verts, idx, in1.X, y, in1.Y);
        }

        return verts;
    }

    private static bool[] BuildWalkableLookup(IReadOnlyList<TileRenderState> tiles, int width, int height)
    {
        var grid = new bool[width * height];
        foreach (var tile in tiles)
        {
            if (tile.X >= 0 && tile.X < width && tile.Y >= 0 && tile.Y < height)
            {
                grid[tile.Y * width + tile.X] = tile.IsWalkable;
            }
        }
        return grid;
    }

    private static Vector2 CastRayAgainstTiles(
        Vector2 origin,
        float angleRad,
        float range,
        bool[] walkableGrid,
        int width,
        int height)
    {
        var dir = new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad));
        var steps = Math.Max(1, (int)MathF.Ceiling(range * 8.0f));
        var lastClear = origin;

        for (var i = 1; i <= steps; i++)
        {
            var sample = origin + dir * (range * (i / (float)steps));
            var tx = (int)MathF.Floor(sample.X);
            var ty = (int)MathF.Floor(sample.Y);
            if (tx < 0 || tx >= width || ty < 0 || ty >= height || !walkableGrid[ty * width + tx])
            {
                return lastClear;
            }
            lastClear = sample;
        }

        return lastClear;
    }

    private static int WriteVertex(float[] buffer, int offset, float x, float y, float z)
    {
        buffer[offset] = x;
        buffer[offset + 1] = y;
        buffer[offset + 2] = z;
        return offset + 3;
    }
}
