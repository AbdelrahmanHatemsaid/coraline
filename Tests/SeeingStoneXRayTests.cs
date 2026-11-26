using Coraline.Core;
using Coraline.Rendering;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class SeeingStoneXRayTests
{
    [Fact]
    public void IsActiveFor_ReturnsTrueOnlyInOtherWorld()
    {
        Assert.False(SeeingStoneXRayRenderer.IsActiveFor(new RenderSnapshot { ActiveWorld = WorldDimension.RealWorld }));
        Assert.True(SeeingStoneXRayRenderer.IsActiveFor(new RenderSnapshot { ActiveWorld = WorldDimension.OtherWorld }));
    }

    [Fact]
    public void GetHighlightSpec_HighlightsGuardsAndGhostEyesOnly()
    {
        var guardPatrol = new EntityRenderState(101, EntityType.Guard, new Vector2(10f, 4f), 0f, "Patrol", WorldDimension.OtherWorld);
        var guardChase = new EntityRenderState(102, EntityType.Guard, new Vector2(12f, 4f), 0f, "AlertChase", WorldDimension.OtherWorld);
        var ghostEye = new EntityRenderState(11, EntityType.GhostEye, new Vector2(5f, 13f), 0f, "Idle", WorldDimension.OtherWorld);
        var player = new EntityRenderState(1, EntityType.Player, new Vector2(2f, 2f), 0f, "Sneak", WorldDimension.OtherWorld);

        var patrolSpec = SeeingStoneXRayRenderer.GetHighlightSpec(guardPatrol, 0f);
        var chaseSpec = SeeingStoneXRayRenderer.GetHighlightSpec(guardChase, 0f);
        var eyeSpec = SeeingStoneXRayRenderer.GetHighlightSpec(ghostEye, 0f);
        var playerSpec = SeeingStoneXRayRenderer.GetHighlightSpec(player, 0f);

        Assert.NotNull(patrolSpec);
        Assert.NotNull(chaseSpec);
        Assert.NotNull(eyeSpec);
        Assert.Null(playerSpec);

        Assert.True(chaseSpec!.Value.Color.X > eyeSpec!.Value.Color.X);
        Assert.True(eyeSpec.Value.Color.Y > chaseSpec.Value.Color.Y);
    }

    [Theory]
    [InlineData("xray_vertex.glsl")]
    [InlineData("xray_fragment.glsl")]
    public void XRayShaders_ExistInOutputDirectory(string shaderFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", shaderFile);
        Assert.True(File.Exists(path), $"Expected X-Ray shader at {path}");
    }
}
