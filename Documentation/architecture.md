# PuzzleEngineSandbox – Architecture

> **Status:** WIP, but stable enough for development  
> **Unity:** 6000.x (Unity 6)  
> **Domain:** Grid-based, rule-driven puzzle engine + tooling

---

## 1. Purpose & Scope

PuzzleEngineSandbox is a **systems-first** grid puzzle engine built for:

- Fast iteration on **tile-based puzzle mechanics** (merge / combine / transform).
- Clean separation between **core simulation logic** and **Unity presentation**.
- Serving as a **portfolio-grade codebase** that matches AAA / Metacore-style expectations:
  - deterministic logic
  - testable core
  - data-driven content
  - editor tooling for designers

The repository hosts both:

1. A reusable **Puzzle Engine** (domain + simulation + rules).
2. A **demo game layer** (scenes, visuals, input) showcasing how to use that engine.

---

## 2. Design Principles

1. **Core first, presentation second**  
   - Engine code (grid, rules, simulator) is independent of UI and scene layout.
2. **Data-driven configuration**  
   - Grid size, tile families and interaction rules come from ScriptableObjects, not hard-coded enums.
3. **Single source of truth**  
   - The `GridModel` holds the canonical puzzle state; views are projections.
4. **Deterministic simulation**  
   - Rule application and simulation steps are pure and deterministic given an initial state.
5. **Explicit dependencies**  
   - Dependencies flow inward: Unity behaviours → façade → core.  
   - No core types depend on MonoBehaviours or scene objects.
6. **Editor-friendly**  
   - Gizmos and authoring tools live in dedicated namespaces/folders and are safe to strip for builds.

---

## 3. Folder & Namespace Layout

High-level layout under `Assets/`:

```text
Assets
├─ 3rdParty/                # External packages (DOTween etc.) – optional
├─ Art/                     # Visual assets for demo only
├─ PuzzleEngine/
│  ├─ Content/
│  │  ├─ Database/          # TileDatabase SOs
│  │  ├─ Grid/              # GridConfig SOs
│  │  ├─ Rules/             # RuleSet SOs
│  │  └─ Tiles/             # Individual TileData assets (if authored as SOs)
│  ├─ Runtime/
│  │  ├─ Core/              # GridModel, TileData, PuzzleManager, facades
│  │  ├─ Rules/             # Rule, RuleEngine implementations
│  │  └─ Simulation/        # GridSimulator and stepper logic
│  ├─ Debug/                # GridGizmoRenderer & debug utilities
│  └─ Editor/               # Future: level editor, inspectors, tooling
├─ Scenes/
│  └─ PuzzleEngineDemo.unity
└─ Settings/, Tests/, etc.
```

Namespaces mirror this layout:

- `PuzzleEngine.Core` – domain façade + grid model
- `PuzzleEngine.Rules` – rule definitions and rule engine
- `PuzzleEngine.Simulation` – simulation pipeline
- `PuzzleEngine.Debugs` – gizmos & debug helpers
- Future:
  - `PuzzleEngine.Editor.*` – editor tools, inspectors
  - `PuzzleEngine.Demo.*` – demo-game specific code

---

## 4. High-Level Architecture

```mermaid
flowchart TD

    subgraph UnityLayer[Unity Layer]
        PM[PuzzleManager (MonoBehaviour)]
        Views[Views / Presentation]
        Input[Input & Controllers]
        SOs[ScriptableObjects<br/>GridConfigSO / TileDatabaseSO / RuleSetSO]
        Gizmos[GridGizmoRenderer]
    end

    subgraph Core[Puzzle Engine Core]
        GM[GridModel]
        RE[RuleEngine]
        GS[GridSimulator]
    end

    Input --> PM
    Views --> PM

    PM --> GM
    PM --> RE
    PM --> GS

    SOs --> PM

    GM <--> RE
    GM <--> GS
    RE --> GS

    Gizmos --> PM
    Gizmos --> GM
```
- **Unity Layer**: everything MonoBehaviour / scene-bound.
- **Puzzle Engine Core**: pure C# logic, independent of scenes and GameObjects.

