using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Coraline.OpenGLCore;

public static class ShaderLoader
{
    public static ShaderProgram Load(string vertexFileName, string fragmentFileName)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
        var vertexPath = Path.Combine(baseDir, vertexFileName);
        var fragmentPath = Path.Combine(baseDir, fragmentFileName);
        return new ShaderProgram(vertexPath, fragmentPath);
    }
}

public sealed class ShaderProgram : IDisposable
{
    private readonly Dictionary<string, int> _uniformLocations = new();
    private bool _disposed;

    public int Handle { get; private set; }

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
            GL.DeleteProgram(Handle);
            Handle = 0;
            throw new InvalidOperationException($"GLSL Program Link Error: {infoLog}");
        }
    }

    public void Use() => GL.UseProgram(Handle);

    public void SetMatrix4(string name, Matrix4 value)
    {
        var location = GetUniformLocation(name);
        if (location != -1) GL.UniformMatrix4(location, false, ref value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        var location = GetUniformLocation(name);
        if (location != -1) GL.Uniform3(location, value);
    }

    public void SetVector4(string name, Vector4 value)
    {
        var location = GetUniformLocation(name);
        if (location != -1) GL.Uniform4(location, value);
    }

    public void SetFloat(string name, float value)
    {
        var location = GetUniformLocation(name);
        if (location != -1) GL.Uniform1(location, value);
    }

    public void SetInt(string name, int value)
    {
        var location = GetUniformLocation(name);
        if (location != -1) GL.Uniform1(location, value);
    }

    public void SetBool(string name, bool value) => SetInt(name, value ? 1 : 0);

    public void Dispose()
    {
        if (_disposed) return;

        if (Handle != 0)
        {
            GL.DeleteProgram(Handle);
            Handle = 0;
        }

        _uniformLocations.Clear();
        _disposed = true;
    }

    private int GetUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var location = GL.GetUniformLocation(Handle, name);
        _uniformLocations[name] = location;
        return location;
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
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"GLSL {type} Compile Error: {infoLog}");
        }

        return shader;
    }
}
