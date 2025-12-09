using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Serializable layout for a puzzle grid.
    /// Stores only non-empty tiles to keep level data compact.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelLayout",
        menuName = "PuzzleEngine/Level Layout",
        order = 10)]
    public class LevelLayoutSO : ScriptableObject
    {
        [Min(1)] public int width = 6;
        [Min(1)] public int height = 6;

        [Serializable]
        public struct Cell
        {
            public int x;
            public int y;
            public int tileTypeId;
            public int level;
        }

        [Tooltip("Only non-empty tiles are stored here.")]
        public List<Cell> cells = new();

        /// <summary>
        /// Overwrites this asset's data from the given grid.
        /// Only non-empty cells are stored.
        /// </summary>
        public void CaptureFromGrid(GridModel grid)
        {
            if (grid == null)
            {
                Debug.LogWarning("[LevelLayoutSO] CaptureFromGrid called with null grid.");
                return;
            }

            width = grid.Width;
            height = grid.Height;
            cells.Clear();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var tile = grid.Get(x, y);
                    if (tile.IsEmpty)
                        continue;

                    cells.Add(new Cell
                    {
                        x = x,
                        y = y,
                        tileTypeId = tile.TileTypeId,
                        level = tile.Level
                    });
                }
            }
        }

        /// <summary>
        /// Applies this layout to the given grid.
        /// Grid is resized if dimensions differ.
        /// </summary>
        public void ApplyToGrid(GridModel grid)
        {
            if (grid == null)
            {
                Debug.LogWarning("[LevelLayoutSO] ApplyToGrid called with null grid.");
                return;
            }

            // For now we expect the grid to match the layout size.
            // Later we can add a GridModel.Resize() if needed.
            if (grid.Width != width || grid.Height != height)
            {
                Debug.LogWarning(
                    $"[LevelLayoutSO] Layout size ({width}x{height}) does not match grid size ({grid.Width}x{grid.Height}). " +
                    "Layout will not be applied.");
                return;
            }

            // Clear entire grid to empty
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.Set(x, y, TileData.Empty);
                }
            }

            // Apply non-empty cells from layout
            foreach (var cell in cells)
            {
                if (!grid.IsInside(cell.x, cell.y))
                    continue;

                var tile = new TileData(cell.tileTypeId, cell.level);
                grid.Set(cell.x, cell.y, tile);
            }
        }
    }
}