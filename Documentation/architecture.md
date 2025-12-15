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
2. A **demo game layer** (scenes, visuals, input, visuals) showcasing how to use that engine.

---

## 2. Design Principles

1. **Core first, presentation second**  
   - Engine code (grid, rules, simulator) is independent of UI and scene layout.
2. **Data-driven configuration**  
   - Grid size, tile families, merge rules and interaction rules come from ScriptableObjects, not hard-coded enums.
3. **Single source of truth**  
   - The `GridModel` holds the canonical puzzle state; views are projections.
4. **Deterministic simulation**  
   - Rule application and simulation steps are pure and deterministic given an initial state.
5. **Explicit dependencies**  
   - Dependencies flow inward: Unity behaviours → façade → core.  
   - No core types depend on MonoBehaviours or scene objects.
6. **Editor-friendly**  
   - Gizmos and authoring tools live in dedicated namespaces/folders and are safe to strip for builds.
7. **Configurable interaction layer**  
   - “What happens when tiles meet?” (merge rules) is separated from  
     “Which tiles can the player make meet, and how far does it cascade?” (interaction rules).
8. **Clear separation of concerns**  
   - Core logic (model + rules + simulation), interaction (input + interaction rules),
     and view (GridView / TileView) are distinct layers.

---

## 3. Folder & Namespace Layout

High-level layout under `Assets/`:

```text
Assets
├─ 3rdParty/                        # External packages (DOTween etc.) – optional
├─ Art/                             # Visual assets for demo only
├─ PuzzleEngine/
│  ├─ Content/
│  │  ├─ Database/                  # TileDatabase SOs
│  │  ├─ Grid/                      # GridConfig SOs
│  │  ├─ Layouts/                   # LevelLayoutSO assets
│  │  ├─ Prefabs/                   # TileView prefab, etc.
│  │  ├─ Rules/                     # RuleSetSO, MergeRulesSO, InteractionRuleSO instances
│  │  └─ Tiles/                     # Individual TileTypeSO assets
│  ├─ Runtime/
│  │  ├─ Core/                      # GridModel, TileData, PuzzleManager, GridClickController, InteractionRuleSO, LevelLayoutSO
│  │  ├─ Rules/                     # MergeRulesSO, RuleSetSO, TileDatabaseSO, TileTypeSO
│  │  ├─ Simulation/                # GridSimulator and stepper logic
│  │  ├─ View/                      # GridView, TileView
│  │  └─ Debugs/                    # GridGizmoRenderer & debug utilities
│  ├─ Editor/
│  │  └─ GridEditor/                # PuzzleGridEditorWindow and related tooling
│  └─ Tests/
│     └─ EditMode/                  # RuleEngineMergeTests, InteractionRuleEvaluatorTests, etc.
├─ Scenes/
│  └─ PuzzleEngineDemo.unity
└─ Settings/, Tests/, etc.
```

Namespaces mirror this layout:

- `PuzzleEngine.Runtime.Core`       – domain façade, grid model, interaction rules  
- `PuzzleEngine.Runtime.Rules`      – tile definitions, merge rules, rule engine  
- `PuzzleEngine.Runtime.Simulation` – simulation pipeline  
- `PuzzleEngine.Runtime.View`       – views / presentation (GridView / TileView)  
- `PuzzleEngine.Runtime.Debugs`     – gizmos & debug helpers  
- `PuzzleEngine.Editor.*`           – editor tools, inspectors  
- `PuzzleEngine.Tests.*`            – test assemblies  

---

## 4. High-Level Architecture

```mermaid
flowchart TD

    subgraph UnityLayer[Unity Layer]
        PM[PuzzleManager (MonoBehaviour)]
        Input[GridClickController<br/>(Input & Interaction)]
        Views[GridView / TileView<br/>(Presentation)]
        IR[InteractionRuleSO<br/>(Adjacency + Cascade)]
        SOs[Content SOs<br/>GridConfigSO / TileDatabaseSO / RuleSetSO / LevelLayoutSO]
        Gizmos[GridGizmoRenderer]
    end

    subgraph Core[Puzzle Engine Core]
        GM[GridModel]
        RE[RuleEngine]
        GS[GridSimulator]
    end

    Input --> PM
    Views --> PM
    IR --> Input
    SOs --> PM

    PM --> GM
    PM --> RE
    PM --> GS

    GM <--> RE
    GM <--> GS
    RE --> GS

    Gizmos --> PM
    Gizmos --> GM

    PM -- OnGridChanged --> Views
```

