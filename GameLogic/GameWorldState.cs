using Coraline.AI;
using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.GameLogic;

public sealed class GameWorldState
{
    private const float GuardVisionRange = 6.0f;
    private const float GuardProximityRange = 1.25f;
    private static readonly float GuardFovRad = MathHelper.DegreesToRadians(75f);

    private readonly LevelMap _map;
    private readonly GameEventBus _eventBus;
    private readonly List<GuardAgent> _guards = [];
    private readonly HashSet<int> _collectedEyeIds = [];
    private readonly SneakStance _sneakStance = new();
    private readonly SprintStance _sprintStance = new();
    private float _distractionCooldown;

    public Vector2 PlayerPosition { get; private set; }
    public float PlayerRotationRad { get; private set; }
    public IPlayerStance CurrentStance { get; private set; }
    public WorldDimension ActiveWorld { get; private set; } = WorldDimension.RealWorld;
    public float DetectionPercent { get; private set; }
    public bool IsGameWon { get; private set; }
    public bool IsGameOver { get; private set; }
    public IReadOnlyList<GuardAgent> Guards => _guards;

    public void SetDetectionPercent(float percent) => DetectionPercent = Math.Clamp(percent, 0f, 100f);

    public GameWorldState(LevelMap map, GameEventBus eventBus)
    {
        _map = map;
        _eventBus = eventBus;
        CurrentStance = _sneakStance;
        PlayerPosition = new Vector2(map.PlayerSpawn.X + 0.5f, map.PlayerSpawn.Y + 0.5f);
        foreach (var g in map.Guards)
            _guards.Add(new GuardAgent(g.Id, g.Waypoints.Select(w => new Vector2(w.X + 0.5f, w.Y + 0.5f))));
    }

    public void ToggleWorld()
    {
        ActiveWorld = ActiveWorld == WorldDimension.RealWorld ? WorldDimension.OtherWorld : WorldDimension.RealWorld;
        _eventBus.Publish(new WorldShiftedEvent(ActiveWorld));
    }

    public void Step(float deltaTime, IInputProvider input)
    {
        if (IsGameWon || IsGameOver) return;
        if (input.IsToggleWorldPressed()) ToggleWorld();

        if (_distractionCooldown > 0f) _distractionCooldown -= deltaTime;
        if (input.IsDistractionPressed() && _distractionCooldown <= 0f) ThrowDistraction();

        CurrentStance = input.IsSprintHeld() ? _sprintStance : _sneakStance;
        MovePlayer(deltaTime, input.GetMovementAxes());
        UpdateDetectionAndGuards(deltaTime);
        CheckCollectiblesAndExtraction(input.IsInteractHeld());
    }

    public RenderSnapshot CreateSnapshot()
    {
        var isReal = ActiveWorld == WorldDimension.RealWorld;
        var tiles = _map.Tiles.Select(t => new TileRenderState(t.X, t.Y, isReal ? t.RealWorldWalkable : t.OtherWorldWalkable, t.RealWorldWalkable != t.OtherWorldWalkable)).ToList();

        var entities = new List<EntityRenderState>
        {
            new(1, EntityType.Player, PlayerPosition, PlayerRotationRad, CurrentStance.Name, ActiveWorld),
            new(2, EntityType.SmallDoor, new Vector2(_map.SmallDoor.X + 0.5f, _map.SmallDoor.Y + 0.5f), 0f, "Door", ActiveWorld)
        };
        foreach (var eye in _map.GhostEyes.Where(e => !_collectedEyeIds.Contains(e.Id)))
            entities.Add(new EntityRenderState(10 + eye.Id, EntityType.GhostEye, new Vector2(eye.X + 0.5f, eye.Y + 0.5f), 0f, "Idle", WorldDimension.OtherWorld));
        foreach (var guard in _guards)
            entities.Add(new EntityRenderState(guard.EntityId, EntityType.Guard, guard.Position, guard.RotationRad, guard.State.ToString(), WorldDimension.OtherWorld));

        return new RenderSnapshot
        {
            ActiveWorld = ActiveWorld, DetectionPercent = DetectionPercent, CollectedEyes = _collectedEyeIds.Count,
            IsGameWon = IsGameWon, IsGameOver = IsGameOver, Tiles = tiles, Entities = entities, DebugLines = BuildGuardDebugLines()
        };
    }

    private List<(Vector2 Start, Vector2 End)> BuildGuardDebugLines()
    {
        var lines = new List<(Vector2 Start, Vector2 End)>();
        if (ActiveWorld != WorldDimension.OtherWorld) return lines;

        var halfFov = GuardFovRad * 0.5f;
        foreach (var guard in _guards)
        {
            var leftEnd = LineOfSight.CastVisionRay(_map, guard.Position, guard.RotationRad - halfFov, GuardVisionRange, ActiveWorld);
            var centerEnd = LineOfSight.CastVisionRay(_map, guard.Position, guard.RotationRad, GuardVisionRange, ActiveWorld);
            var rightEnd = LineOfSight.CastVisionRay(_map, guard.Position, guard.RotationRad + halfFov, GuardVisionRange, ActiveWorld);

            lines.AddRange([(guard.Position, leftEnd), (guard.Position, centerEnd), (guard.Position, rightEnd), (leftEnd, centerEnd), (centerEnd, rightEnd)]);

            var path = AStarPathfinder.FindPath(_map, guard.Position, PlayerPosition, WorldDimension.OtherWorld);
            for (var i = 0; i < path.Count - 1; i++) lines.Add((path[i], path[i + 1]));
        }

        return lines;
    }

