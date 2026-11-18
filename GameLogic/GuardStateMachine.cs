using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;

namespace Coraline.AI;

public enum GuardBehaviorState
{
    Patrol,
    AlertChase
}

public sealed class GuardAgent
{
    private readonly List<Vector2> _waypoints;
    private int _waypointIndex;

    public int EntityId { get; }
    public Vector2 Position { get; private set; }
    public float RotationRad { get; private set; }
    public GuardBehaviorState State { get; private set; } = GuardBehaviorState.Patrol;

    public GuardAgent(int entityId, IEnumerable<Vector2> waypoints)
    {
        EntityId = entityId;
        _waypoints = waypoints.ToList();
        Position = _waypoints.Count > 0 ? _waypoints[0] : Vector2.Zero;
    }

    public void Update(
        float deltaTime,
        LevelMap map,
        Vector2 playerPosition,
        float detectionPercent)
    {
        State = detectionPercent >= 100.0f
            ? GuardBehaviorState.AlertChase
            : GuardBehaviorState.Patrol;

        if (State == GuardBehaviorState.AlertChase)
        {
            StepChase(deltaTime, map, playerPosition);
        }
        else
        {
            StepPatrol(deltaTime);
        }
    }

    private void StepPatrol(float deltaTime)
    {
        if (_waypoints.Count == 0)
        {
            return;
        }

        var target = _waypoints[_waypointIndex];
        MoveTowards(target, 2.0f, deltaTime);
        if ((target - Position).LengthSquared < 0.04f)
        {
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Count;
        }
    }

    private void StepChase(float deltaTime, LevelMap map, Vector2 playerPosition)
    {
        var path = AStarPathfinder.FindPath(map, Position, playerPosition, WorldDimension.OtherWorld);
        var nextTarget = path.Count > 1 ? path[1] : playerPosition;
        MoveTowards(nextTarget, 3.6f, deltaTime);
    }

    private void MoveTowards(Vector2 target, float speed, float deltaTime)
    {
        var delta = target - Position;
        var dist = delta.Length;
        if (dist <= 0.001f)
        {
            return;
        }

        var dir = delta / dist;
        RotationRad = MathF.Atan2(dir.Y, dir.X);
        var step = MathF.Min(dist, speed * deltaTime);
        Position += dir * step;
    }
}
