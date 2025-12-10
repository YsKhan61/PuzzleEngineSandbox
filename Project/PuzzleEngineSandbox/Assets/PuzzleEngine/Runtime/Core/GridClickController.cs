using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Minimal runtime input driver for the puzzle grid.
    /// Lets you click two cells and applies a rule between them
    /// using PuzzleManager.TryApplyRuleBetween, then steps simulation once.
    /// This is just a demo controller – not final gameplay.
    /// </summary>
    public class GridClickController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuzzleManager puzzleManager;

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
        }

        private void Update()
        {
            if (puzzleManager == null || puzzleManager.Grid == null)
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

            // Convert screen → world
            var world = targetCamera.ScreenToWorldPoint(screenPos);

            // Convert world → local grid space
            float localX = world.x - worldOrigin.x;
            float localY = world.y - worldOrigin.y;

            if (cellSize <= 0.0001f)
                return null;

            int x = Mathf.FloorToInt(localX / cellSize);
            int y = Mathf.FloorToInt(-localY / cellSize); // note: depends on your grid Y direction

            var grid = puzzleManager.Grid;
            if (grid == null || !grid.IsInside(x, y))
                return null;

            return new Vector2Int(x, y);
        }

        private void OnCellClicked(Vector2Int cell)
        {
            if (_firstSelection == null)
            {
                _firstSelection = cell;
                Debug.Log($"[GridClickController] First selection: {cell.x},{cell.y}");
            }
            else
            {
                var first = _firstSelection.Value;
                var second = cell;

                Debug.Log($"[GridClickController] Second selection: {second.x},{second.y}. Applying rule...");

                bool changed = puzzleManager.TryApplyRuleBetween(
                    first.x, first.y,
                    second.x, second.y);

                if (changed)
                {
                    // Optional: step simulation once for chain reactions
                    puzzleManager.StepSimulation();
                    Debug.Log("[GridClickController] Rule applied and simulation stepped.");
                }
                else
                {
                    Debug.Log("[GridClickController] No rule applied for this pair.");
                }

                _firstSelection = null;
            }
        }
    }
}