- **Unity Layer**: everything MonoBehaviour / scene-bound.  
- **Puzzle Engine Core**: pure C# logic, independent of scenes and GameObjects.  
- **InteractionRuleSO + GridClickController** form a configurable interaction layer between player input and the engine.  
- **GridView / TileView** render the core `GridModel` without owning any logic.

---

## 5. Core Domain Model

### 5.1 GridModel

**Namespace:** `PuzzleEngine.Runtime.Core`

Represents the **logical grid** – single source of truth for tiles.

Key responsibilities:

- Store `TileData` per `(x, y)` cell.
- Bounds checking (`IsInside`).
- Get/set operations (`Get`, `Set`).
- Clear/reset (`Clear`).

Constraints:

- No direct references to Unity types.
- No knowledge of visuals, input, or physics.

---

### 5.2 TileData

**Namespace:** `PuzzleEngine.Runtime.Core`

Represents a **tile instance** at a grid position.

Typical fields (current + planned):

- `TileTypeId` (int) – ID mapping to a `TileTypeSO` entry in `TileDatabaseSO`.
- Optional metadata (future):
  - power level
  - flags (e.g. blocking, combinable, special)
- Static reference to “empty” tile: `TileData.Empty`.

`TileData` is intentionally small and cheap to copy.

---

### 5.3 RuleEngine & RuleSet / MergeRules

**Namespace:** `PuzzleEngine.Runtime.Rules`

**`TileTypeSO`** (ScriptableObject):

- Defines a tile family: ID, debug color, name, and future behaviour flags.

**`TileDatabaseSO`**:

- Registry of all `TileTypeSO` entries used in a puzzle set.

**`RuleSetSO` / `MergeRulesSO`**:

- Author-time definition of all **merge / interaction outcomes**:
  - e.g. Fire + Water → Steam
  - Wood + Wood → Plank
- Stored under `Content/Rules`.

**`RuleEngine`** (runtime class):

- Built at startup from `TileDatabaseSO` + `RuleSetSO` / `MergeRulesSO`.
- Executes **local interaction rules** like:
  - merge A + B → C
  - replace both tiles, one tile, or neither.
- Provides a single API surface, e.g.:

```csharp
bool TryApply(TileData a, TileData b, out TileData newA, out TileData newB);
```

Design goals:

- Rules are **data-driven** (SOs) but compiled into efficient runtime lookups.
- The engine can be unit-tested without Unity.

---

### 5.4 GridSimulator

**Namespace:** `PuzzleEngine.Runtime.Simulation`

Deterministic simulation stepper that:

- Consumes a `GridModel` + `RuleEngine`.
- Encodes “global” puzzle behaviour, e.g.:
  - scanning for applicable pairs
  - resolving cascades / matches
  - potential future: gravity, falling tiles, etc.

API shape:

```csharp
public class GridSimulator
{
    public bool Step(GridModel grid);
}
```

- `Step` mutates the grid in place.
- Returns `true` if any tiles changed (useful for “run until stable”).
- `PuzzleManager.RunUntilStable(maxSteps)` can drive this until the system reaches a fixed point.

---

## 6. Unity Façade – PuzzleManager

**Namespace:** `PuzzleEngine.Runtime.Core`  
**Type:** `PuzzleManager : MonoBehaviour`

Central MonoBehaviour that **owns and wires** all core systems for a given scene.

Serialized fields:

- `GridConfigSO gridConfig` – grid width/height, origin, maybe cell size.
- `TileDatabaseSO tileDatabase` – registry of all tiles.
- `RuleSetSO ruleSet` / `MergeRulesSO` – merge rules to build `RuleEngine` from.
- `LevelLayoutSO defaultLayout` – optional authored layout to load on startup.
- `bool autoLoadDefaultLayout` – if true, applies `defaultLayout` automatically.

Runtime properties:

- `GridModel Grid { get; private set; }`
- `RuleEngine RuleEngine { get; private set; }`
- `GridSimulator Simulator { get; private set; }`

Events (planned/current):

```csharp
public event Action<GridModel> OnGridChanged;
```

Raised whenever the grid is (potentially) modified:

- setting a tile
- applying a rule between two tiles
- running a simulation step
- loading a layout.

Lifecycle:

