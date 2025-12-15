namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Lightweight value-type representation of a single tile on the grid.
    /// Pure data (no Unity types) so it’s DOTS– and simulation–friendly.
    /// </summary>
    [System.Serializable]
    public struct TileData
    {
        // Backing fields (kept public for simple serialization / inspector debug)
        public int tileTypeId;
        public int level;
        public int state;

        /// <summary>Numeric ID into TileDatabase (–1 = empty).</summary>
        public int TileTypeId => tileTypeId;

        /// <summary>Level used for merges (1,2,3,...).</summary>
        public int Level
        {
            readonly get => level;
            set => level = value;
        }

        /// <summary>Encoded state (future: normal/frozen/burning/etc.).</summary>
        public int State
        {
            readonly get => state;
            set => state = value;
        }

        /// <summary>True if this tile slot is empty.</summary>
        public bool IsEmpty => tileTypeId < 0;

        public TileData(int tileTypeId, int level = 1, int state = 0)
        {
            this.tileTypeId = tileTypeId;
            this.level = level;
            this.state = state;
        }

        public static TileData Empty => new TileData(-1, 0, 0);
    }
}
