# CORALINE: THE OTHER WORLD

**Project Duration:** 2 Weeks (14 Days)  
**Team:** Abdelrahman & Noor  
**Tech Stack:** C# .NET 8 / OpenTK 4 (OpenGL 4.5) / NAudio / StbImageSharp / xUnit  

A Native 3D Isometric Tactical Stealth game engineered around SOLID C# Architecture, GLSL Shader Pipelines, Headless Automated xUnit Testing, and strict GitHub PR Workflows.

---

## 1. Gameplay Design & Signature "Other World" Twist

* **Dual-World Shifting (Real World vs. Other World):**
  * **Visibility vs. Layout Trade-off:** The Beldam's patrolling constructs and the 3 hidden **Ghost Eyes** can **only be seen in the Other World**. However, the house layout differs between dimensions—certain doorways are bricked up in the Other World while open in the Real World (and vice versa).
  * **Tactical Tension:** Toggle the **Seeing Stone** (`Tab`) to peer into the Other World to track patrol routes, spot traps, and collect Ghost Eyes, then shift back to slip through Real World shortcuts—knowing guards can still hear sprinting footsteps across the veil.
* **Color-Drain Tension Shader:**
  * As the **Detection Meter** rises from `0%` to `100%`, the OpenGL FBO post-processing shader progressively drains the screen of color toward cold grayscale with a tightening dark vignette.
* **Win / Loss Conditions:**
  * **Win:** Collect all **3 Ghost Eyes** in the Other World and return to the Drawing Room to complete a hold-interaction (`E`) on the **Small Door**.
  * **Loss:** Entering a guard's wall-clipped Line-of-Sight vision cone fills the Detection Meter to `100%`, triggering `AlertChaseState` (A* pursuit). Physical contact triggers the **Button Eyes** Game Over state.

---

## 2. Repository Structure & Ownership

* **Abdelrahman (`Coraline.OpenGLCore`, `Coraline.Rendering`):** OpenTK `GameWindow`, `GL.DebugMessageCallback`, `DebugRenderer`, Primitive Mesh Builders, VAO/VBO `IDisposable` lifecycle, GLSL Shaders (Toon, X-Ray Seeing Stone, Color-Drain FBO Post-Processor), Quad HUD, Asset Pipeline.
* **Noor (`Coraline.GameLogic`, `Coraline.AI`):** `RenderSnapshot`, `GameEventBus`, `MapLoader` (24×16 JSON), Headless Stances (`IPlayerStance`), Wall-Clipped Raycast Line-of-Sight, Detection Meter, Dual-World State, A* Pathfinding, Guard FSM, NAudio Triggers, xUnit Tests.

```text
/
├── Assets/
│   ├── Textures/
│   │   ├── coraline_player.jpg
│   │   ├── beldam_enemy.jpg
│   │   ├── small_door.jpg
│   │   ├── ui_seeing_stone.jpg
│   │   ├── ui_ghost_eye.jpg
│   │   └── ui_detection_meter.jpg
│   ├── Shaders/
│   │   ├── primitive_vertex.glsl
│   │   ├── primitive_fragment.glsl
│   │   └── post_colordrain.glsl
│   ├── Maps/
│   │   └── level1_other_house.json
│   └── Audio/
│       ├── footsteps.wav
│       ├── distraction.wav
│       └── alert.wav
├── Core/          # Shared interfaces, RenderSnapshot, GameEventBus
├── GameLogic/     # Noor: Headless stealth, LOS raycasts, AI FSM, A*
├── Rendering3D/   # Abdelrahman: OpenTK, DebugRenderer, Primitives, Shaders
├── Tests/         # Automated headless xUnit test suite
├── docs/          # Engineering specification (Markdown & PDF)
├── Coraline.sln
└── CoralineGame.csproj
```

---

## 3. Build, Run & Test Commands

```bash
# Build entire solution (0 errors gate)
dotnet build Coraline.sln

# Run headless xUnit test suite
dotnet test Coraline.sln

# Launch the game
dotnet run --project CoralineGame.csproj
```

---

## 4. GitHub Workflow & Branch Naming

* **Branch Naming Convention:** `<type>/<owner>-<feature>` (e.g., `feat/abdelrahman-gl-context`, `feat/noor-core-contracts`).
* **Pre-Merge Gate:** `dotnet build` and `dotnet test` must pass before requesting peer review.
* Full sprint breakdown and specification are located in [docs/coraline_game_engineering_plan.md](docs/coraline_game_engineering_plan.md).
