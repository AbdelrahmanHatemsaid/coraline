using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.OpenGLCore;

public sealed class ShaderProgram : IDisposable
{
    public int Handle { get; private set; }
    private bool _disposed;

    public ShaderProgram(string vertexPath, string fragmentPath)
    {
        var vertexSource = File.ReadAllText(vertexPath);
        var fragmentSource = File.ReadAllText(fragmentPath);

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var success);
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        if (success == 0)
        {
            var infoLog = GL.GetProgramInfoLog(Handle);
            throw new InvalidOperationException($"GLSL Program Link Error: {infoLog}");
        }
    }

    public void Use() => GL.UseProgram(Handle);

    public void SetMatrix4(string name, Matrix4 value)
    {
        var location = GL.GetUniformLocation(Handle, name);
        if (location != -1)
        {
            GL.UniformMatrix4(location, false, ref value);
        }
    }

    public void SetVector4(string name, Vector4 value)
    {
        var location = GL.GetUniformLocation(Handle, name);
        if (location != -1)
        {
            GL.Uniform4(location, value);
        }
    }

    public void SetFloat(string name, float value)
    {
        var location = GL.GetUniformLocation(Handle, name);
        if (location != -1)
        {
            GL.Uniform1(location, value);
        }
    }

    private static int CompileShader(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var success);
        if (success == 0)
        {
            var infoLog = GL.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"GLSL {type} Compile Error: {infoLog}");
        }

        return shader;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Handle != 0)
        {
            GL.DeleteProgram(Handle);
            Handle = 0;
        }

        _disposed = true;
    }
}
