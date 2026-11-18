using Coraline.Rendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Coraline;

public static class Program
{
    public static void Main()
    {
        var gameWindowSettings = GameWindowSettings.Default;
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720),
            Title = "Coraline: The Other World",
            APIVersion = new Version(4, 5),
            Flags = ContextFlags.ForwardCompatible | ContextFlags.Debug
        };

        using var game = new CoralineGame(gameWindowSettings, nativeWindowSettings);
        game.Run();
    }
}
