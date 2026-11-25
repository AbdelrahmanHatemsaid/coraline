using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

file sealed class MockInput : IInputProvider
{
    public bool ToggleWorld;
    public bool Interact;
    public Vector2 Move = Vector2.Zero;

    public bool IsToggleWorldPressed() => ToggleWorld;
    public bool IsInteractHeld()       => Interact;
    public bool IsSprintHeld()         => false;
    public bool IsDistractionPressed() => false;
    public Vector2 GetMovementAxes()   => Move;
}

file static class Factory
{
    public static (GameWorldState State, MockInput Input) Build()
    {
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        var map = MapLoader.LoadFromFile(mapPath);
        var bus = new GameEventBus();
        var state = new GameWorldState(map, bus);
        return (state, new MockInput());
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class SeeingStoneTests
{
    // 1. Tab switches Real → Other
    [Fact]
    public void SeeingStone_Toggle_SwitchesDimension()
    {
        var (state, input) = Factory.Build();
        Assert.Equal(WorldDimension.RealWorld, state.ActiveWorld);

        input.ToggleWorld = true;
        state.Step(0.016f, input);

        Assert.Equal(WorldDimension.OtherWorld, state.ActiveWorld);
    }

    // 2. Charge depletes on toggle and blocks a second immediate toggle
    [Fact]
    public void SeeingStone_CooldownBlocksRetoggle_WhileRecharging()
    {
        var (state, input) = Factory.Build();

        // First toggle — succeeds
        input.ToggleWorld = true;
        state.Step(0.016f, input);
        Assert.Equal(WorldDimension.OtherWorld, state.ActiveWorld);

        // Charge should be ~0 immediately after toggle
        Assert.True(state.SeeingStoneChargePercent < 100f);

        // Second toggle in same frame — must NOT switch back
        state.Step(0.016f, input);
        Assert.Equal(WorldDimension.OtherWorld, state.ActiveWorld);
    }

    // 3. After 3 s worth of steps, charge reaches 100 %
    [Fact]
    public void SeeingStone_ChargeReachesFullyAfterCooldown()
    {
        var (state, input) = Factory.Build();

        // Drain charge
        input.ToggleWorld = true;
        state.Step(0.016f, input);
        input.ToggleWorld = false;

        Assert.True(state.SeeingStoneChargePercent < 100f);

        // Simulate 3.1 s recharge (100 steps × 0.031 s)
        for (var i = 0; i < 100; i++)
            state.Step(0.031f, input);

        Assert.True(state.SeeingStoneChargePercent >= 100f,
            $"Expected full charge after 3.1 s, got {state.SeeingStoneChargePercent:F1}");
    }

    // 4. Ghost Eyes only collectible in OtherWorld
    [Fact]
    public void SeeingStone_GhostEyes_OnlyCollectibleInOtherWorld()
    {
        var (state, input) = Factory.Build();
        // Teleport to first Ghost Eye position (tile 11,3 → world 11.5, 3.5)
        state.TeleportPlayer(new Vector2(11.5f, 3.5f));

        // Step while in RealWorld — should not collect
        input.ToggleWorld = false;
        state.Step(0.016f, input);
        Assert.Equal(0, state.CreateSnapshot().CollectedEyes);
    }

    // 5. All three eyes + interact at door → game won
    [Fact]
    public void SeeingStone_AllThreeEyes_EnablesExtraction()
    {
        var (state, input) = Factory.Build();

        // Toggle to OtherWorld
        input.ToggleWorld = true;
        state.Step(0.016f, input);
        input.ToggleWorld = false;
        Assert.Equal(WorldDimension.OtherWorld, state.ActiveWorld);

        // Collect all 3 eyes (positions from level JSON: (5,13) / (12,3) / (21,12))
        Vector2[] eyePositions = [new(5.5f, 13.5f), new(12.5f, 3.5f), new(21.5f, 12.5f)];
        foreach (var pos in eyePositions)
        {
            state.TeleportPlayer(pos);
            state.Step(0.016f, input);
        }

        Assert.Equal(3, state.CreateSnapshot().CollectedEyes);

        // Interact at Small Door (tile 1,7 → world 1.5, 7.5)
        state.TeleportPlayer(new Vector2(1.5f, 7.5f));
        input.Interact = true;
        state.Step(1.0f, input);

        Assert.True(state.CreateSnapshot().IsGameWon);
    }

    // 6. Snapshot SeeingStoneChargePercent reflects current charge
    [Fact]
    public void SeeingStone_ChargePercent_InSnapshot_ReflectsCurrentCharge()
    {
        var (state, input) = Factory.Build();

        // Full charge at start
        var snap1 = state.CreateSnapshot();
        Assert.Equal(100f, snap1.SeeingStoneChargePercent, 1);

        // Drain via toggle
        input.ToggleWorld = true;
        state.Step(0.016f, input);
        input.ToggleWorld = false;

        var snap2 = state.CreateSnapshot();
        Assert.True(snap2.SeeingStoneChargePercent < 100f,
            "Snapshot charge should drop after toggle");

        // Partial recharge (1 s worth)
        for (var i = 0; i < 33; i++)
            state.Step(0.030f, input);

        var snap3 = state.CreateSnapshot();
        Assert.True(snap3.SeeingStoneChargePercent > snap2.SeeingStoneChargePercent,
            "Snapshot charge should increase while recharging");
    }
}
