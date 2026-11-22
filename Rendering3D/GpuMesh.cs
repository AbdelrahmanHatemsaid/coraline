using OpenTK.Graphics.OpenGL4;

namespace Coraline.OpenGLCore;

public sealed class GpuMesh : IDisposable
{
    private bool _disposed;

    public int Vao { get; private set; }
    public int Vbo { get; private set; }
    public int VertexCount { get; }

    public GpuMesh(PrimitiveMeshData meshData)
    {
        VertexCount = meshData.VertexCount;

        Vao = GL.GenVertexArray();
        Vbo = GL.GenBuffer();

        GL.BindVertexArray(Vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            meshData.Vertices.Length * sizeof(float),
            meshData.Vertices,
            BufferUsageHint.StaticDraw);

        const int stride = PrimitiveMeshData.FloatsPerVertex * sizeof(float);

        // Location 0: aPosition (vec3)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

        // Location 1: aNormal (vec3)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

        // Location 2: aTexCoord (vec2)
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(Vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, VertexCount);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (Vbo != 0)
        {
            GL.DeleteBuffer(Vbo);
            Vbo = 0;
        }

        if (Vao != 0)
        {
            GL.DeleteVertexArray(Vao);
            Vao = 0;
        }

        _disposed = true;
    }
}
