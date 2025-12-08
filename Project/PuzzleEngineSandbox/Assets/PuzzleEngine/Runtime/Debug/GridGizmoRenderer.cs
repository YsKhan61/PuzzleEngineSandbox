using UnityEngine;
using PuzzleEngine.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PuzzleEngine.Debugs
{
    [DisallowMultipleComponent]
    public class GridGizmoRenderer : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawTiles = true;   // for later tile-preview
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 origin = Vector2.zero;
        [SerializeField] private bool drawLabels = false;
        [SerializeField] private Color emptyColor = Color.white;

        private PuzzleManager puzzleManager;

        private void Reset()
        {
            puzzleManager = GetComponent<PuzzleManager>();
        }

        private void OnValidate()
        {
            if (puzzleManager == null)
                puzzleManager = GetComponent<PuzzleManager>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGrid && !drawTiles)
                return;

            if (puzzleManager == null)
                puzzleManager = GetComponent<PuzzleManager>();

            if (puzzleManager == null)
                return;

            // -----------------------------------------------------------------
            // 1. Figure out grid width/height
            // -----------------------------------------------------------------
            int width  = 0;
            int height = 0;

            // Prefer runtime Grid (Play mode)
            if (puzzleManager.Grid != null)
            {
                width  = puzzleManager.Grid.Width;
                height = puzzleManager.Grid.Height;
            }
            else
            {
                // Edit mode: fallback to GridConfigSO values
                var cfg = puzzleManager.GetGridConfigForDebug();
                if (cfg != null)
                {
                    width  = cfg.width;
                    height = cfg.height;
                }
            }

            if (width <= 0 || height <= 0)
                return;

            // -----------------------------------------------------------------
            // 2. Draw the grid
            // -----------------------------------------------------------------
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color  = emptyColor;

            Vector3 cellSizeVec = new Vector3(cellSize, cellSize, 0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Center of each cell in local space
                    Vector3 center = new Vector3(
                        origin.x + (x + 0.5f) * cellSize,
                        origin.y - (y + 0.5f) * cellSize,
                        0f
                    );

                    Gizmos.DrawWireCube(center, cellSizeVec);

                    if (drawLabels)
                    {
                        Handles.Label(center, $"{x},{y}");
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
