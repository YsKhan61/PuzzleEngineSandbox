using PuzzleEngine.Runtime.Core;
using PuzzleEngine.Runtime.Rules;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PuzzleEngine.Runtime.Debugs
{
    [DisallowMultipleComponent]
    public class GridGizmoRenderer : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private bool drawTiles = true;   // draw tile contents as colored fills
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 origin = Vector2.zero;
        [SerializeField] private bool drawLabels = false;

        [Tooltip("Color used for the grid wireframe.")]
        [SerializeField] private Color gridLineColor = Color.red;

        [Tooltip("Fallback color when a tile type has no specific debug color.")]
        [SerializeField] private Color tileFallbackColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);

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
            // 1. Determine grid width/height
            // -----------------------------------------------------------------
            int width = 0;
            int height = 0;

            var grid = puzzleManager.Grid;

            if (grid != null)
            {
                width = grid.Width;
                height = grid.Height;
            }
            else
            {
                // Edit mode: fallback to GridConfigSO values
                var cfg = puzzleManager.GetGridConfigForDebug();
                if (cfg != null)
                {
                    width = cfg.width;
                    height = cfg.height;
                }
            }

            if (width <= 0 || height <= 0)
                return;

            // Get tile database for color lookup (editor-only helper on PuzzleManager)
            TileDatabaseSO tileDatabase = puzzleManager.GetTileDatabaseForDebug();

            Gizmos.matrix = transform.localToWorldMatrix;

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

                    // ---------------------------------------------------------
                    // 2. Draw tile fill (if any)
                    // ---------------------------------------------------------
                    if (drawTiles && grid != null)
                    {
                        var tile = grid.Get(x, y);

                        if (!tile.IsEmpty)
                        {
                            Color fillColor = GetColorForTile(tile.TileTypeId, tileDatabase);

                            // Slightly smaller cube so the grid line is still visible
                            Vector3 fillSize = cellSizeVec * 0.95f;

                            Gizmos.color = fillColor;
                            Gizmos.DrawCube(center, fillSize);
                        }
                    }

                    // ---------------------------------------------------------
                    // 3. Draw grid wireframe on top
                    // ---------------------------------------------------------
                    if (drawGrid)
                    {
                        Gizmos.color = gridLineColor;
                        Gizmos.DrawWireCube(center, cellSizeVec);
                    }

                    // ---------------------------------------------------------
                    // 4. Optional coordinate labels
                    // ---------------------------------------------------------
                    if (drawLabels)
                    {
                        Handles.Label(center, $"{x},{y}");
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        private Color GetColorForTile(int tileTypeId, TileDatabaseSO database)
        {
            if (database != null && database.TileTypes != null)
            {
                foreach (var type in database.TileTypes)
                {
                    if (type != null && type.Id == tileTypeId)
                    {
                        // Assume TileTypeSO has a DebugColor or similar; adjust property name if needed
                        return type.DebugColor;
                    }
                }
            }

            return tileFallbackColor;
        }
    #endif
    }
}
