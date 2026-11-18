using Coraline.AI;
using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.GameLogic;

public sealed class GameWorldState
{
    private readonly LevelMap _map;
    private readonly GameEventBus _eventBus;
    private readonly List<GuardAgent> _guards = [];
    private readonly HashSet<int> _collectedEyeIds = [];
    private readonly SneakStance _sneakStance = new();
    private readonly SprintStance _sprintStance = new();

    public Vector2 PlayerPosition { get; private set; }
    public float PlayerRotationRad { get; private set; }
    public IPlayerStance CurrentStance { get; private set; }
    public WorldDimension ActiveWorld { get; private set; } = WorldDimension.RealWorld;
    public float DetectionPercent { get; private set; }
    public bool IsGameWon { get; private set; }
    public bool IsGameOver { get; private set; }

    public GameWorldState(LevelMap map, GameEventBus eventBus)
    {
        _map = map;
        _eventBus = eventBus;
        CurrentStance = _sneakStance;
        PlayerPosition = new Vector2(map.PlayerSpawn.X + 0.5f, map.PlayerSpawn.Y + 0.5f);

        foreach (var g in map.Guards)
        {
            var points = g.Waypoints.Select(w => new Vector2(w.X + 0.5f, w.Y + 0.5f));
            _guards.Add(new GuardAgent(g.Id, points));
        }
    }

    public void ToggleWorld()
    {
        ActiveWorld = ActiveWorld == WorldDimension.RealWorld
            ? WorldDimension.OtherWorld
            : WorldDimension.RealWorld;
        _eventBus.Publish(new WorldShiftedEvent(ActiveWorld));
    }

    public void Step(float deltaTime, IInputProvider input)
    {
        if (IsGameWon || IsGameOver)
        {
            return;
        }

        if (input.IsToggleWorldPressed())
        {
            ToggleWorld();
        }

        CurrentStance = input.IsSprintHeld() ? _sprintStance : _sneakStance;
        MovePlayer(deltaTime, input.GetMovementAxes());
        UpdateDetectionAndGuards(deltaTime);
        CheckCollectiblesAndExtraction(input.IsInteractHeld());
    }

    public RenderSnapshot CreateSnapshot()
    {
        var entities = new List<EntityRenderState>
        {
            new(1, EntityType.Player, PlayerPosition, PlayerRotationRad, CurrentStance.Name, ActiveWorld),
            new(2, EntityType.SmallDoor, new Vector2(_map.SmallDoor.X + 0.5f, _map.SmallDoor.Y + 0.5f), 0f, "Door", WorldDimension.RealWorld)
        };

        foreach (var eye in _map.GhostEyes.Where(e => !_collectedEyeIds.Contains(e.Id)))
        {
            entities.Add(new EntityRenderState(
                10 + eye.Id,
                EntityType.GhostEye,
                new Vector2(eye.X + 0.5f, eye.Y + 0.5f),
                0f,
                "Idle",
                WorldDimension.OtherWorld));
        }

        var debugLines = new List<(Vector2 Start, Vector2 End)>();
        foreach (var guard in _guards)
        {
            entities.Add(new EntityRenderState(
                guard.EntityId,
                EntityType.Guard,
                guard.Position,
                guard.RotationRad,
                guard.State.ToString(),
                WorldDimension.OtherWorld));

            var lookEnd = guard.Position + new Vector2(MathF.Cos(guard.RotationRad), MathF.Sin(guard.RotationRad)) * 5.0f;
            debugLines.Add((guard.Position, lookEnd));
        }

        return new RenderSnapshot
        {
            ActiveWorld = ActiveWorld,
            DetectionPercent = DetectionPercent,
            CollectedEyes = _collectedEyeIds.Count,
            IsGameWon = IsGameWon,
            IsGameOver = IsGameOver,
            Entities = entities,
            DebugLines = debugLines
        };
    }

    private void MovePlayer(float deltaTime, Vector2 moveAxes)
    {
        if (moveAxes.LengthSquared <= 0.0001f)
        {
            return;
        }

        var dir = moveAxes.Normalized();
        PlayerRotationRad = MathF.Atan2(dir.Y, dir.X);
        var candidate = PlayerPosition + dir * CurrentStance.MoveSpeed * deltaTime;
        if (_map.IsWalkable((int)MathF.Floor(candidate.X), (int)MathF.Floor(candidate.Y), ActiveWorld))
        {
            PlayerPosition = candidate;
            if (CurrentStance.NoiseRadius > 0.0f)
            {
                _eventBus.Publish(new NoiseEmittedEvent(PlayerPosition, CurrentStance.NoiseRadius, CurrentStance.Name));
            }
        }
    }

    private void UpdateDetectionAndGuards(float deltaTime)
    {
        var spotted = false;
        foreach (var guard in _guards)
        {
            if (LineOfSight.HasLineOfSight(_map, guard.Position, guard.RotationRad, PlayerPosition, 6.0f, MathHelper.DegreesToRadians(75f), ActiveWorld))
            {
                spotted = true;
            }

            guard.Update(deltaTime, _map, PlayerPosition, DetectionPercent);
            if (guard.State == GuardBehaviorState.AlertChase && (guard.Position - PlayerPosition).Length < 0.65f)
            {
                IsGameOver = true;
            }
        }

        var rate = spotted ? 45.0f : -20.0f;
        DetectionPercent = Math.Clamp(DetectionPercent + rate * deltaTime, 0.0f, 100.0f);
    }

    private void CheckCollectiblesAndExtraction(bool interactHeld)
    {
        if (ActiveWorld == WorldDimension.OtherWorld)
        {
            foreach (var eye in _map.GhostEyes)
            {
                var eyePos = new Vector2(eye.X + 0.5f, eye.Y + 0.5f);
                if (!_collectedEyeIds.Contains(eye.Id) && (eyePos - PlayerPosition).Length < 0.85f)
                {
                    _collectedEyeIds.Add(eye.Id);
                    _eventBus.Publish(new GhostEyeCollectedEvent(eye.Id, _collectedEyeIds.Count));
                }
            }
        }

        var doorPos = new Vector2(_map.SmallDoor.X + 0.5f, _map.SmallDoor.Y + 0.5f);
        if (interactHeld && _collectedEyeIds.Count >= 3 && (doorPos - PlayerPosition).Length < 1.25f)
        {
            IsGameWon = true;
        }
    }
}