    public void TeleportPlayer(Vector2 position) => PlayerPosition = position;

    private void MovePlayer(float deltaTime, Vector2 moveAxes)
    {
        if (moveAxes.LengthSquared <= 0.0001f) return;

        var dir = moveAxes.Normalized();
        PlayerRotationRad = MathF.Atan2(dir.Y, dir.X);
        var step = dir * CurrentStance.MoveSpeed * deltaTime;
        var startPos = PlayerPosition;

        var candidate = startPos + step;
        if (CanOccupy(candidate))
        {
            PlayerPosition = candidate;
        }
        else
        {
            var canX = step.X != 0f && CanOccupy(new Vector2(startPos.X + step.X, startPos.Y));
            var canY = step.Y != 0f && CanOccupy(new Vector2(startPos.X, startPos.Y + step.Y));
            var preferX = MathF.Abs(step.X) >= MathF.Abs(step.Y);

            if (preferX) PlayerPosition = canX ? new Vector2(startPos.X + step.X, startPos.Y) : (canY ? new Vector2(startPos.X, startPos.Y + step.Y) : startPos);
            else PlayerPosition = canY ? new Vector2(startPos.X, startPos.Y + step.Y) : (canX ? new Vector2(startPos.X + step.X, startPos.Y) : startPos);
        }

        if (PlayerPosition != startPos && CurrentStance.NoiseRadius > 0.0f)
        {
            var noise = new NoiseEmittedEvent(PlayerPosition, CurrentStance.NoiseRadius, CurrentStance.Name);
            _eventBus.Publish(noise);
            NotifyGuardsOfNoise(noise);
        }
    }

    private void ThrowDistraction()
    {
        _distractionCooldown = 1.0f;
        var landing = LineOfSight.CastVisionRay(_map, PlayerPosition, PlayerRotationRad, 3.5f, ActiveWorld);
        var noise = new NoiseEmittedEvent(landing, 8.0f, "Distraction");
        _eventBus.Publish(noise);
        NotifyGuardsOfNoise(noise);
    }

    private void NotifyGuardsOfNoise(NoiseEmittedEvent noise)
    {
        foreach (var guard in _guards)
            if ((guard.Position - noise.Origin).Length <= noise.Radius) guard.OnNoiseHeard(noise.Origin);
    }

    private bool CanOccupy(Vector2 pos) =>
        _map.IsWalkable((int)MathF.Floor(pos.X), (int)MathF.Floor(pos.Y), ActiveWorld);

    private void UpdateDetectionAndGuards(float deltaTime)
    {
        var spotted = false;
        foreach (var guard in _guards)
        {
            var inProximity = (guard.Position - PlayerPosition).Length <= GuardProximityRange;
            var inLos = ActiveWorld == WorldDimension.OtherWorld &&
                        LineOfSight.HasLineOfSight(_map, guard.Position, guard.RotationRad, PlayerPosition, GuardVisionRange, GuardFovRad, WorldDimension.OtherWorld);

            if (inProximity || inLos) spotted = true;

            guard.Update(deltaTime, _map, PlayerPosition, DetectionPercent);
            if (guard.State == GuardBehaviorState.AlertChase && (guard.Position - PlayerPosition).Length < 0.65f)
            {
                IsGameOver = true;
            }
        }

        var prev = DetectionPercent;
        var rate = spotted ? 45.0f : -20.0f;
        DetectionPercent = Math.Clamp(DetectionPercent + rate * deltaTime, 0.0f, 100.0f);
        if (prev < 100.0f && DetectionPercent >= 100.0f) _eventBus.Publish(new NoiseEmittedEvent(PlayerPosition, 0f, "Alert"));
    }

    private void CheckCollectiblesAndExtraction(bool interactHeld)
    {
        if (ActiveWorld == WorldDimension.OtherWorld)
        {
            foreach (var eye in _map.GhostEyes.Where(e => !_collectedEyeIds.Contains(e.Id) && (new Vector2(e.X + 0.5f, e.Y + 0.5f) - PlayerPosition).Length < 0.85f))
            {
                _collectedEyeIds.Add(eye.Id);
                _eventBus.Publish(new GhostEyeCollectedEvent(eye.Id, _collectedEyeIds.Count));
            }
        }

        var doorPos = new Vector2(_map.SmallDoor.X + 0.5f, _map.SmallDoor.Y + 0.5f);
        if (interactHeld && _collectedEyeIds.Count >= 3 && (doorPos - PlayerPosition).Length < 1.25f) IsGameWon = true;
    }
}
