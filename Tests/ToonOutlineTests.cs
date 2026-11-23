using Coraline.Core;
using Coraline.OpenGLCore;
using Coraline.Rendering;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class ToonOutlineTests
{
    [Fact]
    public void ComicOutlineRenderer_GetStyle_AdaptsColorAndThicknessAcrossDimensions()
    {
        var realStyle = ComicOutlineRenderer.GetStyle(WorldDimension.RealWorld);
        var otherStyle = ComicOutlineRenderer.GetStyle(WorldDimension.OtherWorld);

        Assert.True(realStyle.WallThickness > 0.02f);
        Assert.True(otherStyle.WallThickness >= realStyle.WallThickness);
        Assert.NotEqual(realStyle.Color, otherStyle.Color);
    }

    [Fact]
    public void PrimitiveMeshBuilder_CylinderWindingMatchesOutwardNormals()
    {
        var cylinder = PrimitiveMeshBuilder.CreateCylinder(0.5f, 1.0f, segments: 12);

        for (var i = 0; i < cylinder.VertexCount; i += 3)
        {
            var o0 = i * PrimitiveMeshData.FloatsPerVertex;
            var o1 = (i + 1) * PrimitiveMeshData.FloatsPerVertex;
            var o2 = (i + 2) * PrimitiveMeshData.FloatsPerVertex;

            var p0 = new Vector3(cylinder.Vertices[o0], cylinder.Vertices[o0 + 1], cylinder.Vertices[o0 + 2]);
            var p1 = new Vector3(cylinder.Vertices[o1], cylinder.Vertices[o1 + 1], cylinder.Vertices[o1 + 2]);
            var p2 = new Vector3(cylinder.Vertices[o2], cylinder.Vertices[o2 + 1], cylinder.Vertices[o2 + 2]);
            var storedNormal = new Vector3(cylinder.Vertices[o0 + 3], cylinder.Vertices[o0 + 4], cylinder.Vertices[o0 + 5]);

            // OpenGL CCW front face: (p1 - p0) x (p2 - p0) points in same hemisphere as outward normal
            var geoNormal = Vector3.Cross(p1 - p0, p2 - p0);
            Assert.True(Vector3.Dot(geoNormal, storedNormal) > 0f, $"Triangle {i / 3} has inverted winding.");
        }
    }

    [Theory]
    [InlineData("outline_vertex.glsl")]
    [InlineData("outline_fragment.glsl")]
    [InlineData("primitive_fragment.glsl")]
    public void ToonAndOutlineShaders_ExistInOutputDirectory(string shaderFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", shaderFile);
        Assert.True(File.Exists(path), $"Expected shader file at {path}");
    }
}
