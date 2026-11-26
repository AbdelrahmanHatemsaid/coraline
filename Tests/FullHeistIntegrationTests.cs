using Coraline.AI;
using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class FullHeistIntegrationTests
{
    private sealed class MockInput : IInputProvider
    {
        public Vector2 Move { get; set; } = Vector2.Zero;
        public bool Sprint { get; set; }
        public bool Toggle { get; set; }
        public bool Interact { get; set; }
        public bool Distract { get; set; }

        public Vector2 GetMovementAxes() => Move;
        public bool IsSprintHeld() => Sprint;
        public bool IsToggleWorldPressed() => Toggle;
        public bool IsInteractHeld() => Interact;
        public bool IsDistractionPressed() => Distract;
    }

    private static (GameWorldState World, LevelMap Map, GameEventBus Bus) CreateWorld()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);
        var bus = new GameEventBus();
        return (new GameWorldState(map, bus), map, bus);
    }

    [Fact]
    public void Heist_FullPlaythrough_ShiftDimensionCollectEyesAndExtract()
    {
        var (world, _, _) = CreateWorld();
        var input = new MockInput();

        Assert.Equal(WorldDimension.RealWorld, world.ActiveWorld);

        // 1. Shift to Other World using Seeing Stone
        input.Toggle = true;
        world.Step(0.016f, input);
        input.Toggle = false;
        Assert.Equal(WorldDimension.OtherWorld, world.ActiveWorld);

        // 2. Collect all 3 Ghost Eyes across the 3 rooms: (5,13), (12,3), (21,12)
        Vector2[] eyePositions = [new(5.5f, 13.5f), new(12.5f, 3.5f), new(21.5f, 12.5f)];
        foreach (var pos in eyePositions)
        {
            world.TeleportPlayer(pos);
            world.Step(0.016f, input);
        }

        var snapshot = world.CreateSnapshot();
        Assert.Equal(3, snapshot.CollectedEyes);
        Assert.False(snapshot.IsGameWon);

        // 3. Navigate back to Drawing Room Small Door (1.5, 7.5) and hold interact
        world.TeleportPlayer(new Vector2(1.5f, 7.5f));
        input.Interact = true;

        world.Step(0.50f, input);
        Assert.False(world.IsGameWon); // Partial hold must not trigger escape

        world.Step(0.55f, input);
        Assert.True(world.IsGameWon);
        Assert.False(world.IsGameOver);
    }

    [Fact]
    public void Heist_DimensionalDoorwayTradeoff_WalkabilityDiffersByDimension()
    {
        var (world, map, _) = CreateWorld();

        // Doorway 1 at (8,4): Open in RealWorld, bricked in OtherWorld
        Assert.True(map.IsWalkable(8, 4, WorldDimension.RealWorld));
        Assert.False(map.IsWalkable(8, 4, WorldDimension.OtherWorld));

        // Doorway 3 at (16,5): Bricked in RealWorld, open in OtherWorld
        Assert.False(map.IsWalkable(16, 5, WorldDimension.RealWorld));
        Assert.True(map.IsWalkable(16, 5, WorldDimension.OtherWorld));

        // Tile differences reflected in snapshot
        var snap = world.CreateSnapshot();
        var d1 = snap.Tiles.First(t => t.X == 8 && t.Y == 4);
        var d3 = snap.Tiles.First(t => t.X == 16 && t.Y == 5);
        Assert.True(d1.DiffersBetweenWorlds);
        Assert.True(d3.DiffersBetweenWorlds);
    }

    [Fact]
    public void Heist_DistractionThrow_DivertsPatrollingGuard()
    {
        var (world, _, _) = CreateWorld();
        var input = new MockInput();
        var guard = world.Guards[0];

        Assert.Equal(GuardBehaviorState.Patrol, guard.State);

        // Position player in Kitchen near guard and throw distraction rock
        world.TeleportPlayer(new Vector2(10.5f, 6.5f));
        input.Distract = true;
        world.Step(0.016f, input);

        Assert.Equal(GuardBehaviorState.Investigating, guard.State);
        Assert.NotNull(guard.InvestigationTarget);
    }

    [Fact]
    public void Heist_GuardAlertChase_PursuesPlayerAndTriggersGameOverOnContact()
    {
        var (world, _, _) = CreateWorld();
        var input = new MockInput();
        var guard = world.Guards[0];

        // Shift to OtherWorld and place player in direct guard LOS
        input.Toggle = true;
        world.Step(0.016f, input);
        input.Toggle = false;

        world.TeleportPlayer(guard.Position + new Vector2(1.5f, 0f));
        world.SetDetectionPercent(100f);

        // Guard should switch to AlertChase
        world.Step(0.016f, input);
        Assert.Equal(GuardBehaviorState.AlertChase, guard.State);

        // Move guard right onto player to verify Caught GameOver
        guard.Teleport(world.PlayerPosition + new Vector2(0.2f, 0f));
        world.Step(0.016f, input);

        Assert.True(world.IsGameOver);
        Assert.False(world.IsGameWon);
    }

    [Fact]
    public void Heist_SnapshotIntegrity_MatchesGameRules()
    {
        var (world, _, _) = CreateWorld();
        var snap = world.CreateSnapshot();

        Assert.NotNull(snap.Tiles);
        Assert.Equal(24 * 16, snap.Tiles.Count);

        var player = snap.Entities.First(e => e.Type == EntityType.Player);
        Assert.Equal(world.PlayerPosition, player.Position);

        var door = snap.Entities.First(e => e.Type == EntityType.SmallDoor);
        Assert.Equal(new Vector2(1.5f, 7.5f), door.Position);

        var eyes = snap.Entities.Where(e => e.Type == EntityType.GhostEye).ToList();
        Assert.Equal(3, eyes.Count);
        Assert.All(eyes, eye => Assert.Equal(WorldDimension.OtherWorld, eye.VisibleIn));

        Assert.Equal(100f, snap.SeeingStoneChargePercent);
        Assert.Equal(0.0f, snap.DetectionPercent);
    }
}
