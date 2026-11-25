using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.AI;

public static class AStarPathfinder
{
    private static readonly (int Dx, int Dy)[] Neighbors =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1)
    ];

    public static List<Vector2> FindPath(
        GameLogic.LevelMap map,
        Vector2 startWorld,
        Vector2 goalWorld,
        WorldDimension dimension)
    {
        var start = ((int)MathF.Floor(startWorld.X), (int)MathF.Floor(startWorld.Y));
        var goal = ((int)MathF.Floor(goalWorld.X), (int)MathF.Floor(goalWorld.Y));

        if (!map.IsWalkable(goal.Item1, goal.Item2, dimension))
        {
            var fallback = FindNearestWalkable(map, goal, start, dimension);
            if (fallback is null) return [];
            goal = fallback.Value;
        }

        if (!map.IsWalkable(start.Item1, start.Item2, dimension))
        {
            var fallbackStart = FindNearestWalkable(map, start, goal, dimension);
            if (fallbackStart is null) return [];
            start = fallbackStart.Value;
        }

        if (start == goal) return [new Vector2(start.Item1 + 0.5f, start.Item2 + 0.5f)];

        var openSet = new PriorityQueue<(int X, int Y), int>();
        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
        var gScore = new Dictionary<(int X, int Y), int> { [start] = 0 };

        openSet.Enqueue(start, Heuristic(start, goal));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var (dx, dy) in Neighbors)
            {
                var next = (current.X + dx, current.Y + dy);
                if (!map.IsWalkable(next.Item1, next.Item2, dimension))
                {
                    continue;
                }

                var tentativeG = gScore[current] + 1;
                if (!gScore.TryGetValue(next, out var existingG) || tentativeG < existingG)
                {
                    cameFrom[next] = current;
                    gScore[next] = tentativeG;
                    openSet.Enqueue(next, tentativeG + Heuristic(next, goal));
                }
            }
        }

        return [];
    }

    private static (int X, int Y)? FindNearestWalkable(
        GameLogic.LevelMap map,
        (int X, int Y) target,
        (int X, int Y) reference,
        WorldDimension dimension)
    {
        return Neighbors
            .Select(n => (X: target.X + n.Dx, Y: target.Y + n.Dy))
            .Where(n => map.IsWalkable(n.X, n.Y, dimension))
            .OrderBy(n => Heuristic(n, reference))
            .Select(n => ((int X, int Y)?)n)
            .FirstOrDefault();
    }

    private static int Heuristic((int X, int Y) a, (int X, int Y) b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static List<Vector2> ReconstructPath(
        Dictionary<(int X, int Y), (int X, int Y)> cameFrom,
        (int X, int Y) current)
    {
        var path = new List<Vector2> { new(current.X + 0.5f, current.Y + 0.5f) };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(new Vector2(current.X + 0.5f, current.Y + 0.5f));
        }

        path.Reverse();
        return path;
    }
}
