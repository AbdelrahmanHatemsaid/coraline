using Coraline.Core;
using Coraline.Rendering;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class PrimitivePropsTests
{
    [Fact]
    public void BuildRoomFurniture_SpansAllThreeRoomsWithRichMultiPartProps()
    {
        var realProps = PropPlacementLayout.BuildRoomFurniture(WorldDimension.RealWorld);
        var otherProps = PropPlacementLayout.BuildRoomFurniture(WorldDimension.OtherWorld);

        Assert.Equal(realProps.Count, otherProps.Count);
        Assert.True(realProps.Count >= 55, $"Expected rich multi-part furniture, got {realProps.Count}");
        Assert.Contains(realProps, p => p.Position.X < 8f);                 // Drawing Room
        Assert.Contains(realProps, p => p.Position.X is >= 8f and < 16f);  // Kitchen
        Assert.Contains(realProps, p => p.Position.X >= 16f);               // Bedroom
        Assert.NotEqual(realProps[0].Color, otherProps[0].Color);
    }

    [Fact]
    public void BuildSmallDoorArch_HighlightsDoorknobWhenThreeEyesCollected()
    {
        var locked = PropPlacementLayout.BuildSmallDoorArch(new Vector2(1.5f, 7.5f), unlocked: false, WorldDimension.RealWorld);
        var unlocked = PropPlacementLayout.BuildSmallDoorArch(new Vector2(1.5f, 7.5f), unlocked: true, WorldDimension.RealWorld);

        Assert.Equal(5, locked.Count);
        Assert.Equal(5, unlocked.Count);
        Assert.NotEqual(locked[^1].Color, unlocked[^1].Color);
        Assert.True(unlocked[^1].Color.Y > unlocked[^1].Color.X); // Emerald unlock glow
    }

    [Fact]
    public void BuildWebTrap_GeneratesRadialSpokesAndRings()
    {
        Assert.Equal(3, PropPlacementLayout.WebTrapPositions.Length);
        var webParts = PropPlacementLayout.BuildWebTrap(PropPlacementLayout.WebTrapPositions[0]);

        Assert.Equal(6, webParts.Count);
        Assert.Equal(4, webParts.Count(p => p.Shape == PropShape.Cube));
        Assert.Equal(2, webParts.Count(p => p.Shape == PropShape.Cylinder));
    }
}
