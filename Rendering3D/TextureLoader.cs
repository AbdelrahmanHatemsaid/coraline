using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace Coraline.OpenGLCore;

public readonly record struct DecodedTextureData(int Width, int Height, byte[] RgbaBytes);

public static class TextureLoader
{
    public static string ResolveAssetPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Textures", fileName);

    public static DecodedTextureData DecodeImage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Texture asset not found: {filePath}", filePath);
        }

        StbImage.stbi_set_flip_vertically_on_load(1);
        using var stream = File.OpenRead(filePath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return new DecodedTextureData(image.Width, image.Height, image.Data);
    }

    public static Texture2D Load(string fileName) => new(ResolveAssetPath(fileName));
}

public sealed class Texture2D : IDisposable
{
    private bool _disposed;

    public int Handle { get; private set; }
    public int Width { get; }
    public int Height { get; }

    public Texture2D(string filePath)
    {
        var decoded = TextureLoader.DecodeImage(filePath);
        Width = decoded.Width;
        Height = decoded.Height;

        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            decoded.Width,
            decoded.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            decoded.RgbaBytes);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (Handle != 0)
        {
            GL.DeleteTexture(Handle);
            Handle = 0;
        }

        _disposed = true;
    }
}
