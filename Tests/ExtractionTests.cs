using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class ExtractionTests
{
    private static readonly Vector2 DoorPosition = new(1.5f, 7.5f);

    [Fact]
    public void Extraction_WinsOnlyAfterHoldingForOneSecond()
    {
        var world = CreateWorldAtDoor(3);
        var input = new Input(true);

        world.Step(0.75f, input);
        Assert.False(world.IsGameWon);

        world.Step(0.25f, input);
        Assert.True(world.CreateSnapshot().IsGameWon);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Extraction_ResetsWhenReleasedOrPlayerLeaves(bool release)
    {
        var world = CreateWorldAtDoor(3);
        var input = new Input(true);
        world.Step(0.75f, input);

        if (!release) world.TeleportPlayer(new Vector2(4.5f, 7.5f));
        world.Step(0.25f, new Input(!release));

        world.TeleportPlayer(DoorPosition);
        world.Step(0.75f, input);
        Assert.False(world.IsGameWon);

        world.Step(0.25f, input);
        Assert.True(world.IsGameWon);
    }

    [Fact]
    public void Extraction_RequiresAllThreeEyes()
    {
        var world = CreateWorldAtDoor(2);

        world.Step(2f, new Input(true));

        Assert.False(world.IsGameWon);
    }

    [Fact]
    public void Extraction_BeingCaughtTakesPrecedenceOverWinning()
    {
        var world = CreateWorldAtDoor(3, guardAtDoor: true);
        var input = new Input(true);
        world.Step(0.75f, input);
        world.SetDetectionPercent(100f);

        world.Step(0.25f, input);

        var snapshot = world.CreateSnapshot();
        Assert.True(snapshot.IsGameOver);
        Assert.False(snapshot.IsGameWon);
    }

    private static GameWorldState CreateWorldAtDoor(int eyes, bool guardAtDoor = false)
    {
        var map = new LevelMap
        {
            GhostEyes = [new(1, 2, 2, "Room"), new(2, 2, 3, "Room"), new(3, 2, 4, "Room")],
            Guards = guardAtDoor ? [new(20, "Room", [new(1, 7)])] : []
        };
        var world = new GameWorldState(map, new GameEventBus());
        world.ToggleWorld();

        foreach (var eye in map.GhostEyes.Take(eyes))
        {
            world.TeleportPlayer(new Vector2(eye.X + 0.5f, eye.Y + 0.5f));
            world.Step(0f, new Input(false));
        }

        world.TeleportPlayer(DoorPosition);
        return world;
    }

    private sealed class Input(bool interactHeld) : IInputProvider
    {
        public Vector2 GetMovementAxes() => Vector2.Zero;
        public bool IsSprintHeld() => false;
        public bool IsToggleWorldPressed() => false;
        public bool IsInteractHeld() => interactHeld;
        public bool IsDistractionPressed() => false;
    }
}
