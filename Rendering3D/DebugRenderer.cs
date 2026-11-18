using Coraline.Core;
using OpenTK.Graphics.OpenGL4;

namespace Coraline.Rendering;

public sealed class DebugRenderer : IDisposable
{
    private int _vao;
    private int _vbo;
    private bool _disposed;

    public void Initialize()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 6 * 3 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.BindVertexArray(0);
    }

    public void Render(RenderSnapshot snapshot)
    {
        GL.BindVertexArray(_vao);
        foreach (var (start, end) in snapshot.DebugLines)
        {
            float[] lineVertices =
            [
                start.X, 0.1f, start.Y,
                end.X, 0.1f, end.Y
            ];
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, lineVertices.Length * sizeof(float), lineVertices);
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);
        }
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        _disposed = true;
    }
}
