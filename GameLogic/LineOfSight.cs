using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.GameLogic;

public static class LineOfSight
{
    public static bool HasLineOfSight(
        LevelMap map,
        Vector2 observerPos,
        float observerFacingRad,
        Vector2 targetPos,
        float maxDistance,
        float fovRadians,
        WorldDimension dimension)
    {
        if (maxDistance <= 0f || fovRadians <= 0f)
        {
            return false;
        }

        var toTarget = targetPos - observerPos;
        var distance = toTarget.Length;
        if (distance > maxDistance)
        {
            return false;
        }

        if (distance > 0.0001f)
        {
            var forward = new Vector2(MathF.Cos(observerFacingRad), MathF.Sin(observerFacingRad));
            var dir = toTarget / distance;
            var dot = Vector2.Dot(forward, dir);
            var minDot = MathF.Cos(fovRadians * 0.5f);
            if (dot < minDot)
            {
                return false;
            }
        }

        return IsRayClear(map, observerPos, targetPos, dimension);
    }

    public static bool IsRayClear(LevelMap map, Vector2 start, Vector2 end, WorldDimension dimension)
    {
        var delta = end - start;
        var dist = delta.Length;
        if (dist <= 0.001f)
        {
            return true;
        }

        var steps = Math.Max(1, (int)MathF.Ceiling(dist * 4.0f));
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var sample = Vector2.Lerp(start, end, t);
            var tileX = (int)MathF.Floor(sample.X);
            var tileY = (int)MathF.Floor(sample.Y);
            if (!map.IsWalkable(tileX, tileY, dimension))
            {
                return false;
            }
        }

        return true;
    }

    public static Vector2 CastVisionRay(
        LevelMap map,
        Vector2 start,
        float angleRad,
        float maxDistance,
        WorldDimension dimension)
    {
        var dir = new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad));
        var steps = Math.Max(1, (int)MathF.Ceiling(maxDistance * 6.0f));
        var lastValid = start;

        for (var i = 1; i <= steps; i++)
        {
            var sample = start + dir * (maxDistance * (i / (float)steps));
            var tileX = (int)MathF.Floor(sample.X);
            var tileY = (int)MathF.Floor(sample.Y);
            if (!map.IsWalkable(tileX, tileY, dimension))
            {
                return lastValid;
            }

            lastValid = sample;
        }

        return lastValid;
    }
}
