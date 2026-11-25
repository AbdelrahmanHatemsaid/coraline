using Coraline.AI;
using Coraline.Audio;
using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class AudioAndDistractionTests
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

    private sealed class MockAudioService : IAudioService
    {
        public List<string> PlayedSounds { get; } = [];
        public void Play(string soundName) => PlayedSounds.Add(soundName);
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
    public void Distraction_WhenInputPressed_EmitsNoiseEventInFacingDirection()
    {
        var (world, _, bus) = CreateTestWorld();
        NoiseEmittedEvent? emitted = null;
        bus.Subscribe<NoiseEmittedEvent>(e => emitted = e);

        world.TeleportPlayer(new Vector2(2.5f, 2.5f));
        world.Step(0.05f, new MockInputProvider { MovementAxes = new Vector2(1f, 0f) });

        var throwInput = new MockInputProvider { DistractionPressed = true };
        world.Step(0.05f, throwInput);

        Assert.NotNull(emitted);
        Assert.Equal("Distraction", emitted.Value.SourceTag);
        Assert.Equal(8.0f, emitted.Value.Radius);
        Assert.True(emitted.Value.Origin.X > 2.5f);
    }

    [Fact]
    public void Distraction_Cooldown_PreventsSpammingInConsecutiveFrames()
    {
        var (world, _, bus) = CreateTestWorld();
        var eventCount = 0;
        bus.Subscribe<NoiseEmittedEvent>(e =>
        {
            if (e.SourceTag == "Distraction") eventCount++;
        });

        var throwInput = new MockInputProvider { DistractionPressed = true };
        world.Step(0.1f, throwInput);
        Assert.Equal(1, eventCount);

        world.Step(0.1f, throwInput);
        Assert.Equal(1, eventCount);

        world.Step(1.0f, new MockInputProvider());
        world.Step(0.1f, throwInput);
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void Guard_HearsDistractionWithinNoiseRadius_SwitchesToInvestigating()
    {
        var (world, _, _) = CreateTestWorld();
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(5.5f, 3.5f));
        world.TeleportPlayer(new Vector2(2.5f, 3.5f));

        world.Step(0.05f, new MockInputProvider { MovementAxes = new Vector2(1f, 0f) });
        world.Step(0.05f, new MockInputProvider { DistractionPressed = true });

        Assert.Equal(GuardBehaviorState.Investigating, guard.State);
        Assert.NotNull(guard.InvestigationTarget);
    }

    [Fact]
    public void Guard_OutsideNoiseRadius_IgnoresDistractionAndContinuesPatrol()
    {
        var (world, _, _) = CreateTestWorld();
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(18.5f, 12.5f));
        world.TeleportPlayer(new Vector2(2.5f, 2.5f));

        world.Step(0.05f, new MockInputProvider { DistractionPressed = true });

        Assert.Equal(GuardBehaviorState.Patrol, guard.State);
        Assert.Null(guard.InvestigationTarget);
    }

    [Fact]
    public void Guard_InAlertChase_DoesNotDivertToDistraction()
    {
        var (world, _, _) = CreateTestWorld();
        world.ToggleWorld();
        var guard = world.Guards[0];
        guard.Teleport(new Vector2(3.5f, 3.5f));
        world.TeleportPlayer(new Vector2(5.5f, 3.5f));
        world.SetDetectionPercent(100.0f);
        world.Step(0.05f, new MockInputProvider());

        Assert.Equal(GuardBehaviorState.AlertChase, guard.State);

        world.Step(0.05f, new MockInputProvider { DistractionPressed = true });

        Assert.Equal(GuardBehaviorState.AlertChase, guard.State);
    }

    [Fact]
    public void AudioTriggerSystem_PlaysDistractionSound_WhenDistractionNoiseEmitted()
    {
        var bus = new GameEventBus();
        var mockAudio = new MockAudioService();
        using var system = new AudioTriggerSystem(bus, mockAudio);

        bus.Publish(new NoiseEmittedEvent(new Vector2(5f, 5f), 8f, "Distraction"));

        Assert.Contains("distraction", mockAudio.PlayedSounds);
    }

    [Fact]
    public void AudioTriggerSystem_PlaysFootstepsSound_ThrottledByCooldown()
    {
        var bus = new GameEventBus();
        var mockAudio = new MockAudioService();
        using var system = new AudioTriggerSystem(bus, mockAudio);

        bus.Publish(new NoiseEmittedEvent(new Vector2(2f, 2f), 6f, "Sprint"));
        bus.Publish(new NoiseEmittedEvent(new Vector2(2.5f, 2f), 6f, "Sprint"));

        Assert.Single(mockAudio.PlayedSounds);
        Assert.Equal("footsteps", mockAudio.PlayedSounds[0]);

        system.Update(0.3f);
        bus.Publish(new NoiseEmittedEvent(new Vector2(3f, 2f), 6f, "Sprint"));
        Assert.Equal(2, mockAudio.PlayedSounds.Count);
    }

    [Fact]
    public void AudioTriggerSystem_PlaysAlertSound_WhenAlertTriggered()
    {
        var bus = new GameEventBus();
        var mockAudio = new MockAudioService();
        using var system = new AudioTriggerSystem(bus, mockAudio);

        bus.Publish(new NoiseEmittedEvent(Vector2.Zero, 0f, "Alert"));

        Assert.Contains("alert", mockAudio.PlayedSounds);
    }

    [Fact]
    public void NAudioPlayer_RapidPlayAndDispose_DoesNotThrowUnhandledExceptions()
    {
        using var player = new NAudioPlayer();
        player.Play("footsteps");
        player.Play("distraction");
        player.Play("alert");
        player.Dispose();
        player.Play("footsteps");
        player.Dispose();
    }
}
