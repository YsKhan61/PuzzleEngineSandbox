using System.Collections.Generic;
using PuzzleEngine.Runtime.Core;
using UnityEngine;

namespace PuzzleEngine.Runtime.View
{
    /// <summary>
    /// Bridges PuzzleManager's GridModel to visual TileView instances.
    /// Uses a top-left origin (worldOrigin) similar to GridViewRenderer.
    /// </summary>
    [DisallowMultipleComponent]
    public class GridView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private Transform gridRoot;

        [Header("Layout")]
        [Tooltip("World-space origin for cell (0,0), top-left of the grid.")]
        [SerializeField] private Vector2 worldOrigin = Vector2.zero;

        [Tooltip("Size of each cell in world units.")]
        [SerializeField] private float cellSize = 1f;

        private readonly Dictionary<Vector2Int, TileView> _tiles =
            new Dictionary<Vector2Int, TileView>();

        private void Awake()
        {
            if (gridRoot == null)
                gridRoot = transform;

            if (puzzleManager == null)
            {
                puzzleManager = FindObjectOfType<PuzzleManager>();
            }
        }

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.EnsureInitialized();
                puzzleManager.OnGridChanged += HandleGridChanged;

                if (puzzleManager.Grid != null)
                {
                    HandleGridChanged(puzzleManager.Grid);
                }
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnGridChanged -= HandleGridChanged;
            }
        }

        private void HandleGridChanged(GridModel model)
        {
            if (model == null || tilePrefab == null)
                return;

            RebuildIfSizeChanged(model);
            RefreshAllTiles(model);
        }

        private void RebuildIfSizeChanged(GridModel model)
        {
            int expectedCount = model.Width * model.Height;
            if (_tiles.Count == expectedCount)
                return;

            // Clear and rebuild completely if size changed.
            foreach (var kvp in _tiles)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying)
                        Destroy(kvp.Value.gameObject);
                    else
                        DestroyImmediate(kvp.Value.gameObject);
                }
            }

            _tiles.Clear();

            for (int y = 0; y < model.Height; y++)
            {
                for (int x = 0; x < model.Width; x++)
                {
                    Vector2Int key = new Vector2Int(x, y);

                    var tileView = Instantiate(tilePrefab, gridRoot);
                    tileView.name = $"TileView_{x}_{y}";

                    // Match GridViewRenderer: origin at top-left, y goes downward
                    Vector3 worldPos = new Vector3(
                        worldOrigin.x + (x + 0.5f) * cellSize,
                        worldOrigin.y - (y + 0.5f) * cellSize,
                        gridRoot.position.z);

                    tileView.transform.position = worldPos;

                    var data = model.Get(x, y);
                    tileView.Initialize(x, y, data);

                    _tiles[key] = tileView;
                }
            }
        }

        private void RefreshAllTiles(GridModel model)
        {
            foreach (var kvp in _tiles)
            {
                Vector2Int coord = kvp.Key;
                TileView view = kvp.Value;

                if (view == null)
                    continue;

                if (!model.IsInside(coord.x, coord.y))
                    continue;

                TileData data = model.Get(coord.x, coord.y);
                view.UpdateFromModel(data);
            }
        }
    }
}