---

## 5. Core Domain Model

### 5.1 GridModel

**Namespace:** `PuzzleEngine.Core`

Represents the **logical grid** – single source of truth for tiles.

Key responsibilities:

- Store `TileData` per `(x, y)` cell.
- Bounds checking (`IsInside`).
- Get/set operations (`Get`, `Set`).
- Potential helpers for iteration (`ForEach`, row/column accessors – future).

Constraints:

- No direct references to Unity types.
- No knowledge of visuals, input, or physics.

---

### 5.2 TileData

**Namespace:** `PuzzleEngine.Core` (or shared domain namespace)

Represents a **tile instance** at a grid position.

Typical fields (current + planned):

- `TileId` / `TileType` (string or struct identifier)
- Optional metadata:
  - power level
  - flags (e.g. blocking, combinable, special)
- Static reference to “empty” tile: `TileData.Empty`

`TileData` is intentionally small and cheap to copy.

---

### 5.3 RuleEngine & RuleSet

**Namespace:** `PuzzleEngine.Rules`

**`RuleSetSO`** (ScriptableObject):

- Author-time definition of all interaction rules for a puzzle set.
- Stored under `Content/Rules`.

**`RuleEngine`** (runtime class):

- Built at startup from `TileDatabaseSO` + `RuleSetSO`.
- Executes **local interaction rules** like:
  - merge A + B → C
  - block certain combinations
- Provides a single API surface, e.g.:

```csharp
bool TryApply(TileData a, TileData b, out TileData newA, out TileData newB);
```

Design goals:

- Rules are **data-driven** (SOs) but compiled into efficient runtime lookups.
- The engine can be unit-tested without Unity.

---

### 5.4 GridSimulator

**Namespace:** `PuzzleEngine.Simulation`

Deterministic simulation stepper that:

- Consumes a `GridModel` + `RuleEngine`.
- Encodes “global” puzzle behaviour, e.g.:
  - gravity / falling tiles
  - chain reactions
  - cascades / matching logic

API shape:

```csharp
public class GridSimulator
{
    public bool Step(GridModel grid);
}
```

- `Step` mutates the grid in place.
- Returns `true` if any tiles changed (useful for “run until stable”).

---

## 6. Unity Façade – PuzzleManager

**Namespace:** `PuzzleEngine.Core`  
**Type:** `PuzzleManager : MonoBehaviour`

Central MonoBehaviour that **owns and wires** all core systems for a given scene.

Serialized fields:

- `GridConfigSO gridConfig` – grid width/height, origin, maybe cell size.
- `TileDatabaseSO tileDatabase` – registry of all tiles.
- `RuleSetSO ruleSet` – interaction rules to build `RuleEngine` from.

Runtime properties:

- `GridModel Grid { get; private set; }`
- `RuleEngine RuleEngine { get; private set; }`
- `GridSimulator Simulator { get; private set; }`

Lifecycle:

```csharp
private void Awake()
{
    EnsureGridConfig();
    InitializeGrid();
    InitializeRules();
}
```

Responsibilities:

- Ensure a valid `GridConfigSO` (or create an in-memory default for safety).
- Construct the `GridModel` with the configured size.
- Instantiate `RuleEngine` and `GridSimulator` (if content is provided).
- Provide **safe helper methods** for callers:
  - `TrySetTile(int x, int y, TileData tile)`
  - `TryGetTile(int x, int y, out TileData tile)`
  - `TryApplyRuleBetween(int x1, int y1, int x2, int y2)`
  - `StepSimulation()`
  - `RunUntilStable(int maxSteps = 64)`

Editor-only helpers:

```csharp
#if UNITY_EDITOR
public GridConfigSO GetGridConfigForDebug() => gridConfig;
public TileDatabaseSO GetTileDatabaseForDebug() => tileDatabase;
#endif
```

These are used by gizmos and editor tools without polluting runtime logic.

---

## 7. Debug & Tooling

### 7.1 GridGizmoRenderer

**Namespace:** `PuzzleEngine.Debugs`  
**Type:** `GridGizmoRenderer : MonoBehaviour`  
**Attached to:** Same GameObject as `PuzzleManager` in the demo scene.

