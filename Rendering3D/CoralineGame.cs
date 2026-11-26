using System.Runtime.InteropServices;
using Coraline.Audio;
using Coraline.Core;
using Coraline.GameLogic;
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
    private readonly PrimitiveSceneRenderer _primitiveRenderer = new();
    private readonly ColorDrainPostProcessor _postProcessor = new();
    private readonly QuadHudRenderer _quadHud = new();
    private readonly TacticalCamera3D _camera = new();
    private LevelEffectsRenderer _effectsRenderer = null!;
    private GameEventBus _eventBus = null!;
    private LevelMap _levelMap = null!;
    private GameWorldState _worldState = null!;
    private RenderSnapshot _currentSnapshot = new();
    private NAudioPlayer _audioPlayer = null!;
    private AudioTriggerSystem _audioTriggerSystem = null!;
    private float _elapsedTime;
    private bool _useDebugRenderer;

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

        // 2. Initialize DebugRenderer (#3), PrimitiveSceneRenderer (#5), ColorDrain FBO (#11), & Quad HUD (#19)
        _debugRenderer.Initialize();
        _primitiveRenderer.Initialize();
        _postProcessor.Initialize(ClientSize.X, ClientSize.Y);
        _quadHud.Initialize();

        // 3. Initialize EventBus, EffectsRenderer (#9), Audio, & Load 24x16 3-Room JSON Map
        _eventBus = new GameEventBus();
        _effectsRenderer = new LevelEffectsRenderer(_eventBus);
        _effectsRenderer.Initialize();
        _audioPlayer = new NAudioPlayer();
        _audioTriggerSystem = new AudioTriggerSystem(_eventBus, _audioPlayer);
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", "level1_other_house.json");
        _levelMap = MapLoader.LoadFromFile(mapPath);
        _worldState = new GameWorldState(_levelMap, _eventBus);
        _currentSnapshot = _worldState.CreateSnapshot();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _postProcessor.Resize(e.Width, e.Height);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _camera.Zoom(-e.OffsetY * 0.8f);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        if (KeyboardState.IsKeyPressed(Keys.F1))
        {
            _useDebugRenderer = !_useDebugRenderer;
        }

        var dt = (float)args.Time;
        _elapsedTime += dt;

        // 1. Poll Input -> IInputProvider
        _inputProvider.UpdateState(KeyboardState);

        // 2. Step Headless Logic (Stances, Raycast LOS, AI, Dual-World State)
        _effectsRenderer.Update(dt);
        _audioTriggerSystem.Update(dt);
        _worldState.Step(dt, _inputProvider);

        // 3. Build read-only RenderSnapshot for the frame & smoothly follow player
        _currentSnapshot = _worldState.CreateSnapshot();
        var playerEntity = _currentSnapshot.Entities.FirstOrDefault(e => e.Type == EntityType.Player);
        _camera.FollowTarget(playerEntity.Position, dt);

        var mode = _useDebugRenderer ? "Debug" : "3D Primitives";
        Title = $"Coraline | {mode} (F1) | World: {_currentSnapshot.ActiveWorld} (Tab) | Detection: {_currentSnapshot.DetectionPercent:0}% | Eyes: {_currentSnapshot.CollectedEyes}/3";
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        var aspectRatio = ClientSize.Y > 0 ? ClientSize.X / (float)ClientSize.Y : 16f / 9f;
        var view = _camera.GetViewMatrix();
        var projection = _camera.GetProjectionMatrix(aspectRatio);

        // Render scene into offscreen FBO, apply Color-Drain Tension post-process, then draw crisp Quad HUD
        _postProcessor.BeginScene();
        if (_useDebugRenderer)
        {
            _debugRenderer.Render(_currentSnapshot, view, projection, include2DOverlay: false);
        }
        else
        {
            _primitiveRenderer.Render(_currentSnapshot, _camera, aspectRatio, _elapsedTime);
            _effectsRenderer.Render(_currentSnapshot, view, projection);
        }
        _postProcessor.EndSceneAndDraw(_currentSnapshot);
        _quadHud.Render(_currentSnapshot, aspectRatio);

        Context.SwapBuffers();
    }

    protected override void OnUnload()
    {
        _effectsRenderer.Dispose();
        _audioTriggerSystem.Dispose();
        _audioPlayer.Dispose();
        _quadHud.Dispose();
        _postProcessor.Dispose();
        _primitiveRenderer.Dispose();
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
        if (severity == DebugSeverity.DebugSeverityNotification) return;

        var message = Marshal.PtrToStringAnsi(messagePtr, length);
        Console.Error.WriteLine($"[OpenGL {severity}] ({source}/{type} #{id}): {message}");
    }
}
