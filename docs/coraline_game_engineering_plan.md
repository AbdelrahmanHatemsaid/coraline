# CORALINE: THE OTHER WORLD
**Project Duration:** 2 Weeks (14 Days)  
**Team:** Abdelrahman & Noor  
**Tech Stack:** C# .NET 8 / OpenTK 4 (OpenGL 4.5) / NAudio / StbImageSharp / xUnit  

A Native 3D Isometric Tactical Stealth game engineered around SOLID C# Architecture, GLSL Shader Pipelines, Headless Automated xUnit Testing, and strict GitHub PR Workflows.

---

## 1. Gameplay Design & Signature "Other World" Twist

To elevate the project beyond a standard vision-cone stealth game, the **Other World** is a core playable mechanic rather than just a visual theme:

* **Signature Twist — Dual-World Shifting (Real World vs. Other World):**
  * **Visibility vs. Layout Trade-off:** The Beldam's patrolling constructs and the 3 hidden **Ghost Eyes** can **only be seen in the Other World**. However, the house layout differs slightly between dimensions—certain doorways are bricked up in the Other World while open in the Real World (and vice versa).
  * **Tactical Tension:** Players must toggle their **Seeing Stone** to shift/peer into the Other World to track patrol routes, spot traps, and collect Ghost Eyes, then shift back to slip through Real World shortcuts—knowing that guards can still hear sprinting footsteps across the veil.
* **Signature Visual Feedback — Color-Drain Tension:**
  * As the **Detection Meter** rises from `0%` to `100%`, the OpenGL post-processing shader progressively **drains the screen of color** (shifting toward cold grayscale with a tightening dark vignette), mirroring the dread of being sewn into the Other World.
* **Core Stealth Loop:**
  * Navigate a fixed **3-room isometric tile grid** (Drawing Room, Kitchen, Bedroom) using `SneakStance` (silent, slow) and `SprintStance` (fast, emits `NoiseEmittedEvent` ripples).
  * Throw distractions to pull patrolling constructs away from choke points.
* **How to Win (Extraction):**
  1. Collect all **3 Ghost Eyes** hidden across the 3 rooms (only visible/collectible in the Other World).
  2. Return to the Drawing Room and complete a hold-interaction on the **Small Door** (`IInteractable`) without being caught.
* **How to Lose (Caught State):**
  * Entering a guard's **wall-clipped Line-of-Sight (LOS)** vision cone fills the Detection Meter (`0% -> 100%`).
  * At `100%`, guards enter `AlertChaseState`, using A* pathfinding to pursue Coraline. Physical contact triggers the **Button Eyes** Game Over state.

---

## 2. Core Technical Stack & Early Debugging

* **Window, OpenGL & Input:** **OpenTK** (`GameWindow`, OpenGL 4.5 bindings, `KeyboardState` / `MouseState`).
* **Math Library:** **OpenTK.Mathematics** (`Vector2`, `Vector3`, `Matrix4`) shared across logic and rendering with zero conversion overhead.
* **Audio System:** **NAudio** for independent playback of footsteps, distractions, world-shift chimes, and alert stingers.
* **OpenGL Debug Callback (Day 1 Requirement):** Enable `GL.DebugMessageCallback` (`EnableCap.DebugOutput` + `DebugOutputSynchronous`) in Ticket #1 so shader compilation errors, invalid enum calls, and VAO/VBO leaks immediately print file/line diagnostics to the console.

### The Game Loop Architecture (OpenTK)
```csharp
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;

public class CoralineGame : GameWindow
{
    public CoralineGame(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) {}

    protected override void OnLoad()
    {
        // 1. Register GL.DebugMessageCallback immediately (Abdelrahman)
        // 2. Load Textures, Compile Shaders, Init DebugRenderer (Abdelrahman)
        // 3. Init EventBus, Load 24x16 3-Room JSON Map (Noor)
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        // 1. Poll Input -> IInputProvider
        // 2. Step Headless Logic (Stances, Raycast LOS, AI, Dual-World State)
        // 3. Build read-only RenderSnapshot for the frame
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        // Read ONLY from RenderSnapshot -> Draw3D (or DebugRenderer) -> PostProcess -> Quad HUD
        Context.SwapBuffers();
    }
}
```

---

## 3. Scope Guardrails & Shared Contracts (Time-Savers)

To guarantee a finished, polished game in 14 days, the following technical constraints are locked from Day 1:

