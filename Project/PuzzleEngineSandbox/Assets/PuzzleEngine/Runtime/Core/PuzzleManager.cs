using System;
using PuzzleEngine.Runtime.Rules;
using PuzzleEngine.Runtime.Simulation;
using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// MonoBehaviour façade over the core puzzle systems.
    /// Owns the GridModel and wires in the RuleEngine and GridSimulator.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PuzzleManager : MonoBehaviour
    {
        [Header("Grid Config")]
        [SerializeField] private GridConfigSO gridConfig;

        [Header("Content")]
        [Tooltip("Registry of all tile types used in this puzzle set.")]
        [SerializeField] private TileDatabaseSO tileDatabase;

        [Tooltip("Set of interaction rules (merge/combine) used by this puzzle.")]
        [SerializeField] private RuleSetSO ruleSet;
        
        [Header("Level")]
        [Tooltip("Optional default layout to load at startup (and in Edit Mode when enabled).")]
        [SerializeField] private LevelLayoutSO defaultLayout;

        [Tooltip("If true, will auto-load the default layout on startup/initialization.")]
        [SerializeField] private bool autoLoadDefaultLayout = true;

        /// <summary>Runtime grid model – single source of truth for tiles.</summary>
        public GridModel Grid { get; private set; }

        /// <summary>Runtime rule engine (built from TileDatabase + RuleSet).</summary>
        public RuleEngine RuleEngine { get; private set; }

        /// <summary>Deterministic simulation stepper for the grid.</summary>
        public GridSimulator Simulator { get; private set; }

        /// <summary>
        /// Raised whenever the grid content changes (tiles set, rules applied, layout loaded, etc.).
        /// </summary>
        public event Action<GridModel> OnGridChanged;

        private void Awake()
        {
            // Runtime entry point
            EnsureInitialized();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep things consistent in Edit Mode when config or references change
            if (Application.isPlaying) return;
            Grid = null;
            RuleEngine = null;
            Simulator = null;
            EnsureInitialized();
        }
#endif

        // --------------------------------------------------------------------
        // Initialization
        // --------------------------------------------------------------------

        /// <summary>
        /// Shared initialization entry point for both runtime and editor tools.
        /// Safe to call multiple times; will only rebuild when needed.
        /// </summary>
        public void EnsureInitialized(bool applyDefaultLayout = true)
        {
            EnsureGridConfig();

            if (!gridConfig)
            {
                Debug.LogWarning("[PuzzleManager] GridConfigSO is missing; grid will not be initialized.", this);
                return;
            }

            bool gridRebuilt = false;

            if (Grid == null ||
                Grid.Width != gridConfig.width ||
                Grid.Height != gridConfig.height)
            {
                InitializeGrid();
                gridRebuilt = true;
            }

            if ((RuleEngine == null || Simulator == null) &&
                tileDatabase &&
                ruleSet)
            {
                InitializeRules();
            }

            // Optionally load default layout
            if (applyDefaultLayout &&
                autoLoadDefaultLayout &&
                defaultLayout &&
                Grid != null)
            {
                defaultLayout.ApplyToGrid(Grid);
                gridRebuilt = true;
            }

            // Notify listeners if we have a valid grid (and especially if we rebuilt it)
            if (Grid != null && gridRebuilt)
            {
                RaiseGridChanged();
            }
        }


        /// <summary>
        /// Ensures we have some GridConfigSO assigned; creates an in-memory default otherwise.
        /// </summary>
        private void EnsureGridConfig()
        {
            if (gridConfig)
                return;

            Debug.LogError(
                "[PuzzleManager] GridConfigSO is not assigned. Creating a default in-memory asset (6x6).",
                this);

            gridConfig = ScriptableObject.CreateInstance<GridConfigSO>();
            gridConfig.width = 6;
            gridConfig.height = 6;
        }

        private void InitializeGrid()
        {
            Grid = new GridModel(gridConfig.width, gridConfig.height);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Avoid spamming logs in play mode, but useful during tooling work
                Debug.Log($"[PuzzleManager] Initialized grid {gridConfig.width}x{gridConfig.height} (Edit Mode).", this);
            }
#endif
        }

        private void InitializeRules()
        {
            if (!tileDatabase || !ruleSet)
            {
                Debug.LogWarning(
                    "[PuzzleManager] TileDatabase or RuleSet not assigned. RuleEngine and Simulator will not be available.",
                    this);
                RuleEngine = null;
                Simulator = null;
                return;
            }

            RuleEngine = new RuleEngine(tileDatabase, ruleSet);

            // Assuming GridSimulator takes RuleEngine and steps any Grid passed in
            Simulator = new GridSimulator(RuleEngine);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.Log("[PuzzleManager] RuleEngine and GridSimulator initialized (Edit Mode).", this);
            }
