using PuzzleEngine.Rules;
using UnityEngine;
using PuzzleEngine.Simulation;

namespace PuzzleEngine.Core
{
    /// <summary>
    /// MonoBehaviour façade over the core puzzle systems.
    /// Owns the GridModel and wires in the RuleEngine and GridSimulator.
    /// </summary>
    [DisallowMultipleComponent]
    public class PuzzleManager : MonoBehaviour
    {
        [Header("Grid Config")] [SerializeField]
        private GridConfigSO gridConfig;

        [Header("Content")] [Tooltip("Registry of all tile types used in this puzzle set.")] [SerializeField]
        private TileDatabaseSO tileDatabase;

        [Tooltip("Set of interaction rules (merge/combine) used by this puzzle.")] [SerializeField]
        private RuleSetSO ruleSet;

        /// <summary>Runtime grid model – single source of truth for tiles.</summary>
        public GridModel Grid { get; private set; }

        /// <summary>Runtime rule engine (built from TileDatabase + RuleSet).</summary>
        public RuleEngine RuleEngine { get; private set; }

        /// <summary>Deterministic simulation stepper for the grid.</summary>
        public GridSimulator Simulator { get; private set; }

        private void Awake()
        {
            EnsureGridConfig();
            InitializeGrid();
            InitializeRules();
        }

        #region Initialization

        private void EnsureGridConfig()
        {
            if (gridConfig != null)
                return;

            Debug.LogError("[PuzzleManager] GridConfigSO is not assigned. Creating a default one in-memory.", this);
            gridConfig = ScriptableObject.CreateInstance<GridConfigSO>();
            gridConfig.width = 6;
            gridConfig.height = 6;
        }

        private void InitializeGrid()
        {
            Grid = new GridModel(gridConfig.width, gridConfig.height);
            Debug.Log($"[PuzzleManager] Initialized grid {gridConfig.width}x{gridConfig.height}", this);
        }

        private void InitializeRules()
        {
            if (tileDatabase == null || ruleSet == null)
            {
                Debug.LogWarning("[PuzzleManager] TileDatabase or RuleSet not assigned. " +
                                 "RuleEngine and Simulator will not be available.", this);
                return;
            }

            RuleEngine = new RuleEngine(tileDatabase, ruleSet);
            Simulator = new GridSimulator(RuleEngine);

            Debug.Log("[PuzzleManager] RuleEngine and GridSimulator initialized.", this);
        }

        #endregion

        #region Tile Helpers

        /// <summary>
        /// Safely sets a tile if inside bounds. Returns true on success.
        /// </summary>
        public bool TrySetTile(int x, int y, TileData tile)
        {
            if (Grid == null || !Grid.IsInside(x, y))
                return false;

            Grid.Set(x, y, tile);
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

        #endregion

        #region Rule Application

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
            return true;
        }

        #endregion

        #region Simulation

        /// <summary>
        /// Performs a single deterministic simulation step.
        /// Returns true if any tiles changed.
        /// </summary>
        public bool StepSimulation()
        {
            if (Simulator == null || Grid == null)
                return false;

            return Simulator.Step(Grid);
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

            return steps;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Debug-only accessors used by editor gizmos.
        /// </summary>
        public GridConfigSO GetGridConfigForDebug() => gridConfig;

        public TileDatabaseSO GetTileDatabaseForDebug() => tileDatabase;
#endif
    }
}