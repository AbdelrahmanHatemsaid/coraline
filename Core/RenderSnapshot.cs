using OpenTK.Mathematics;

namespace Coraline.Core;

public enum WorldDimension
{
    RealWorld,
    OtherWorld
}

public enum EntityType
{
    Player,
    Guard,
    GhostEye,
    SmallDoor,
    Distraction,
    WebTrap
}

public readonly record struct EntityRenderState(
    int EntityId,
    EntityType Type,
    Vector2 Position,
    float RotationRad,
    string StateTag,
    WorldDimension VisibleIn
);

public sealed class RenderSnapshot
{
    public WorldDimension ActiveWorld { get; init; } = WorldDimension.RealWorld;
    public float DetectionPercent { get; init; }
    public int CollectedEyes { get; init; }
    public bool IsGameWon { get; init; }
    public bool IsGameOver { get; init; }
    public IReadOnlyList<EntityRenderState> Entities { get; init; } = [];
    public IReadOnlyList<(Vector2 Start, Vector2 End)> DebugLines { get; init; } = [];
}
