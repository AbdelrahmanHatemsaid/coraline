using Coraline.Core;
using Coraline.Rendering;
using Xunit;

namespace Coraline.Tests;

public sealed class QuadHudRendererTests
{
    [Fact]
    public void ComputeLayout_MapsDetectionCollectedEyesAndWorldState()
    {
        var snapshot = new RenderSnapshot
        {
            ActiveWorld = WorldDimension.OtherWorld,
            DetectionPercent = 65.0f,
            CollectedEyes = 2,
            IsGameOver = false,
            IsGameWon = false
        };

        var layout = QuadHudRenderer.ComputeLayout(snapshot);

        Assert.InRange(layout.DetectionFillRatio, 0.649f, 0.651f);
        Assert.Equal(2, layout.LitGhostEyeSlots);
        Assert.True(layout.IsSeeingStoneActive);
        Assert.False(layout.ShowButtonEyesOverlay);
        Assert.False(layout.ShowVictoryOverlay);
    }

    [Fact]
    public void ComputeLayout_ClampsOutOfRangeValuesAndFlagsEndStates()
    {
        var caught = QuadHudRenderer.ComputeLayout(new RenderSnapshot
        {
            ActiveWorld = WorldDimension.RealWorld,
            DetectionPercent = 140.0f,
            CollectedEyes = -1,
            IsGameOver = true
        });

        Assert.Equal(1.0f, caught.DetectionFillRatio);
        Assert.Equal(0, caught.LitGhostEyeSlots);
        Assert.False(caught.IsSeeingStoneActive);
        Assert.True(caught.ShowButtonEyesOverlay);

        var won = QuadHudRenderer.ComputeLayout(new RenderSnapshot
        {
            DetectionPercent = -10.0f,
            CollectedEyes = 5,
            IsGameWon = true
        });

        Assert.Equal(0.0f, won.DetectionFillRatio);
        Assert.Equal(3, won.LitGhostEyeSlots);
        Assert.True(won.ShowVictoryOverlay);
    }

    [Theory]
    [InlineData("Shaders", "hud_vertex.glsl")]
    [InlineData("Shaders", "hud_fragment.glsl")]
    [InlineData("Textures", "ui_seeing_stone.jpg")]
    [InlineData("Textures", "ui_ghost_eye.jpg")]
    [InlineData("Textures", "ui_detection_meter.jpg")]
    public void HudAssets_ExistInOutputDirectory(string folder, string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", folder, fileName);
        Assert.True(File.Exists(path), $"Missing HUD asset: {path}");
    }
}
