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

        private readonly Dictionary<Vector2Int, TileView> _tiles = new();

        private Vector2Int? _selectedCoord;
        private TileView _selectedView;
        
        private void Awake()
        {
            if (!gridRoot)
                gridRoot = transform;

            if (!puzzleManager)
                puzzleManager = FindObjectOfType<PuzzleManager>();
            
#if UNITY_EDITOR
            if (!tileDatabase && puzzleManager)
                tileDatabase = puzzleManager.GetTileDatabaseForDebug();
#endif
        }

        private void OnEnable()
        {
            if (puzzleManager)
            {
                puzzleManager.EnsureInitialized();
                puzzleManager.OnGridChanged += HandleGridChanged;

                if (puzzleManager.Grid != null)
                    HandleGridChanged(puzzleManager.Grid);
            }
        }

        private void OnDisable()
        {
            if (puzzleManager)
                puzzleManager.OnGridChanged -= HandleGridChanged;
        }

        private void HandleGridChanged(GridModel model)
        {
            if (model == null || !tilePrefab)
                return;

            RebuildIfSizeChanged(model);
            RefreshAllTiles(model);
            RefreshSelectionHighlight();
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
        
        public void SetSelectedCell(Vector2Int? coord)
        {
            // Clear previous
            if (_selectedView)
                _selectedView.SetSelected(false);

            _selectedView = null;
            _selectedCoord = coord;

            if (!_selectedCoord.HasValue)
                return;

            if (_tiles.TryGetValue(_selectedCoord.Value, out var view) && view)
            {
                _selectedView = view;
                _selectedView.SetSelected(true);
            }
        }

        /// <summary>
        /// Triggers a short invalid-selection flash on the given cell,
        /// if a TileView exists for that coordinate.
        /// Intended to be used when a second click is not allowed
        /// by the current InteractionRule.
        /// </summary>
        public void ShowInvalidSelection(Vector2Int coord)
        {
            if (_tiles.TryGetValue(coord, out var view) && view != null)
            {
                view.ShowInvalidSelection();
            }
        }
        
        public bool TryWorldToCell(Vector3 worldPos, out Vector2Int coord)
        {
            coord = default;

            if (puzzleManager == null || puzzleManager.Grid == null)
                return false;

            float localX = worldPos.x - worldOrigin.x;
            float localY = worldPos.y - worldOrigin.y;

            int x = Mathf.FloorToInt(localX / cellSize);
            int y = Mathf.FloorToInt(-localY / cellSize); // top-left origin, y down

            if (!puzzleManager.Grid.IsInside(x, y))
                return false;

            coord = new Vector2Int(x, y);
            return true;
        }

        private void RefreshSelectionHighlight()
        {
            if (!_selectedCoord.HasValue)
                return;

            if (_tiles.TryGetValue(_selectedCoord.Value, out var view))
                _selectedView = view;

            if (_selectedView)
                _selectedView.SetSelected(true);
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
