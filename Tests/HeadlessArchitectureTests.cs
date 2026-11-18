using Coraline.AI;
using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class HeadlessArchitectureTests
{
    [Fact]
    public void GameEventBus_PublishesEventsToSubscribers()
    {
        var bus = new GameEventBus();
        NoiseEmittedEvent? received = null;

        bus.Subscribe<NoiseEmittedEvent>(e => received = e);
        bus.Publish(new NoiseEmittedEvent(new Vector2(4f, 5f), 6f, "Sprint"));

        Assert.NotNull(received);
        Assert.Equal("Sprint", received.Value.SourceTag);
        Assert.Equal(6f, received.Value.Radius);
    }

    [Fact]
    public void GameEventBus_Unsubscribe_StopsReceivingEvents()
    {
        var bus = new GameEventBus();
        var callCount = 0;
        Action<WorldShiftedEvent> handler = _ => callCount++;

        bus.Subscribe(handler);
        bus.Publish(new WorldShiftedEvent(WorldDimension.OtherWorld));
        Assert.Equal(1, callCount);

        var unsubscribed = bus.Unsubscribe(handler);
        Assert.True(unsubscribed);

        bus.Publish(new WorldShiftedEvent(WorldDimension.RealWorld));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void GameEventBus_MultipleSubscribers_AllReceiveEvent()
    {
        var bus = new GameEventBus();
        var receivedFirst = false;
        var receivedSecond = false;

        bus.Subscribe<GhostEyeCollectedEvent>(_ => receivedFirst = true);
        bus.Subscribe<GhostEyeCollectedEvent>(_ => receivedSecond = true);

        bus.Publish(new GhostEyeCollectedEvent(1, 1));

        Assert.True(receivedFirst);
        Assert.True(receivedSecond);
    }

    [Fact]
    public void GameEventBus_EventIsolation_DoesNotTriggerUnrelatedSubscribers()
    {
        var bus = new GameEventBus();
        var noiseTriggered = false;

        bus.Subscribe<NoiseEmittedEvent>(_ => noiseTriggered = true);
        bus.Publish(new WorldShiftedEvent(WorldDimension.OtherWorld));

        Assert.False(noiseTriggered);
    }

    [Fact]
    public void GameEventBus_UnsubscribingOneHandler_KeepsOtherHandlersActive()
    {
        var bus = new GameEventBus();
        var handler1Calls = 0;
        var handler2Calls = 0;
        Action<NoiseEmittedEvent> h1 = _ => handler1Calls++;
        Action<NoiseEmittedEvent> h2 = _ => handler2Calls++;

        bus.Subscribe(h1);
        bus.Subscribe(h2);

        bus.Unsubscribe(h1);
        bus.Publish(new NoiseEmittedEvent(Vector2.Zero, 1f, "Test"));

        Assert.Equal(0, handler1Calls);
        Assert.Equal(1, handler2Calls);
    }

    [Fact]
    public void MapLoader_Loads24x16ThreeRoomGridWithDualWorldFlags()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);

        Assert.Equal(24, map.Width);
        Assert.Equal(16, map.Height);
        Assert.Equal(384, map.Tiles.Count);

        // Doorway 1 (x=8, y=4): Open in Real World, Bricked up in Other World
        Assert.True(map.IsWalkable(8, 4, WorldDimension.RealWorld));
        Assert.False(map.IsWalkable(8, 4, WorldDimension.OtherWorld));

        // Doorway 3 (x=16, y=5): Bricked up in Real World, Open in Other World
        Assert.False(map.IsWalkable(16, 5, WorldDimension.RealWorld));
        Assert.True(map.IsWalkable(16, 5, WorldDimension.OtherWorld));
    }

    [Fact]
    public void LineOfSight_IsBlockedByWallsAcrossDimensions()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);

        var observer = new Vector2(6.5f, 4.5f);
        var target = new Vector2(10.5f, 4.5f);

        // Through doorway (8, 4): clear in RealWorld, blocked by bricked doorway in OtherWorld
        var clearInReal = LineOfSight.HasLineOfSight(
            map, observer, 0f, target, 10f, MathHelper.DegreesToRadians(90f), WorldDimension.RealWorld);
        var clearInOther = LineOfSight.HasLineOfSight(
            map, observer, 0f, target, 10f, MathHelper.DegreesToRadians(90f), WorldDimension.OtherWorld);

        Assert.True(clearInReal);
        Assert.False(clearInOther);
    }

    [Fact]
    public void AStarPathfinder_FindsValidPathRespectingWorldLayout()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);

        var start = new Vector2(2.5f, 2.5f);
        var goal = new Vector2(12.5f, 4.5f);

        var path = AStarPathfinder.FindPath(map, start, goal, WorldDimension.OtherWorld);

        Assert.NotEmpty(path);
    }

    [Fact]
    public void RenderSnapshot_ContainsTilesEntitiesAndOtherWorldDebugLines()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);
        var world = new GameWorldState(map, new GameEventBus());

        var realSnapshot = world.CreateSnapshot();
        Assert.Equal(384, realSnapshot.Tiles.Count);
        Assert.Empty(realSnapshot.DebugLines);

        world.ToggleWorld();
        var otherSnapshot = world.CreateSnapshot();
        Assert.Equal(WorldDimension.OtherWorld, otherSnapshot.ActiveWorld);
        Assert.NotEmpty(otherSnapshot.DebugLines);
    }
}