```csharp
private void Awake()
{
    EnsureGridConfig();
    InitializeGrid();
    InitializeRules();

    if (autoLoadDefaultLayout && defaultLayout != null)
        defaultLayout.ApplyToGrid(Grid);
}
```

Responsibilities:

- Ensure a valid `GridConfigSO` (or create an in-memory default for safety).
- Construct the `GridModel` with the configured size.
- Instantiate `RuleEngine` and `GridSimulator` (if content is provided).
- Optionally load a `LevelLayoutSO` on startup.
- Provide **safe helper methods** for callers:
  - `TrySetTile(int x, int y, TileData tile)`
  - `TryGetTile(int x, int y, out TileData tile)`
  - `TryApplyRuleBetween(int x1, int y1, int x2, int y2)`
  - `StepSimulation()`
  - `RunUntilStable(int maxSteps = 64)`
  - `LoadLayout(LevelLayoutSO layout)`
  - `SaveCurrentLayout(LevelLayoutSO layout)`

Editor-only helpers:

```csharp
#if UNITY_EDITOR
public GridConfigSO GetGridConfigForDebug() => gridConfig;
public TileDatabaseSO GetTileDatabaseForDebug() => tileDatabase;
#endif
```

These are used by gizmos and editor tools without polluting runtime logic.

---

## 7. View Layer – GridView & TileView

**Namespace:** `PuzzleEngine.Runtime.View`

The view layer renders the logical state (`GridModel`) into Unity `GameObject`s and is kept strictly separate from game logic.

### 7.1 TileView

**Type:** `TileView : MonoBehaviour`

Represents a single visible cell in the grid.

Responsibilities:

- Store its logical coordinates `X`, `Y` and current `TileData`.
- Hold references to:
  - a main `SpriteRenderer` for the tile
  - an optional `SpriteRenderer` (or visual) for a selection/highlight overlay.
- APIs:

```csharp
void Initialize(int x, int y, TileData data, Color color);
void UpdateFromModel(TileData data, Color color);
void SetSelected(bool selected);
```

Behaviour:

- When `UpdateFromModel` is called, updates sprite/color based on `TileData`.
- When `SetSelected(true)` is called, shows highlight (e.g., via overlay or scale bump); hides it when `false`.

TileView does **not** query the grid itself; it is driven entirely by `GridView`.

---

### 7.2 GridView

**Type:** `GridView : MonoBehaviour`

Bridges `PuzzleManager.Grid` to `TileView` instances.

Responsibilities:

- Subscribes to `PuzzleManager.OnGridChanged`.
- Instantiates one `TileView` (via prefab) per cell when the grid size changes.
- Positions tiles using a top-left origin:

```csharp
position = worldOrigin + new Vector2((x + 0.5f) * cellSize, -(y + 0.5f) * cellSize);
```

- On grid changes, iterates through all cells and updates `TileView` instances.
- Resolves colors per tile using `TileDatabaseSO` and `TileTypeSO.DebugColor`.
- Manages selection highlight:

```csharp
void SetSelectedCell(Vector2Int? coord);
```

- Optionally exposes a helper for input systems:

```csharp
bool TryWorldToCell(Vector3 worldPos, out Vector2Int coord);
```

Design:

- GridView knows about PuzzleManager + TileDatabaseSO but not about input logic.
- It is a **pure projection** of grid state into the scene.

---

## 8. Interaction Layer – Input & Interaction Rules

### 8.1 InteractionRuleSO

**Namespace:** `PuzzleEngine.Runtime.Core`  
**Type:** `InteractionRuleSO : ScriptableObject`

Defines how the player is allowed to use merge rules for a given level or mode.

Fields:

```csharp
public enum AdjacencyMode
{
    Anywhere,               // like Merge Mansion: any 2 cells on the board
    Orthogonal,             // 4-neighbour only
    OrthogonalAndDiagonal   // 8-neighbour (orthogonal + diagonals)
}

public enum CascadeMode
{
    OnlySelectedPair,           // only the chosen pair is merged
    SelectedPairAndNeighbors,   // chosen pair + its neighbours get a single pass
    GlobalCascade               // full-board simulation (RunUntilStable)
}
```

Examples:

- **Merge Mansion style**: `Anywhere + GlobalCascade`
- **Local match style**: `Orthogonal + OnlySelectedPair`
- **Explosive local style**: `OrthogonalAndDiagonal + SelectedPairAndNeighbors`

