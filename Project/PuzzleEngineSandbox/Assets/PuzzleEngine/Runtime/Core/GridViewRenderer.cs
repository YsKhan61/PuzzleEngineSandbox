using System.Collections.Generic;
using PuzzleEngine.Runtime.Rules;
using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Simple runtime visualizer that instantiates SpriteRenderers for each grid cell
    /// and tints them based on the TileData in PuzzleManager.Grid.
    /// This is a minimal "view" layer for the demo scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class GridViewRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private TileDatabaseSO tileDatabase;

        [Header("View Settings")]
        [Tooltip("Prefab with a SpriteRenderer used for each cell.")]
        [SerializeField] private GameObject tileViewPrefab;

        [Tooltip("World-space origin of cell (0,0). Should match gizmo origin.")]
        [SerializeField] private Vector2 worldOrigin = Vector2.zero;

        [Tooltip("Size of each cell in world units. Should match grid cell size.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("If true, tiles will be created on Start based on current grid size.")]
        [SerializeField] private bool autoBuildOnStart = true;

        private readonly List<SpriteRenderer> _tileViews = new();

        private void Awake()
        {
            if (puzzleManager == null)
                puzzleManager = FindObjectOfType<PuzzleManager>();

            if (tileDatabase == null && puzzleManager != null)
                tileDatabase = puzzleManager.GetTileDatabaseForDebug();
        }

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildTileViews();
                SyncFromGrid();
            }
        }

        /// <summary>
        /// Creates one tile view per cell based on the current grid size.
        /// Destroys any previous views.
        /// </summary>
        public void BuildTileViews()
        {
            ClearExistingViews();

            if (puzzleManager == null || puzzleManager.Grid == null)
                return;

            var grid = puzzleManager.Grid;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var instance = Instantiate(tileViewPrefab, transform);
                    var sr = instance.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        Debug.LogError("[GridViewRenderer] tileViewPrefab needs a SpriteRenderer.", this);
                        Destroy(instance);
                        continue;
                    }

                    instance.transform.position = new Vector3(
                        worldOrigin.x + (x + 0.5f) * cellSize,
                        worldOrigin.y - (y + 0.5f) * cellSize,
                        0f);

                    _tileViews.Add(sr);
                }
            }
        }

        /// <summary>
        /// Updates tile colors to match the current grid contents.
        /// </summary>
        public void SyncFromGrid()
        {
            if (puzzleManager == null || puzzleManager.Grid == null)
                return;

            var grid = puzzleManager.Grid;

            if (_tileViews.Count != grid.Width * grid.Height)
            {
                Debug.LogWarning("[GridViewRenderer] Tile view count does not match grid size. Rebuilding.");
                BuildTileViews();
            }

            int index = 0;
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (index >= _tileViews.Count)
                        return;

                    var sr = _tileViews[index++];
                    var tile = grid.Get(x, y);

                    if (tile.IsEmpty)
                    {
                        sr.color = Color.clear; // invisible when empty
                    }
                    else
                    {
                        sr.color = GetColorForTile(tile.TileTypeId);
                    }
                }
            }
        }

        private Color GetColorForTile(int tileTypeId)
        {
            if (tileDatabase != null && tileDatabase.TileTypes != null)
            {
                foreach (var type in tileDatabase.TileTypes)
                {
                    if (type != null && type.Id == tileTypeId)
                    {
                        // Adjust property name if your TileTypeSO uses a different field
                        return type.DebugColor;
                    }
                }
            }

            return Color.white;
        }

        private void ClearExistingViews()
        {
            foreach (var sr in _tileViews)
            {
                if (sr != null)
                    DestroyImmediate(sr.gameObject);
            }
            _tileViews.Clear();
        }

        private void LateUpdate()
        {
            // Simple: update every frame for now.
            // Later we can optimize with events or dirty flags.
            SyncFromGrid();
        }
    }
}