using Coraline.Core;
using Coraline.Rendering;
using Xunit;

namespace Coraline.Tests;

public sealed class ColorDrainPostProcessorTests
{
    [Fact]
    public void ComputeEffects_AtZeroDetection_HasNoDrainAndWideVignette()
    {
        var snapshot = new RenderSnapshot
        {
            ActiveWorld = WorldDimension.RealWorld,
            DetectionPercent = 0.0f
        };

        var effects = ColorDrainPostProcessor.ComputeEffects(snapshot);

        Assert.Equal(0.0f, effects.DetectionPercent);
        Assert.Equal(0.0f, effects.DrainFactor);
        Assert.Equal(0.0f, effects.OtherWorldBlend);
        Assert.True(effects.VignetteRadius > 1.3f);
    }

    [Fact]
    public void ComputeEffects_AsDetectionRises_IncreasesDrainAndTightensVignette()
    {
        var low = ColorDrainPostProcessor.ComputeEffects(new RenderSnapshot { DetectionPercent = 10.0f });
        var mid = ColorDrainPostProcessor.ComputeEffects(new RenderSnapshot { DetectionPercent = 50.0f });
        var full = ColorDrainPostProcessor.ComputeEffects(new RenderSnapshot { DetectionPercent = 100.0f });

        Assert.True(low.DrainFactor < mid.DrainFactor);
        Assert.True(mid.DrainFactor < full.DrainFactor);
        Assert.Equal(1.0f, full.DrainFactor);

        Assert.True(low.VignetteRadius > mid.VignetteRadius);
        Assert.True(mid.VignetteRadius > full.VignetteRadius);
    }

    [Fact]
    public void ComputeEffects_ClampsOutOfRangeDetectionAndSetsOtherWorldBlend()
    {
        var under = ColorDrainPostProcessor.ComputeEffects(new RenderSnapshot
        {
            ActiveWorld = WorldDimension.RealWorld,
            DetectionPercent = -25.0f
        });
        var over = ColorDrainPostProcessor.ComputeEffects(new RenderSnapshot
        {
            ActiveWorld = WorldDimension.OtherWorld,
            DetectionPercent = 160.0f
        });

        Assert.Equal(0.0f, under.DetectionPercent);
        Assert.Equal(0.0f, under.OtherWorldBlend);

        Assert.Equal(100.0f, over.DetectionPercent);
        Assert.Equal(1.0f, over.DrainFactor);
        Assert.Equal(1.0f, over.OtherWorldBlend);
    }

    [Theory]
    [InlineData("post_vertex.glsl")]
    [InlineData("post_colordrain.glsl")]
    public void PostProcessShaders_ExistInOutputAssetsDirectory(string shaderFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", shaderFile);
        Assert.True(File.Exists(path), $"Expected post-process shader at {path}");
    }
}
