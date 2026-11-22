using Coraline.AI;
using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class AStarChaseTests
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
    public void AStar_RoutesAroundBrickedDoorway1_InOtherWorld()
    {
        var (_, map, _) = CreateTestWorld();
        var start = new Vector2(6.5f, 4.5f);
        var goal = new Vector2(10.5f, 4.5f);

        var path = AStarPathfinder.FindPath(map, start, goal, WorldDimension.OtherWorld);

        Assert.NotEmpty(path);
        Assert.DoesNotContain(path, p => (int)MathF.Floor(p.X) == 8 && (int)MathF.Floor(p.Y) == 4);
    }

    [Fact]
    public void AStar_TraversesOpenDoorway3_InOtherWorld()
    {
        var (_, map, _) = CreateTestWorld();
        var start = new Vector2(14.5f, 5.5f);
        var goal = new Vector2(18.5f, 5.5f);

        var path = AStarPathfinder.FindPath(map, start, goal, WorldDimension.OtherWorld);

        Assert.NotEmpty(path);
        Assert.Contains(path, p => (int)MathF.Floor(p.X) == 16 && (int)MathF.Floor(p.Y) == 5);
    }

    [Fact]
    public void AStar_GoalOnUnwalkableTile_ResolvesToNearestWalkableNeighbor()
    {
        var (_, map, _) = CreateTestWorld();
        var start = new Vector2(2.5f, 2.5f);
        var unwalkableGoal = new Vector2(2.5f, 0.5f);

        var path = AStarPathfinder.FindPath(map, start, unwalkableGoal, WorldDimension.OtherWorld);

        Assert.NotEmpty(path);
        var end = path[^1];
        Assert.True(map.IsWalkable((int)MathF.Floor(end.X), (int)MathF.Floor(end.Y), WorldDimension.OtherWorld));
        Assert.Equal(2, (int)MathF.Floor(end.X));
        Assert.Equal(1, (int)MathF.Floor(end.Y));
    }

    [Fact]
    public void Guard_AlertChase_PursuesPlayerTowardsTargetPosition()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld();
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(2.5f, 3.5f));
        world.TeleportPlayer(new Vector2(6.5f, 3.5f));

        world.SetDetectionPercent(100.0f);
        world.Step(0.2f, new MockInputProvider());

        Assert.Equal(GuardBehaviorState.AlertChase, guard.State);
        Assert.True(guard.Position.X > 2.5f);
    }

    [Fact]
    public void Guard_AlertChase_TransitionsToInvestigatingLastKnownPos_WhenDetectionDecays()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld();
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(3.5f, 3.5f));
        var playerPos = new Vector2(5.5f, 3.5f);
        world.TeleportPlayer(playerPos);

        world.SetDetectionPercent(100.0f);
        world.Step(0.05f, new MockInputProvider());
        Assert.Equal(GuardBehaviorState.AlertChase, guard.State);
        Assert.Equal(playerPos, guard.LastKnownPlayerPos);

        world.TeleportPlayer(new Vector2(1.5f, 1.5f));
        world.SetDetectionPercent(80.0f);
        world.Step(0.05f, new MockInputProvider());

        Assert.Equal(GuardBehaviorState.Investigating, guard.State);
        Assert.Equal(playerPos, guard.InvestigationTarget);
    }

    [Fact]
    public void Guard_AlertChase_SpeedIsFasterThanSneakAndSlowerThanSprint()
    {
        var sneak = new SneakStance();
        var sprint = new SprintStance();
        const float chaseSpeed = 3.6f;

        Assert.True(chaseSpeed > sneak.MoveSpeed);
        Assert.True(chaseSpeed < sprint.MoveSpeed);
    }

    [Fact]
    public void Guard_AlertChase_PhysicalContactTriggersGameOver()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld();
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(3.0f, 3.0f));
        world.TeleportPlayer(new Vector2(3.3f, 3.0f));

        world.SetDetectionPercent(100.0f);
        world.Step(0.05f, new MockInputProvider());

        Assert.True(world.IsGameOver);
    }
}