InteractionRuleSO assets live under `PuzzleEngine/Content/Rules/` next to merge rule sets.

---

### 8.2 InteractionRuleEvaluator

**Namespace:** `PuzzleEngine.Runtime.Core`  
**Type:** `static InteractionRuleEvaluator`

Pure helper that centralises the logic defined in `InteractionRuleSO` so it can be unit-tested.

Key methods:

```csharp
bool IsSelectionAllowed(Vector2Int a, Vector2Int b, InteractionRuleSO rule);
IEnumerable<Vector2Int> GetNeighborCoords(Vector2Int center, GridModel grid, InteractionRuleSO rule);
```

- `IsSelectionAllowed` enforces `AdjacencyMode` for pair selection.
- `GetNeighborCoords` defines the neighbourhood used for local cascade modes.
- Contains no MonoBehaviours and no direct scene dependencies.

---

### 8.3 GridClickController

**Namespace:** `PuzzleEngine.Runtime.Core`  
**Type:** `GridClickController : MonoBehaviour`

Minimal input driver for the demo layer. Converts mouse clicks into grid interactions.

Serialized fields:

- `PuzzleManager puzzleManager`
- `GridView gridView`
- `InteractionRuleSO interactionRule`
- `Vector2 worldOrigin`
- `float cellSize`
- `Camera targetCamera`

Behaviour:

1. On mouse click:
   - Convert screen position → world → grid cell using `worldOrigin` and `cellSize`.
2. First valid click:
   - Store cell as first selection.
   - Tell `GridView` to highlight it (`SetSelectedCell`).
3. Second valid click:
   - Use `InteractionRuleEvaluator.IsSelectionAllowed(first, second, interactionRule)` to decide if the pair is allowed.
   - If not allowed: treat the second cell as the new first selection.
   - If allowed:
     - Call `PuzzleManager.TryApplyRuleBetween(first, second)`.
     - If merge succeeded, call `ApplyCascade(first, second, changed)` based on `interactionRule.cascadeMode`.
     - Clear selection and highlight.

Cascade modes:

- **OnlySelectedPair** – do nothing after the initial merge.
- **SelectedPairAndNeighbors** – use `InteractionRuleEvaluator.GetNeighborCoords` around both selected cells and call `TryApplyRuleBetween(center, neighbour)` once.
- **GlobalCascade** – call `PuzzleManager.RunUntilStable()` to resolve all possible merges.

---

## 9. Debug & Tooling

### 9.1 GridGizmoRenderer

**Namespace:** `PuzzleEngine.Runtime.Debugs`  
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

### 9.2 PuzzleGridEditorWindow (Editor Grid Tool)

**Namespace:** `PuzzleEngine.Editor.GridEditor`

Custom editor window used to author level layouts (`LevelLayoutSO`):

- Displays a tile palette sourced from `TileDatabaseSO`.
- Shows a mini-grid that matches `GridModel` dimensions.
- Allows painting tile IDs directly into the layout.
- Supports:
  - Selecting a `PuzzleManager` instance in the scene for sizing.
  - Saving the current grid into a `LevelLayoutSO`.
  - Loading an existing `LevelLayoutSO` into the grid.
- Uses `TileTypeSO.DebugColor` to render palette and grid cells consistently with the runtime view.

This tool is **design-time only**; it does not run in builds.

---

## 10. Data Flow

### 10.1 Scene Startup

1. Scene loads `PuzzleEngineDemo`.
2. `PuzzleManager.Awake()` executes:
   - `EnsureGridConfig()` validates or creates a grid config.
   - `InitializeGrid()` creates `GridModel(width, height)`.
   - `InitializeRules()` constructs `RuleEngine` and `GridSimulator`.
   - Optionally loads `defaultLayout` when `autoLoadDefaultLayout` is enabled.
3. `GridView` is initialised and builds its `TileView` instances based on the current `GridModel`.
4. `GridClickController.Awake()` finds references to `PuzzleManager`, `GridView`, and `InteractionRuleSO`.

### 10.2 Player Interaction

1. Player clicks a cell → `GridClickController` resolves it to grid coordinates.
2. First click:
   - Stores the coordinate as `_firstSelection`.
   - Calls `GridView.SetSelectedCell` to highlight the tile.
