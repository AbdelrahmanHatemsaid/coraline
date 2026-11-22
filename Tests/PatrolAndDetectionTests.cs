using Coraline.AI;
using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class PatrolAndDetectionTests
{
    private sealed class MockInputProvider : IInputProvider
    {
        public Vector2 MovementAxes { get; set; } = Vector2.Zero;
        public bool SprintHeld { get; set; }
        public bool ToggleWorldPressed { get; set; }
        public bool InteractHeld { get; set; }
        public bool DistractionPressed { get; set; }

        public Vector2 GetMovementAxes() => MovementAxes;
        public bool IsSprintHeld() => SprintHeld;
        public bool IsToggleWorldPressed() => ToggleWorldPressed;
        public bool IsInteractHeld() => InteractHeld;
        public bool IsDistractionPressed() => DistractionPressed;
    }

    private static (GameWorldState World, LevelMap Map, GameEventBus Bus) CreateTestWorld()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);
        var bus = new GameEventBus();
        var world = new GameWorldState(map, bus);
        return (world, map, bus);
    }

    [Fact]
    public void Guard_PatrolsWaypoints_AdvancesAlongPath()
    {
        var waypoints = new List<Vector2> { new(2f, 2f), new(4f, 2f) };
        var guard = new GuardAgent(1, waypoints);

        Assert.Equal(GuardBehaviorState.Patrol, guard.State);
        Assert.Equal(new Vector2(2f, 2f), guard.Position);

        // Advance 0.5s at patrol speed (2.0f units/s) -> moves 1.0 unit towards (4, 2)
        var map = new LevelMap { Width = 10, Height = 10 };
        guard.Update(0.5f, map, new Vector2(9f, 9f), 0f);

        Assert.True(guard.Position.X > 2.5f);
        Assert.Equal(0f, guard.RotationRad); // Facing +X
        Assert.Equal(1, guard.WaypointIndex); // Targeting waypoint index 1
    }

    [Fact]
    public void Detection_RisesAt45PercentPerSec_WhenSpottedInOtherWorld()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld(); // Shift to OtherWorld where guard vision is active
        Assert.Equal(WorldDimension.OtherWorld, world.ActiveWorld);

        // Position guard at (2.5, 3.5) facing +X and player at (4.5, 3.5)
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(2.5f, 3.5f));
        world.TeleportPlayer(new Vector2(4.5f, 3.5f));

        var input = new MockInputProvider();
        world.Step(0.5f, input);

        // 45% * 0.5s = 22.5%
        Assert.InRange(world.DetectionPercent, 22.0f, 23.0f);
    }

    [Fact]
    public void Detection_DoesNotRise_WhenPlayerInRealWorldOutsideProximity()
    {
        var (world, _, _) = CreateTestWorld();
        Assert.Equal(WorldDimension.RealWorld, world.ActiveWorld);

        // Position player 3 units ahead of guard (outside proximity of 1.25)
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(2.5f, 3.5f));
        world.TeleportPlayer(new Vector2(5.5f, 3.5f));

        var input = new MockInputProvider();
        world.Step(0.5f, input);

        // In RealWorld, Other World guard cannot see Coraline across the veil
        Assert.Equal(0.0f, world.DetectionPercent);
    }

    [Fact]
    public void Detection_ProximityTrigger_SpotsPlayerInAnyDimensionOrBehindGuard()
    {
        var (world, _, _) = CreateTestWorld();
        Assert.Equal(WorldDimension.RealWorld, world.ActiveWorld);

        // Position player 0.8 units behind guard in Real World
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(3.0f, 3.5f));
        world.TeleportPlayer(new Vector2(2.2f, 3.5f)); // 0.8 units behind (guard faces +X)

        var input = new MockInputProvider();
        world.Step(0.5f, input);

        // Sensed through proximity across the veil
        Assert.True(world.DetectionPercent > 0.0f);
        Assert.InRange(world.DetectionPercent, 22.0f, 23.0f);
    }

    [Fact]
    public void Detection_DecaysAt20PercentPerSec_WhenUnspotted()
    {
        var (world, _, _) = CreateTestWorld();
        world.SetDetectionPercent(60.0f);

        // Place player safely behind room wall away from guards
        world.TeleportPlayer(new Vector2(2.5f, 2.5f));
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(18.5f, 12.5f));

        var input = new MockInputProvider();
        world.Step(1.0f, input);

        // 60% - 20% * 1.0s = 40%
        Assert.InRange(world.DetectionPercent, 39.5f, 40.5f);

        world.Step(3.0f, input);
        // Clamps at 0%
        Assert.Equal(0.0f, world.DetectionPercent);
    }

    [Fact]
    public void Detection_ClampsAt100Percent()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld();
        world.SetDetectionPercent(90.0f);

        var guard = world.Guards[0];
        guard.Teleport(new Vector2(2.5f, 3.5f));
        world.TeleportPlayer(new Vector2(4.5f, 3.5f));

        var input = new MockInputProvider();
        world.Step(1.0f, input);

        Assert.Equal(100.0f, world.DetectionPercent);
    }

    [Fact]
    public void GuardFSM_TransitionsToAlertChase_WhenDetectionReaches100()
    {
        var (world, _, _) = CreateTestWorld();
        var guard = world.Guards[0];

        world.SetDetectionPercent(50.0f);
        world.Step(0.05f, new MockInputProvider());
        Assert.Equal(GuardBehaviorState.Patrol, guard.State);

        world.SetDetectionPercent(100.0f);
        world.Step(0.05f, new MockInputProvider());
        Assert.Equal(GuardBehaviorState.AlertChase, guard.State);
    }

    [Fact]
    public void GameOver_Triggered_WhenAlertGuardCatchesPlayer()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld();
        world.SetDetectionPercent(100.0f);

        // Guard close to player (< 0.65f)
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(3.0f, 3.0f));
        world.TeleportPlayer(new Vector2(3.3f, 3.0f)); // 0.3 units away

        world.Step(0.1f, new MockInputProvider());

        Assert.True(world.IsGameOver);
    }
}
