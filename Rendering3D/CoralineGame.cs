using System.Runtime.InteropServices;
using Coraline.Core;
using Coraline.GameLogic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Coraline.Rendering;

public sealed class CoralineGame : GameWindow
{
    private static readonly DebugProc GlDebugDelegate = OnGlDebugMessage;

    private readonly OpenTkInputProvider _inputProvider = new();
    private readonly DebugRenderer _debugRenderer = new();
    private readonly TacticalCamera3D _camera = new();
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
        GL.ClearColor(0.06f, 0.07f, 0.10f, 1.0f);

        // 2. Initialize DebugRenderer (Ticket #3)
        _debugRenderer.Initialize();

        // 3. Initialize EventBus & Load 24x16 3-Room JSON Map
        _eventBus = new GameEventBus();
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        _levelMap = MapLoader.LoadFromFile(mapPath);
        _worldState = new GameWorldState(_levelMap, _eventBus);
        _currentSnapshot = _worldState.CreateSnapshot();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
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

        Title = $"Coraline: The Other World | World: {_currentSnapshot.ActiveWorld} (Tab) | Detection: {_currentSnapshot.DetectionPercent:0}% | Eyes: {_currentSnapshot.CollectedEyes}/3";
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var aspectRatio = Size.Y > 0 ? Size.X / (float)Size.Y : 16f / 9f;
        var view = _camera.GetViewMatrix();
        var projection = _camera.GetProjectionMatrix(aspectRatio);

        // Read ONLY from RenderSnapshot -> DebugRenderer
        _debugRenderer.Render(_currentSnapshot, view, projection);

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
