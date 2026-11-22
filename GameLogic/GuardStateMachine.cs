using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;

namespace Coraline.AI;

public enum GuardBehaviorState
{
    Patrol,
    Investigating,
    AlertChase
}

public sealed class GuardAgent
{
    private readonly List<Vector2> _waypoints;
    private int _waypointIndex;
    private Vector2? _investigationTarget;
    private float _investigationWaitTimer;

    public int EntityId { get; }
    public Vector2 Position { get; private set; }
    public float RotationRad { get; private set; }
    public GuardBehaviorState State { get; private set; } = GuardBehaviorState.Patrol;
    public int WaypointIndex => _waypointIndex;
    public IReadOnlyList<Vector2> Waypoints => _waypoints;
    public Vector2? InvestigationTarget => _investigationTarget;

    public void Teleport(Vector2 position) => Position = position;

    public void OnNoiseHeard(Vector2 noiseOrigin)
    {
        if (State == GuardBehaviorState.AlertChase) return;
        State = GuardBehaviorState.Investigating;
        _investigationTarget = noiseOrigin;
        _investigationWaitTimer = 1.2f;
    }

    public GuardAgent(int entityId, IEnumerable<Vector2> waypoints)
    {
        EntityId = entityId;
        _waypoints = waypoints.ToList();
        Position = _waypoints.Count > 0 ? _waypoints[0] : Vector2.Zero;
        _waypointIndex = _waypoints.Count > 1 ? 1 : 0;
    }

    public void Update(
        float deltaTime,
        LevelMap map,
        Vector2 playerPosition,
        float detectionPercent)
    {
        if (detectionPercent >= 100.0f)
        {
            State = GuardBehaviorState.AlertChase;
        }
        else if (State != GuardBehaviorState.Investigating)
        {
            State = GuardBehaviorState.Patrol;
        }

        if (State == GuardBehaviorState.AlertChase)
        {
            StepChase(deltaTime, map, playerPosition);
        }
        else if (State == GuardBehaviorState.Investigating)
        {
            StepInvestigating(deltaTime);
        }
        else
        {
            StepPatrol(deltaTime);
        }
    }

    private void StepInvestigating(float deltaTime)
    {
        if (_investigationTarget is null)
        {
            State = GuardBehaviorState.Patrol;
            return;
        }

        var target = _investigationTarget.Value;
        if ((target - Position).LengthSquared > 0.09f)
        {
            MoveTowards(target, 2.4f, deltaTime);
        }
        else
        {
            _investigationWaitTimer -= deltaTime;
            if (_investigationWaitTimer <= 0f)
            {
                _investigationTarget = null;
                State = GuardBehaviorState.Patrol;
            }
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