#endif
        }

        // --------------------------------------------------------------------
        // Tile Helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Safely sets a tile if inside bounds. Returns true on success.
        /// </summary>
        public bool TrySetTile(int x, int y, TileData tile)
        {
            if (Grid == null || !Grid.IsInside(x, y))
                return false;

            Grid.Set(x, y, tile);
            RaiseGridChanged();
            return true;
        }

        /// <summary>
        /// Safely gets a tile if inside bounds. Returns false if out of bounds.
        /// </summary>
        public bool TryGetTile(int x, int y, out TileData tile)
        {
            if (Grid == null || !Grid.IsInside(x, y))
            {
                tile = TileData.Empty;
                return false;
            }

            tile = Grid.Get(x, y);
            return true;
        }

        // --------------------------------------------------------------------
        // Rule Application
        // --------------------------------------------------------------------

        /// <summary>
        /// Attempts to apply a rule between the two coordinates (x1,y1) and (x2,y2).
        /// Returns true if a rule existed and changed the grid.
        /// </summary>
        public bool TryApplyRuleBetween(int x1, int y1, int x2, int y2)
        {
            if (RuleEngine == null || Grid == null)
            {
                Debug.LogWarning("[PuzzleManager] RuleEngine is null. Cannot apply rules.", this);
                return false;
            }

            if (!Grid.IsInside(x1, y1) || !Grid.IsInside(x2, y2))
                return false;

            var a = Grid.Get(x1, y1);
            var b = Grid.Get(x2, y2);

            if (!RuleEngine.TryApply(a, b, out var newA, out var newB))
                return false;

            Grid.Set(x1, y1, newA);
            Grid.Set(x2, y2, newB);

            RaiseGridChanged();
            return true;
        }

        // --------------------------------------------------------------------
        // Simulation
        // --------------------------------------------------------------------

        /// <summary>
        /// Performs a single deterministic simulation step.
        /// Returns true if any tiles changed.
        /// </summary>
        public bool StepSimulation()
        {
            if (Simulator == null || Grid == null)
                return false;

            bool changed = Simulator.Step(Grid);
            if (changed)
            {
                RaiseGridChanged();
            }

            return changed;
        }

        /// <summary>
        /// Runs simulation until no more changes occur, or maxSteps reached.
        /// Returns the number of steps executed.
        /// </summary>
        public int RunUntilStable(int maxSteps = 64)
        {
            if (Simulator == null || Grid == null)
                return 0;

            int steps = 0;
            while (steps < maxSteps && Simulator.Step(Grid))
            {
                steps++;
            }

            if (steps > 0)
            {
                RaiseGridChanged();
            }

            return steps;
        }
        
        #region Layout

        /// <summary>
        /// Captures the current grid state into the given LevelLayout asset.
        /// </summary>
        public void SaveCurrentLayout(LevelLayoutSO layout)
        {
            if (layout == null)
            {
                Debug.LogWarning("[PuzzleManager] SaveCurrentLayout called with null layout.", this);
                return;
            }

            if (Grid == null)
            {
                Debug.LogWarning("[PuzzleManager] SaveCurrentLayout called but Grid is null.", this);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(layout, "Save Level Layout");
#endif
            layout.CaptureFromGrid(Grid);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(layout);
#endif
        }

        /// <summary>
        /// Loads a layout into the current grid, rebuilding if needed.
        /// </summary>
        public void LoadLayout(LevelLayoutSO layout)
        {
            if (layout == null)
            {
                Debug.LogWarning("[PuzzleManager] LoadLayout called with null layout.", this);
                return;
            }

            if (Grid == null)
            {
                Debug.LogWarning("[PuzzleManager] LoadLayout called but Grid is null after initialization.", this);
                return;
            }

            layout.ApplyToGrid(Grid);
            RaiseGridChanged();
        }

        #endregion

#if UNITY_EDITOR
        // --------------------------------------------------------------------
        // Debug / Editor helpers
        // --------------------------------------------------------------------

        /// <summary>Debug-only accessor used by editor gizmos.</summary>
        public GridConfigSO GetGridConfigForDebug() => gridConfig;

        public TileDatabaseSO GetTileDatabaseForDebug() => tileDatabase;
#endif

        // --------------------------------------------------------------------
        // Internal helpers
        // --------------------------------------------------------------------

        private void RaiseGridChanged()
        {
            if (Grid == null)
                return;

            OnGridChanged?.Invoke(Grid);
        }
    }
}
