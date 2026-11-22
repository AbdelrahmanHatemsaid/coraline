using Coraline.Core;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;

namespace Coraline.Rendering;

public readonly record struct PostProcessEffects(
    float DetectionPercent,
    float DrainFactor,
    float OtherWorldBlend,
    float VignetteRadius
);

public sealed class ColorDrainPostProcessor : IDisposable
{
    private ShaderProgram? _shader;
    private int _fbo;
    private int _colorTexture;
    private int _depthStencilRbo;
    private int _quadVao;
    private int _quadVbo;
    private int _width;
    private int _height;
    private bool _disposed;

    public static PostProcessEffects ComputeEffects(RenderSnapshot snapshot)
    {
        var clampedDetection = Math.Clamp(snapshot.DetectionPercent, 0.0f, 100.0f);
        var drainFactor = clampedDetection / 100.0f;
        var otherBlend = snapshot.ActiveWorld == WorldDimension.OtherWorld ? 1.0f : 0.0f;
        var vignetteRadius = 1.45f - (drainFactor * 0.72f);

        return new PostProcessEffects(clampedDetection, drainFactor, otherBlend, vignetteRadius);
    }

    public void Initialize(int width, int height)
    {
        var shaderDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
        _shader = new ShaderProgram(
            Path.Combine(shaderDir, "post_vertex.glsl"),
            Path.Combine(shaderDir, "post_colordrain.glsl"));

        CreateFullscreenQuad();
        AllocateFramebuffer(Math.Max(1, width), Math.Max(1, height));
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (width == _width && height == _height))
        {
            return;
        }

        ReleaseFramebuffer();
        AllocateFramebuffer(width, height);
    }

    public void BeginScene()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.Viewport(0, 0, _width, _height);
        GL.Enable(EnableCap.DepthTest);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void EndSceneAndDraw(RenderSnapshot snapshot)
    {
        if (_shader is null) return;

        var effects = ComputeEffects(snapshot);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Disable(EnableCap.DepthTest);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _shader.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _colorTexture);
        var texLoc = GL.GetUniformLocation(_shader.Handle, "uScreenTexture");
        if (texLoc != -1) GL.Uniform1(texLoc, 0);

        _shader.SetFloat("uDetectionPercent", effects.DetectionPercent);
        _shader.SetFloat("uOtherWorldBlend", effects.OtherWorldBlend);
        _shader.SetFloat("uVignetteRadius", effects.VignetteRadius);

        GL.BindVertexArray(_quadVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        if (_disposed) return;

        ReleaseFramebuffer();
        if (_quadVbo != 0) GL.DeleteBuffer(_quadVbo);
        if (_quadVao != 0) GL.DeleteVertexArray(_quadVao);
        _shader?.Dispose();
        _quadVbo = 0;
        _quadVao = 0;
        _disposed = true;
    }

    private void CreateFullscreenQuad()
    {
        float[] quadVertices =
        [
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        ];

        _quadVao = GL.GenVertexArray();
        _quadVbo = GL.GenBuffer();
        GL.BindVertexArray(_quadVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        const int stride = 4 * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.BindVertexArray(0);
    }

    private void AllocateFramebuffer(int width, int height)
    {
        _width = width;
        _height = height;

        _fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        _colorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _colorTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTexture, 0);

        _depthStencilRbo = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthStencilRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthStencilRbo);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new InvalidOperationException($"ColorDrain FBO incomplete: {status}");
        }
    }

    private void ReleaseFramebuffer()
    {
        if (_depthStencilRbo != 0) GL.DeleteRenderbuffer(_depthStencilRbo);
        if (_colorTexture != 0) GL.DeleteTexture(_colorTexture);
        if (_fbo != 0) GL.DeleteFramebuffer(_fbo);
        _depthStencilRbo = 0;
        _colorTexture = 0;
        _fbo = 0;
    }
}
