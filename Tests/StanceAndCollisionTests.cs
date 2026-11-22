using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class StanceAndCollisionTests
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
    public void Stance_DefaultIsSneakWithZeroNoise()
    {
        var (world, _, _) = CreateTestWorld();
        Assert.Equal("Sneak", world.CurrentStance.Name);
        Assert.Equal(2.8f, world.CurrentStance.MoveSpeed);
        Assert.Equal(0.0f, world.CurrentStance.NoiseRadius);
    }

    [Fact]
    public void Stance_SprintHeldSwitchesToSprintStance()
    {
        var (world, _, _) = CreateTestWorld();
        var input = new MockInputProvider { SprintHeld = true };

        world.Step(0.1f, input);

        Assert.Equal("Sprint", world.CurrentStance.Name);
        Assert.Equal(5.4f, world.CurrentStance.MoveSpeed);
        Assert.Equal(6.0f, world.CurrentStance.NoiseRadius);
    }

    [Fact]
    public void Stance_ReleasingSprintRevertsToSneak()
    {
        var (world, _, _) = CreateTestWorld();
        var input = new MockInputProvider { SprintHeld = true };
        world.Step(0.1f, input);
        Assert.Equal("Sprint", world.CurrentStance.Name);

        input.SprintHeld = false;
        world.Step(0.1f, input);
        Assert.Equal("Sneak", world.CurrentStance.Name);
    }

    [Fact]
    public void Movement_SprintMovesFasterThanSneak()
    {
        var (worldSneak, _, _) = CreateTestWorld();
        var (worldSprint, _, _) = CreateTestWorld();
        var sneakInput = new MockInputProvider { MovementAxes = new Vector2(1f, 0f) };
        var sprintInput = new MockInputProvider { MovementAxes = new Vector2(1f, 0f), SprintHeld = true };

        worldSneak.Step(0.2f, sneakInput);
        worldSprint.Step(0.2f, sprintInput);

        var sneakDist = worldSneak.PlayerPosition.X - 2.5f;
        var sprintDist = worldSprint.PlayerPosition.X - 2.5f;

        Assert.True(sprintDist > sneakDist);
        Assert.InRange(sneakDist, 0.55f, 0.57f);
        Assert.InRange(sprintDist, 1.07f, 1.09f);
    }

    [Fact]
    public void Movement_SprintEmitsNoise_SneakDoesNot()
    {
        var (world, _, bus) = CreateTestWorld();
        var noiseEvents = new List<NoiseEmittedEvent>();
        bus.Subscribe<NoiseEmittedEvent>(e => noiseEvents.Add(e));

        var input = new MockInputProvider { MovementAxes = new Vector2(1f, 0f), SprintHeld = false };
        world.Step(0.1f, input);
        Assert.Empty(noiseEvents);

        input.SprintHeld = true;
        world.Step(0.1f, input);
        Assert.Single(noiseEvents);
        Assert.Equal("Sprint", noiseEvents[0].SourceTag);
        Assert.Equal(6.0f, noiseEvents[0].Radius);
    }

    [Fact]
    public void Collision_MovementBlockedBySolidWall()
    {
        var (world, _, _) = CreateTestWorld();
        // Drawing room: tile (2, 0) is wall, tile (2, 1) is floor.
        world.TeleportPlayer(new Vector2(2.5f, 1.1f));
        var input = new MockInputProvider { MovementAxes = new Vector2(0f, -1f) };

        world.Step(0.1f, input);

        // Player cannot enter unwalkable y=0 wall tile
        Assert.True(world.PlayerPosition.Y >= 1.0f);
        Assert.Equal(1.1f, world.PlayerPosition.Y);
    }

    [Fact]
    public void Collision_Doorway1_WalkableInRealWorld_BlockedInOtherWorld()
    {
        var (world, _, _) = CreateTestWorld();
        world.TeleportPlayer(new Vector2(7.8f, 4.5f));
        var input = new MockInputProvider { MovementAxes = new Vector2(1f, 0f) };
        world.Step(0.1f, input);
        Assert.True(world.PlayerPosition.X > 8.0f);

        world.ToggleWorld();
        world.TeleportPlayer(new Vector2(7.8f, 4.5f));
        world.Step(0.1f, input);
        Assert.True(world.PlayerPosition.X <= 8.0f);
    }

    [Fact]
    public void Collision_Doorway3_BlockedInRealWorld_WalkableInOtherWorld()
    {
        var (world, _, _) = CreateTestWorld();
        world.TeleportPlayer(new Vector2(15.8f, 5.5f));
        var input = new MockInputProvider { MovementAxes = new Vector2(1f, 0f) };
        world.Step(0.1f, input);
        Assert.True(world.PlayerPosition.X <= 16.0f);

        world.ToggleWorld();
        world.TeleportPlayer(new Vector2(15.8f, 5.5f));
        world.Step(0.1f, input);
        Assert.True(world.PlayerPosition.X > 16.0f);
    }

    [Fact]
    public void Collision_SlidingAlongWall_PreservesFreeAxisMovement()
    {
        var (world, _, _) = CreateTestWorld();
        // Near top wall (y=0 is wall). Input is east (+X) and north (-Y).
        world.TeleportPlayer(new Vector2(2.5f, 1.05f));
        var input = new MockInputProvider { MovementAxes = new Vector2(1f, -1f) };

        world.Step(0.1f, input);

        Assert.True(world.PlayerPosition.X > 2.5f);
        Assert.True(world.PlayerPosition.Y >= 1.0f);
    }

    [Fact]
    public void LineOfSight_FovAndDistanceBoundaries()
    {
        var (_, map, _) = CreateTestWorld();
        var guardPos = new Vector2(2.5f, 3.5f);
        var guardFacing = 0f; // Facing East

        // Directly ahead within drawing room (3 units away, max 6): visible
        Assert.True(LineOfSight.HasLineOfSight(map, guardPos, guardFacing, new Vector2(5.5f, 3.5f), 6f, MathHelper.DegreesToRadians(75f), WorldDimension.RealWorld));

        // Outside FOV (90 degrees North): not visible
        Assert.False(LineOfSight.HasLineOfSight(map, guardPos, guardFacing, new Vector2(2.5f, 1.5f), 6f, MathHelper.DegreesToRadians(75f), WorldDimension.RealWorld));

        // Behind guard (180 degrees West): not visible
        Assert.False(LineOfSight.HasLineOfSight(map, guardPos, guardFacing, new Vector2(1.5f, 3.5f), 6f, MathHelper.DegreesToRadians(75f), WorldDimension.RealWorld));

        // Beyond max distance (4.0 units away, max 3.0): not visible
        Assert.False(LineOfSight.HasLineOfSight(map, guardPos, guardFacing, new Vector2(6.5f, 3.5f), 3f, MathHelper.DegreesToRadians(75f), WorldDimension.RealWorld));

        // Through solid room wall (x=8): not visible
        Assert.False(LineOfSight.HasLineOfSight(map, guardPos, guardFacing, new Vector2(9.5f, 3.5f), 10f, MathHelper.DegreesToRadians(75f), WorldDimension.RealWorld));
    }
}
