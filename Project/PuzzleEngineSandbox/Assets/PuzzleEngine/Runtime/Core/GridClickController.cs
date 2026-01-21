using UnityEngine;
using PuzzleEngine.Runtime.View;
using UnityEngine.Serialization;
using PuzzleEngine.Runtime.Simulation;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Minimal runtime input driver for the puzzle grid.
    /// Lets you click two cells and applies a rule between them
    /// using PuzzleManager.TryApplyRuleBetween, then triggers cascade
    /// according to InteractionRuleSO and session rules.
    /// </summary>
    public class GridClickController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private GridView gridView;
        [FormerlySerializedAs("interactionRule")]
        [SerializeField] private InteractionRuleSO interactionRule;

        [Header("Grid Mapping")]
        [Tooltip("World-space origin of cell (0,0). Should match GridGizmoRenderer origin.")]
        [SerializeField] private Vector2 worldOrigin = Vector2.zero;

        [Tooltip("Size of each cell in world units. Should match GridGizmoRenderer cellSize.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Camera used for raycasting. If null, Camera.main is used.")]
        [SerializeField] private Camera targetCamera;

        private Vector2Int? _firstSelection;

        private void Awake()
        {
            if (puzzleManager == null)
                puzzleManager = FindObjectOfType<PuzzleManager>();

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (gridView == null)
                gridView = FindObjectOfType<GridView>();
        }

        private void Update()
        {
            if (puzzleManager == null || puzzleManager.Grid == null)
                return;

            // Stop accepting input once level is finished
            if (puzzleManager.LevelCompleted || puzzleManager.LevelFailed)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                var cell = ScreenToGridCell(Input.mousePosition);
                if (cell.HasValue)
                {
                    OnCellClicked(cell.Value);
                }
            }
        }

        private Vector2Int? ScreenToGridCell(Vector3 screenPos)
        {
            if (targetCamera == null)
                return null;

            var world = targetCamera.ScreenToWorldPoint(screenPos);

            float localX = world.x - worldOrigin.x;
            float localY = world.y - worldOrigin.y;

            if (cellSize <= 0.0001f)
                return null;

            int x = Mathf.FloorToInt(localX / cellSize);
            int y = Mathf.FloorToInt(-localY / cellSize); // top-left origin, y down

            var grid = puzzleManager.Grid;
            if (grid == null || !grid.IsInside(x, y))
                return null;

            return new Vector2Int(x, y);
        }

        private void OnCellClicked(Vector2Int cell)
        {
            // First click: just select
            if (_firstSelection == null)
            {
                _firstSelection = cell;
                Debug.Log($"[GridClickController] First selection: {cell.x},{cell.y}");
                RefreshHighlight();
                return;
            }

            var first  = _firstSelection.Value;
            var second = cell;

            // Clicking the same cell again → deselect
            if (second == first)
            {
                _firstSelection = null;
                RefreshHighlight();
                return;
            }

            // Interaction rule: are these two allowed to interact?
            if (!InteractionRuleEvaluator.IsSelectionAllowed(first, second, interactionRule))
            {
                Debug.Log("[GridClickController] Pair not allowed by InteractionRule. Showing invalid feedback.");

                if (gridView != null)
                    gridView.ShowInvalidSelection(second);

                RefreshHighlight();
                return;
            }

            var grid = puzzleManager.Grid;
            if (grid == null)
            {
                _firstSelection = null;
                RefreshHighlight();
                return;
            }

            // Capture original tile types BEFORE applying the rule
            var tileA = grid.Get(first.x, first.y);
            var tileB = grid.Get(second.x, second.y);
            int originalTypeA = tileA.TileTypeId;
            int originalTypeB = tileB.TileTypeId;

            Debug.Log($"[GridClickController] Second selection: {second.x},{second.y}. Applying rule...");

            bool changed = puzzleManager.TryApplyRuleBetween(
                first.x, first.y,
                second.x, second.y);

            // Cascade using original types (for global-matching mode, etc.)
            ApplyCascade(first, second, changed, originalTypeA, originalTypeB);

            // Register a move only if the grid actually changed
            if (changed)
            {
                puzzleManager.RegisterPlayerMove();
            }

            _firstSelection = null;
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            if (gridView == null)
                return;

            if (_firstSelection.HasValue)
                gridView.SetSelectedCell(_firstSelection.Value);
            else
                gridView.SetSelectedCell(null);
        }

        // -------------- Cascade behaviour --------------

        private void ApplyCascade(Vector2Int first, Vector2Int second, bool changed, int originalTypeA, int originalTypeB)
        {
            if (!changed || puzzleManager == null)
                return;

            if (interactionRule == null)
            {
                // Backwards-compatible default: single simulation step
                puzzleManager.StepSimulation();
                return;
            }

            switch (interactionRule.cascadeMode)
            {
                case CascadeMode.OnlySelectedPair:
                    // no extra work
                    break;

                case CascadeMode.SelectedPairAndNeighbors:
                    ApplyLocalNeighborCascade(first);
                    ApplyLocalNeighborCascade(second);
                    break;

                case CascadeMode.GlobalCascade:
                    puzzleManager.RunUntilStable();
                    break;

                case CascadeMode.GlobalMatchingPairs:
                    if (puzzleManager.Grid != null && puzzleManager.RuleEngine != null)
                    {
                        CascadeUtility.ApplyGlobalMatchingPairs(
                            puzzleManager.Grid,
                            puzzleManager.RuleEngine,
                            originalTypeA,
                            originalTypeB);
                    }
                    break;
            }
        }

        private void ApplyLocalNeighborCascade(Vector2Int center)
        {
            var grid = puzzleManager.Grid;
            if (grid == null)
                return;

            foreach (var n in InteractionRuleEvaluator.GetNeighborCoords(center, grid, interactionRule))
            {
                puzzleManager.TryApplyRuleBetween(center.x, center.y, n.x, n.y);
            }
        }
    }
}
