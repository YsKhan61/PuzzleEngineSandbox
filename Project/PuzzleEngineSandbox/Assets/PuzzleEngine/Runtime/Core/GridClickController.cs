using UnityEngine;
using PuzzleEngine.Runtime.View;   // for GridView

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Minimal runtime input driver for the puzzle grid.
    /// Lets you click two cells and applies a rule between them
    /// using PuzzleManager.TryApplyRuleBetween, then triggers cascade
    /// according to InteractionRulesSO.
    /// </summary>
    public class GridClickController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private GridView gridView;
        [SerializeField] private InteractionRulesSO interactionRules;

        [Header("Grid Mapping")]
        [Tooltip("World-space origin of cell (0,0). Should match GridGizmoRenderer origin.")]
        [SerializeField] private Vector2 worldOrigin = Vector2.zero;

        [Tooltip("Size of each cell in world units. Should match GridGizmoRenderer cellSize.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Camera used for raycasting. If null, Camera.main is used.")]
        [SerializeField] private Camera targetCamera;

        private Vector2Int? _firstSelection;

        private static readonly Vector2Int[] OrthoDirs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        private static readonly Vector2Int[] DiagDirs =
        {
            new Vector2Int( 1, 1),
            new Vector2Int( 1,-1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1,-1)
        };

        private void Awake()
        {
            if (!puzzleManager)
                puzzleManager = FindObjectOfType<PuzzleManager>();

            if (!targetCamera)
                targetCamera = Camera.main;

            if (!gridView)
                gridView = FindObjectOfType<GridView>();
        }

        private void Update()
        {
            if (!puzzleManager || puzzleManager.Grid == null)
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
            if (!targetCamera)
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
            if (_firstSelection == null)
            {
                // First selection: just store & highlight
                _firstSelection = cell;
                Debug.Log($"[GridClickController] First selection: {cell.x},{cell.y}");
                RefreshHighlight();
                return;
            }

            var first  = _firstSelection.Value;
            var second = cell;

            // Clicking the same cell again: deselect
            if (second == first)
            {
                _firstSelection = null;
                RefreshHighlight();
                return;
            }

            // Check interaction rule: is this pair allowed?
            if (!IsSelectionAllowed(first, second))
            {
                Debug.Log("[GridClickController] Pair not allowed by InteractionConfig. Changing selection.");
                _firstSelection = second;
                RefreshHighlight();
                return;
            }

            Debug.Log($"[GridClickController] Second selection: {second.x},{second.y}. Applying rule...");

            bool changed = puzzleManager.TryApplyRuleBetween(
                first.x, first.y,
                second.x, second.y);

            // Cascade behaviour controlled by InteractionConfig
            ApplyCascade(first, second, changed);

            // Clear selection & highlight after interaction
            _firstSelection = null;
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            if (!gridView)
                return;

            if (_firstSelection.HasValue)
                gridView.SetSelectedCell(_firstSelection.Value);
            else
                gridView.SetSelectedCell(null);
        }

        // ---------------- Interaction rules ----------------

        /// <summary>
        /// Whether the player is allowed to interact between these two cells,
        /// based on AdjacencyMode (Anywhere / Orthogonal / Ortho+Diag).
        /// </summary>
        private bool IsSelectionAllowed(Vector2Int a, Vector2Int b)
        {
            if (a == b)
                return false;

            var mode = interactionRules
                ? interactionRules.adjacencyMode
                : AdjacencyMode.Anywhere; // Merge-mansion style default

            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);

            switch (mode)
            {
                case AdjacencyMode.Anywhere:
                    // Any two cells on the board may interact
                    return true;

                case AdjacencyMode.Orthogonal:
                    // Exactly one step in horizontal OR vertical
                    return dx + dy == 1;

                case AdjacencyMode.OrthogonalAndDiagonal:
                    // Any of the 8 neighbours
                    return Mathf.Max(dx, dy) == 1;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Applies cascade behaviour after a valid pair was merged,
        /// based on CascadeMode (pair-only / neighbours / global).
        /// </summary>
        private void ApplyCascade(Vector2Int first, Vector2Int second, bool changed)
        {
            if (!changed || !puzzleManager)
                return;

            if (!interactionRules)
            {
                // Backwards-compatible default: single global step
                puzzleManager.StepSimulation();
                return;
            }

            switch (interactionRules.cascadeMode)
            {
                case CascadeMode.OnlySelectedPair:
                    // Do nothing extra; only the clicked pair changed.
                    break;

                case CascadeMode.SelectedPairAndNeighbors:
                    ApplyLocalNeighborCascade(first);
                    ApplyLocalNeighborCascade(second);
                    break;

                case CascadeMode.GlobalCascade:
                    puzzleManager.RunUntilStable();
                    break;
            }
        }

        /// <summary>
        /// Applies rules between a center cell and its neighbours once.
        /// Neighbourhood is orthogonal by default; diagonals included if
        /// adjacency mode is OrthogonalAndDiagonal or Anywhere.
        /// </summary>
        private void ApplyLocalNeighborCascade(Vector2Int center)
        {
            var grid = puzzleManager.Grid;
            if (grid == null)
                return;

            bool includeDiagonals =
                interactionRules &&
                (interactionRules.adjacencyMode == AdjacencyMode.OrthogonalAndDiagonal ||
                 interactionRules.adjacencyMode == AdjacencyMode.Anywhere);

            // Orthogonal neighbours
            foreach (var dir in OrthoDirs)
            {
                var n = center + dir;
                if (grid.IsInside(n.x, n.y))
                {
                    puzzleManager.TryApplyRuleBetween(center.x, center.y, n.x, n.y);
                }
            }

            // Diagonal neighbours (optional)
            if (includeDiagonals)
            {
                foreach (var dir in DiagDirs)
                {
                    var n = center + dir;
                    if (grid.IsInside(n.x, n.y))
                    {
                        puzzleManager.TryApplyRuleBetween(center.x, center.y, n.x, n.y);
                    }
                }
            }
        }
    }
}