Purpose:

- Draw an overlay of the logical grid in the Scene view.
- Provide a visual debugging aid for content authors.

Key properties (serialized):

- `bool drawGrid`
- `bool drawTiles` (future)
- `float cellSize`
- `Vector2 origin`
- `bool drawLabels`
- `Color emptyColor`

Behaviour (editor-only):

- In `OnDrawGizmos`:
  - Determine `width` / `height`:
    - Prefer `PuzzleManager.Grid` when in play mode.
    - Fallback to `GridConfigSO.width/height` via `GetGridConfigForDebug()` in edit mode.
  - Draw `Gizmos.DrawWireCube` for each cell.
  - Optionally label cells via `Handles.Label(x,y)` when `drawLabels` is enabled.

Design notes:

- No simulation logic here: **purely visual**.
- Lives in a separate namespace so it can be excluded from builds if needed.

---

## 8. Data Flow

### 8.1 Scene Startup

1. Scene loads `PuzzleEngineDemo`.
2. `PuzzleManager.Awake()` executes:
   - `EnsureGridConfig()` validates or creates a grid config.
   - `InitializeGrid()` creates `GridModel(width, height)`.
   - `InitializeRules()` constructs `RuleEngine` and `GridSimulator`.
3. Demo-layer controllers (input, UI) obtain a reference to `PuzzleManager` and interact only through its public API.

### 8.2 Simulation Step

1. Player action modifies tiles via `PuzzleManager` helpers:
   - e.g. swapping, placing, or interacting with two coordinates through `TryApplyRuleBetween`.
2. `PuzzleManager.StepSimulation()` is called:
   - internally calls `Simulator.Step(Grid)`.
3. Views are notified to refresh (via events / polling – TBD in demo layer).
4. For cascading behaviour, `RunUntilStable(maxSteps)` can be used to run multiple steps until no changes occur.

---

## 9. Extension Points & Planned Work

The core is intentionally small but structured to grow. Planned additions:

- **Level / Puzzle Data**
  - ScriptableObject definitions for authored levels.
  - Preset initial grid states (tiles, obstacles, goals).
- **Goal System**
  - Data-driven goals (score threshold, clear X tiles, survive N turns, etc.).
  - Separate domain model and evaluator (`GoalSet`, `GoalEvaluator`).
- **Level Editor (Editor Tools)**
  - Custom editor window for painting tiles into a grid.
  - Rule and tile visualizer.
  - One-click export/import for levels.
- **View Layer**
  - Tile view poolers and adapters that bind `GridModel` state to sprites.
  - Simple animation layer separated from core state.
- **Persistence**
  - Save/load of puzzle state and player progress.
- **Testing**
  - PlayMode/ EditMode tests for rules and simulation (pure C# tests against core model).

---

## 10. Dependency Rules

To keep the architecture clean:

- `PuzzleEngine.Core`, `PuzzleEngine.Rules`, `PuzzleEngine.Simulation`  
  **must not depend on**:
  - MonoBehaviours
  - Scenes / GameObjects
  - UI systems

- `PuzzleManager` is the **only allowed entry point** from Unity into the core.

- Editor tooling (`PuzzleEngine.Editor.*`, `PuzzleEngine.Debugs`)  
  may depend on core and UnityEditor, but **not the other way around**.

Enforcing these rules keeps the engine:

- Testable in isolation.
- Re-usable across different Unity projects.
- Easy to read for reviewers from AAA studios.

---

## 11. Checklist for Contributors

Before opening a PR:

- [ ] New core types live under `PuzzleEngine.Core / Rules / Simulation` and are Unity-free.
- [ ] Interactions with the grid go through `PuzzleManager` or well-defined APIs.
- [ ] Debug / gizmo code is in `PuzzleEngine.Debugs` or `PuzzleEngine.Editor.*`.
- [ ] Public APIs are named clearly and documented with XML summaries.
- [ ] No direct access to tiles from views without going through the domain model.
- [ ] New systems and decisions are reflected here in `ARCHITECTURE.md`.

---
