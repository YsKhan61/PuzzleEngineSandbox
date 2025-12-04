using System;

namespace PuzzleEngine.Core
{
    /// <summary>
    /// Pure in-memory 2D grid of TileData.
    /// This is the single source of truth for puzzle state.
    /// </summary>
    [Serializable]
    public class GridModel
    {
        public int Width  { get; }
        public int Height { get; }

        private readonly TileData[,] _tiles;

        public GridModel(int width, int height)
        {
            if (width <= 0)  throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width  = width;
            Height = height;
            _tiles = new TileData[Width, Height];

            Clear();
        }

        /// <summary>Fills the entire grid with Empty tiles.</summary>
        public void Clear()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _tiles[x, y] = TileData.Empty;
                }
            }
        }

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public TileData Get(int x, int y)
        {
            if (!IsInside(x, y))
                throw new ArgumentOutOfRangeException($"GridModel.Get out of bounds: ({x},{y})");

            return _tiles[x, y];
        }

        public void Set(int x, int y, TileData tile)
        {
            if (!IsInside(x, y))
                throw new ArgumentOutOfRangeException($"GridModel.Set out of bounds: ({x},{y})");

            _tiles[x, y] = tile;
        }
    }
}