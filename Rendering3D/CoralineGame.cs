using System.Runtime.InteropServices;
using Coraline.Core;
using Coraline.GameLogic;
using Coraline.OpenGLCore;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Coraline.Rendering;

public sealed class CoralineGame : GameWindow
{
    private static readonly DebugProc GlDebugDelegate = OnGlDebugMessage;

    private readonly OpenTkInputProvider _inputProvider = new();
    private readonly DebugRenderer _debugRenderer = new();
    private GameEventBus _eventBus = null!;
    private LevelMap _levelMap = null!;
    private GameWorldState _worldState = null!;
    private RenderSnapshot _currentSnapshot = new();

    public CoralineGame(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // 1. Register GL.DebugMessageCallback immediately (Ticket #1 requirement)
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(GlDebugDelegate, IntPtr.Zero);

        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.08f, 0.09f, 0.12f, 1.0f);

        // 2. Initialize DebugRenderer
        _debugRenderer.Initialize();

        // 3. Initialize EventBus & Load 24x16 3-Room JSON Map
        _eventBus = new GameEventBus();
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        _levelMap = MapLoader.LoadFromFile(mapPath);
        _worldState = new GameWorldState(_levelMap, _eventBus);
        _currentSnapshot = _worldState.CreateSnapshot();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        // 1. Poll Input -> IInputProvider
        _inputProvider.UpdateState(KeyboardState);

        // 2. Step Headless Logic (Stances, Raycast LOS, AI, Dual-World State)
        _worldState.Step((float)args.Time, _inputProvider);

        // 3. Build read-only RenderSnapshot for the frame
        _currentSnapshot = _worldState.CreateSnapshot();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Read ONLY from RenderSnapshot -> DebugRenderer
        _debugRenderer.Render(_currentSnapshot);

        Context.SwapBuffers();
    }

    protected override void OnUnload()
    {
        _debugRenderer.Dispose();
        base.OnUnload();
    }

    private static void OnGlDebugMessage(
        DebugSource source,
        DebugType type,
        int id,
        DebugSeverity severity,
        int length,
        IntPtr messagePtr,
        IntPtr userParam)
    {
        if (severity == DebugSeverity.DebugSeverityNotification)
        {
            return;
        }

        var message = Marshal.PtrToStringAnsi(messagePtr, length);
        Console.Error.WriteLine($"[OpenGL {severity}] ({source}/{type} #{id}): {message}");
    }
}
