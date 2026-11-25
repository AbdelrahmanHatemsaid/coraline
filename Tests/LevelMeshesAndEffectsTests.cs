using Coraline.Core;
using Coraline.Rendering;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class LevelMeshesAndEffectsTests
{
    [Fact]
    public void VisionConeMeshBuilder_ProducesExpectedVertexCountAndClipsAgainstWall()
    {
        // 5x5 room where x=3 is a solid wall blocking eastbound rays
        var tiles = new List<TileRenderState>();
        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                tiles.Add(new TileRenderState(x, y, IsWalkable: x < 3, DiffersBetweenWorlds: false));
            }
        }

        const int rayCount = 16;
        var origin = new Vector2(1.5f, 2.5f);
        var verts = VisionConeMeshBuilder.BuildConeFanVertices(
            origin,
            facingRad: 0f,
            fovRad: MathHelper.DegreesToRadians(60f),
            range: 6.0f,
            rayCount: rayCount,
            tiles: tiles,
            gridWidth: 5,
            gridHeight: 5);

        Assert.Equal(rayCount * 3 * 3, verts.Length);

        // Every vertex X must be clipped before or at the x=3.0 wall boundary
        for (var i = 0; i < verts.Length; i += 3)
        {
            Assert.True(verts[i] <= 3.0f, $"Ray vertex leaked past wall at X={verts[i]}");
        }
    }

    [Fact]
    public void VisionConeMeshBuilder_RingVerticesMatchSegmentCount()
    {
        const int segments = 12;
        var verts = VisionConeMeshBuilder.BuildRingVertices(new Vector2(4f, 4f), 0.5f, 1.0f, segments);

        Assert.Equal(segments * 6 * 3, verts.Length);
    }

    [Fact]
    public void LevelEffectsRenderer_SpawnsAndExpiresNoiseRipplesAndUnsubscribesOnDispose()
    {
        var bus = new GameEventBus();
        var renderer = new LevelEffectsRenderer(bus);

        bus.Publish(new NoiseEmittedEvent(new Vector2(2f, 2f), 6.0f, "Sprint"));
        bus.Publish(new NoiseEmittedEvent(new Vector2(5f, 2f), 8.0f, "Distraction"));
        Assert.Equal(2, renderer.ActiveRippleCount);

        // Sprint expires at 0.55s; Distraction remains until 1.0s
        renderer.Update(0.60f);
        Assert.Equal(1, renderer.ActiveRippleCount);

        renderer.Update(0.50f);
        Assert.Equal(0, renderer.ActiveRippleCount);

        renderer.Dispose();
        bus.Publish(new NoiseEmittedEvent(new Vector2(2f, 2f), 6.0f, "Sprint"));
        Assert.Equal(0, renderer.ActiveRippleCount);
    }
}