3. Second click:
   - Uses `InteractionRuleEvaluator.IsSelectionAllowed(first, second, interactionRule)` to validate the pair.
   - If valid:
     - Calls `PuzzleManager.TryApplyRuleBetween`.
     - If changed, applies cascade via `interactionRule.cascadeMode`.
   - Clears selection & highlight.

### 10.3 Simulation Step

- `TryApplyRuleBetween` internally:
  - Fetches `TileData` from `GridModel`.
  - Passes them into `RuleEngine.TryApply`.
  - Writes back new tile data on success.
  - Raises `OnGridChanged`.
- `RunUntilStable` and `StepSimulation` call into `GridSimulator`, which:
  - Runs through the grid, applying rules.
  - Stops when no more changes are possible or max steps reached.
  - Raises `OnGridChanged` each time.

### 10.4 View Refresh

- Whenever `OnGridChanged` fires, `GridView`:
  - Rebuilds its tiles if the grid size changed.
  - Iterates through all cells, reads `GridModel` via `PuzzleManager.Grid`, and updates `TileView`s.

---

## 11. Extension Points & Planned Work

The core is intentionally small but structured to grow. Planned additions:

- **Level / Puzzle Data**
  - ScriptableObject definitions for authored levels.
  - Preset initial grid states (tiles, obstacles, goals).
- **Goal System**
  - Data-driven goals (score threshold, clear X tiles, survive N turns, etc.).
  - Separate domain model and evaluator (`GoalSet`, `GoalEvaluator`).
- **Advanced View Layer**
  - Tile view poolers and adapters that bind `GridModel` state to sprites.
  - Simple animation layer separated from core state (merge FX, invalid-move feedback, etc.).
- **Persistence**
  - Save/load of puzzle state and player progress.
- **More Interaction Modes**
  - Limited-depth cascades, move-only interactions, directional rules.
- **Testing**
  - PlayMode / EditMode tests for rules, simulation, and interaction logic.

---

## 12. Testing

Tests live under `PuzzleEngine/Tests/EditMode/` and are run via Unity Test Runner.

Current coverage (examples):

- **RuleEngineMergeTests**
  - Validate that simple merge rules (e.g. Fire + Water → Steam) behave as expected.
  - Ensure ID mapping via `TileDatabaseSO` is correct.

- **InteractionRuleEvaluatorTests**
  - Verify `AdjacencyMode` behaviours:
    - `Anywhere` allows any two distinct cells.
    - `Orthogonal` allows only 4-neighbours.
    - `OrthogonalAndDiagonal` allows 8-neighbours.
  - Verify neighbour selection used for local cascades:
    - Correct count and positions for orthogonal vs. orthogonal+diagonal modes.

Guidelines for new features:

- Any new merge rule type or interaction mode should ship with at least one focused edit-mode test.
- Core classes (`GridModel`, `RuleEngine`, `GridSimulator`, `InteractionRuleEvaluator`) should remain Unity-free for easier testing.

---

## 13. Dependency Rules

To keep the architecture clean:

- `PuzzleEngine.Runtime.Core`, `PuzzleEngine.Runtime.Rules`, `PuzzleEngine.Runtime.Simulation`  
  **must not depend on**:
  - MonoBehaviours
  - Scenes / GameObjects
  - UI systems

- `PuzzleManager` is the **primary entry point** from Unity into the core.

- View and input layers (`PuzzleEngine.Runtime.View`, `GridClickController`) may depend on `PuzzleManager` and Unity APIs but not vice versa.

- Editor tooling (`PuzzleEngine.Editor.*`, `PuzzleEngine.Runtime.Debugs`)  
  may depend on runtime/core and UnityEditor, but **not the other way around**.

Enforcing these rules keeps the engine:

- Testable in isolation.
- Re-usable across different Unity projects.
- Easy to read for reviewers from AAA / Metacore-style studios.

---

## 14. Checklist for Contributors

Before opening a PR:

- [ ] New core types live under `PuzzleEngine.Runtime.Core / Rules / Simulation` and are Unity-free.
- [ ] Interactions with the grid go through `PuzzleManager` or well-defined APIs.
- [ ] View / input code does not bypass the domain model to mutate state.
- [ ] Debug / gizmo code is in `PuzzleEngine.Runtime.Debugs` or `PuzzleEngine.Editor.*`.
- [ ] Public APIs are named clearly and documented with XML summaries.
- [ ] New systems and decisions are reflected here in `ARCHITECTURE.md`.
- [ ] Relevant edit-mode tests are added or updated.