1. **Shared `RenderSnapshot` Contract (Settled in Ticket #2):**
   The logic layer never calls OpenGL. Each frame, `GameLogic` fills a read-only snapshot that `Rendering` consumes:
   ```csharp
   public enum WorldDimension { RealWorld, OtherWorld }

   public readonly record struct EntityRenderState(
       int EntityId,
       EntityType Type,
       Vector2 Position,
       float RotationRad,
       string StateTag,
       WorldDimension VisibleIn
   );

   public sealed class RenderSnapshot
   {
       public WorldDimension ActiveWorld { get; init; }
       public float DetectionPercent { get; init; } // Drives Color-Drain shader & HUD meter
       public int CollectedEyes { get; init; }
       public IReadOnlyList<EntityRenderState> Entities { get; init; } = [];
       public IReadOnlyList<(Vector2 Start, Vector2 End)> DebugLines { get; init; } = [];
   }
   ```
2. **Primitive-Only 3D Geometry (No Model Loader):**
   All 3D walls, floors, doors, furniture, and props are constructed strictly from **OpenGL geometric primitives** (cubes, cylinders, planes) combined with textured quads/billboards for characters. **Do not** integrate Assimp or `.obj`/`.gltf` loaders.
3. **Iconic Quad-Only HUD (No Bitmap Font Engine):**
   All HUD elements—Detection Meter bar, Button/Needle danger icon, 3 Ghost Eye slots, and Seeing Stone cooldown—are rendered as **textured 2D quads and colored progress bars**. No text rendering or bitmap font atlas is required.
4. **Fixed Level Scope (Locked by Day 2):**
   The game uses a single, tightly designed **24×16 tile grid** representing **3 interconnected rooms** of the Other House (*Drawing Room*, *Kitchen*, *Bedroom*). Each tile in `level1_other_house.json` carries flags for `RealWorldWalkable` and `OtherWorldWalkable` so both engineers can author and test levels from Day 2.

---

## 4. Architecture, Ownership & Asset Pipeline

* **Abdelrahman (`Coraline.OpenGLCore`, `Coraline.Rendering`):** OpenTK `GameWindow`, `GL.DebugMessageCallback`, Debug Renderer (colored quads/lines), Primitive Mesh Builders, VAO/VBO `IDisposable` lifecycle, GLSL Shaders (Toon, X-Ray Seeing Stone, Color-Drain FBO Post-Processor), Quad HUD, Asset Pipeline.
* **Noor (`Coraline.GameLogic`, `Coraline.AI`):** `RenderSnapshot`, `GameEventBus`, `MapLoader` (24×16 JSON), Headless Stances (`IPlayerStance`), Wall-Clipped Raycast Line-of-Sight, Detection Meter, Dual-World State, A* Pathfinding, Guard FSM, NAudio Triggers, xUnit Tests.

### Directory Structure
```text
/CoralineProject
├── /Assets
│   ├── /Textures
│   │   ├── coraline_player.jpg
│   │   ├── beldam_enemy.jpg
│   │   ├── small_door.jpg
│   │   ├── ui_seeing_stone.jpg
│   │   ├── ui_ghost_eye.jpg
│   │   └── ui_detection_meter.jpg
│   ├── /Shaders
│   │   ├── primitive_vertex.glsl
│   │   ├── primitive_fragment.glsl
│   │   └── post_colordrain.glsl
│   ├── /Maps
│   │   └── level1_other_house.json
│   └── /Audio
│       └── footsteps.wav
├── /Core          # Shared interfaces, RenderSnapshot, EventBus
├── /GameLogic     # Noor: Headless stealth, LOS raycasts, AI FSM, A*
├── /Rendering3D   # Abdelrahman: OpenTK, DebugRenderer, Primitives, Shaders
├── /Tests         # xUnit headless tests
└── CoralineGame.csproj
```

* **`.csproj` Asset Copy Rule:** Include `<None Update="Assets\**\*.*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`.
* **Texture Decoding:** Use `StbImageSharp` inside `TextureLoader` to decode images into RGBA byte arrays for `GL.TexImage2D`. Core logic never references filenames or texture IDs.

---

## 5. Two-Week Sprint Plan & Playable Milestones

### Week 1: Vertical Slice by Day 4 (Debug Renderer, LOS & Dual-World Core)
* **#1 (Abdelrahman)** — `feat/abdelrahman-gl-context`  
  OpenTK `GameWindow`, `GL.DebugMessageCallback` setup, basic Render Loop, and `.csproj` asset copy rules.
* **#2 (Noor)** — `feat/noor-core-contracts`  
  `GameEventBus`, Core Interfaces (`IGameSystem`, `IInteractable`), and the shared read-only `RenderSnapshot` struct.
* **#3 (Abdelrahman)** — `feat/abdelrahman-debug-renderer`  
  **Early Debug Renderer:** Colored 2D/3D quads and debug lines (`GL.DrawArrays` for quads/lines) reading `RenderSnapshot` so Noor can visually verify guards, vision cones, and A* paths by Day 3.
* **#4 (Noor)** — `feat/noor-json-map-grid`  
  Fixed **24×16 3-room JSON map schema** (`level1_other_house.json`) with `RealWorldWalkable` / `OtherWorldWalkable` tile flags and `MapLoader`.
* **#5 (Abdelrahman)** — `feat/abdelrahman-camera-primitives`  
  `TacticalCamera3D` (isometric View/Projection matrices), `ShaderLoader`, `TextureLoader` (`StbImageSharp`), and primitive cube/cylinder builders.
* **#6 (Noor)** — `feat/noor-stances-collision`  
  `IInputProvider` (OpenTK `KeyboardState`), `SneakStance` / `SprintStance`, Dual-World tile collision, and **Raycast Line-of-Sight (`HasLineOfSight`)** so walls block vision.
* **#7 (Team — Day 4 Milestone)** — `chore/team-vertical-slice-playtest`  
  **Playable Day 4 Checkpoint:** Walk Coraline around the 3-room grid in the Debug Renderer, toggle Real/Other World layout, and verify walls block guard LOS rays.
* **#8 (Noor)** — `feat/noor-patrol-detection`  
  Enemy `PatrolState` FSM + **Detection Meter (`0% -> 100%`)** driven by wall-clipped LOS raycasts (active when spotted in Other World or via proximity).
* **#9 (Abdelrahman)** — `feat/abdelrahman-level-meshes`  
  Render the 3-room 24×16 grid using primitive cubes/planes, textured character billboards, and 3D wall-clipped vision cone geometry.
* **#10 (Noor)** — `feat/noor-audio-noise`  
  `NAudio` wrapper + `NoiseEmittedEvent` triggered by sprinting and thrown distractions.

### Week 2: Signature Twist Polish, A* Chase, Gadgets & Extraction
* **#11 (Abdelrahman)** — `feat/abdelrahman-colordrain-fbo`  
  FBO Post-Processor driving the **Color-Drain Tension shader** (progressively desaturates screen as `DetectionPercent` approaches 100%) + Other World visual tint.
* **#12 (Noor)** — `feat/noor-astar-chase`  
  `AlertChaseState` using grid-based **A* Pathfinding** (respecting Other World layout) to pursue Coraline when Detection hits 100%.
* **#13 (Abdelrahman)** — `feat/abdelrahman-toon-outline`  
  Primitive Toon Shading & Comic Outline shader pass for walls and props.
* **#14 (Noor)** — `feat/noor-seeing-stone-dualworld`  
  **Seeing Stone & Dual-World Mechanic:** Toggle between Real/Other World, reveal guards & Ghost Eyes only in Other World, and track 3-Eye Inventory.
* **#15 (Abdelrahman)** — `feat/abdelrahman-xray-shader`  
  Seeing Stone X-Ray highlight pass for Ghost Eyes and guard silhouettes through walls.
* **#16 (Noor)** — `feat/noor-win-loss-extraction`  
  Win/Loss Game State: Hold-interaction on the **Small Door** (requires 3 Ghost Eyes) for Win; enemy contact during chase for **Caught** Game Over.
* **#17 (Abdelrahman)** — `feat/abdelrahman-primitive-props`  
  Build 3D props purely from primitives + textures (Small Door, Ghost Eye pedestals, sticky web traps).
* **#18 (Noor)** — `test/noor-headless-xunit`  
  Automated xUnit test suite covering Raycast LOS, Dual-World collisions, A* Pathfinding, Detection Meter, and Win/Loss states.
* **#19 (Abdelrahman)** — `feat/abdelrahman-quad-hud`  
  **Quad-Only 2D HUD Overlay:** Detection Meter bar, Needle/Button danger icon, 3 Ghost Eye slots, and Button Eyes Game Over overlay (zero bitmap fonts).
* **#20 (Team — Final Polish)** — `fix/team-final-playtest`  
  Full playtest of 3-room heist loop, GPU `IDisposable` leak check, and final PR merges.

---

## 6. GitHub Team Workflow & Peer Review

* **Strict Branch Naming:** Every branch must follow `<type>/<owner>-<feature>` (e.g., `feat/abdelrahman-debug-renderer`, `feat/noor-astar-chase`). Direct pushes to `main` are disabled.
* **Pre-Merge Gate:** `dotnet build` (0 errors) and `dotnet test` (100% passing headless xUnit tests) must succeed locally before opening a PR.
* **Peer Review Matrix:**
  * **Abdelrahman reviews Noor:** Checks `RenderSnapshot` immutability, LOS/A* math edge cases, and `<200`-line file discipline.
  * **Noor reviews Abdelrahman:** Checks `IDisposable` GPU buffer/shader cleanup, zero logic leakage into rendering, and `GL.DebugMessageCallback` cleanliness.