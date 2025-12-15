using System.Collections.Generic;
using PuzzleEngine.Runtime.Core;
using PuzzleEngine.Runtime.Rules;
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
        [SerializeField] private TileDatabaseSO tileDatabase; 

        [Header("Layout")]
        [Tooltip("World-space origin for cell (0,0), top-left of the grid.")]
        [SerializeField] private Vector2 worldOrigin = Vector2.zero;

        [Tooltip("Size of each cell in world units.")]
        [SerializeField] private float cellSize = 1f;

        private readonly Dictionary<Vector2Int, TileView> _tiles = new ();

        private void Awake()
        {
            if (!gridRoot)
                gridRoot = transform;

            if (!puzzleManager)
            {
                puzzleManager = FindObjectOfType<PuzzleManager>();
            }
        }

        private void OnEnable()
        {
            if (puzzleManager)
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
            if (puzzleManager)
            {
                puzzleManager.OnGridChanged -= HandleGridChanged;
            }
        }

        private void HandleGridChanged(GridModel model)
        {
            if (model == null || !tilePrefab)
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
                if (kvp.Value)
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
                    Color color = ResolveColor(data);
                    tileView.Initialize(x, y, data, color);

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

                if (!view)
                    continue;

                if (!model.IsInside(coord.x, coord.y))
                    continue;

                TileData data = model.Get(coord.x, coord.y);
                Color color = ResolveColor(data);
                view.UpdateFromModel(data, color);
            }
        }
        
        private Color ResolveColor(TileData data)
        {
            if (data.IsEmpty)
                return Color.clear;

            if (!tileDatabase || tileDatabase.TileTypes == null)
                return Color.white;

            int id = data.TileTypeId;
            foreach (var type in tileDatabase.TileTypes)
            {
                if (type && type.Id == id)
                    return type.DebugColor;
            }

            return Color.white;
        }
    }
}
