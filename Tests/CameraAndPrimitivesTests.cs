using Coraline.OpenGLCore;
using Coraline.Rendering;
using OpenTK.Mathematics;
using Xunit;

namespace Coraline.Tests;

public sealed class CameraAndPrimitivesTests
{
    [Fact]
    public void TacticalCamera3D_ZoomClampsWithinBoundsAndBuildsValidMatrices()
    {
        var camera = new TacticalCamera3D();

        camera.Zoom(-100f);
        Assert.Equal(TacticalCamera3D.MinOrthographicSize, camera.OrthographicSize);

        camera.Zoom(100f);
        Assert.Equal(TacticalCamera3D.MaxOrthographicSize, camera.OrthographicSize);

        var view = camera.GetViewMatrix();
        var proj = camera.GetProjectionMatrix(16f / 9f);
        Assert.NotEqual(Matrix4.Zero, view);
        Assert.NotEqual(Matrix4.Zero, proj);
    }

    [Fact]
    public void TacticalCamera3D_FollowTargetMovesTowardClampedWorldPosition()
    {
        var camera = new TacticalCamera3D { Target = new Vector3(12f, 0f, 8f) };

        camera.FollowTarget(new Vector2(2f, 2f), 0.5f);

        Assert.True(camera.Target.X < 12f);
        Assert.True(camera.Target.Z < 8f);
        Assert.InRange(camera.Target.X, 4f, 20f);
        Assert.InRange(camera.Target.Z, 3f, 13f);
    }

    [Fact]
    public void PrimitiveMeshBuilder_CubeHas36VerticesWithUnitNormals()
    {
        var cube = PrimitiveMeshBuilder.CreateCube(1.0f);

        Assert.Equal(36, cube.VertexCount);
        Assert.Equal(36 * PrimitiveMeshData.FloatsPerVertex, cube.Vertices.Length);
        AssertAllNormalsAreUnitLength(cube);
    }

    [Fact]
    public void PrimitiveMeshBuilder_PlaneAndBillboardHave6VerticesWithUnitNormals()
    {
        var plane = PrimitiveMeshBuilder.CreatePlane(2.0f, 2.0f);
        var billboard = PrimitiveMeshBuilder.CreateBillboardQuad(1.0f, 1.5f);

        Assert.Equal(6, plane.VertexCount);
        Assert.Equal(6, billboard.VertexCount);
        AssertAllNormalsAreUnitLength(plane);
        AssertAllNormalsAreUnitLength(billboard);
    }

    [Fact]
    public void PrimitiveMeshBuilder_CylinderGeneratesExpectedSegmentsAndUnitNormals()
    {
        var cylinder = PrimitiveMeshBuilder.CreateCylinder(0.5f, 1.0f, segments: 16);

        // 16 segments * (6 side verts + 3 top cap verts + 3 bottom cap verts) = 192 vertices
        Assert.Equal(192, cylinder.VertexCount);
        Assert.Equal(192 * PrimitiveMeshData.FloatsPerVertex, cylinder.Vertices.Length);
        AssertAllNormalsAreUnitLength(cylinder);
    }

    [Theory]
    [InlineData("coraline_player.jpg")]
    [InlineData("beldam_enemy.jpg")]
    [InlineData("small_door.jpg")]
    [InlineData("ui_seeing_stone.jpg")]
    [InlineData("ui_ghost_eye.jpg")]
    [InlineData("ui_detection_meter.jpg")]
    public void TextureLoader_DecodesAllTextureAssetsToRgbaBytes(string fileName)
    {
        var path = TextureLoader.ResolveAssetPath(fileName);
        var decoded = TextureLoader.DecodeImage(path);

        Assert.True(decoded.Width > 0);
        Assert.True(decoded.Height > 0);
        Assert.Equal(decoded.Width * decoded.Height * 4, decoded.RgbaBytes.Length);
    }

    private static void AssertAllNormalsAreUnitLength(PrimitiveMeshData mesh)
    {
        for (var i = 0; i < mesh.VertexCount; i++)
        {
            var offset = i * PrimitiveMeshData.FloatsPerVertex;
            var normal = new Vector3(
                mesh.Vertices[offset + 3],
                mesh.Vertices[offset + 4],
                mesh.Vertices[offset + 5]);
            Assert.InRange(normal.Length, 0.99f, 1.01f);
        }
    }
}
