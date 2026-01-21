using System;
using UnityEngine;

namespace PuzzleEngine.Runtime.Goals
{
    /// <summary>
    /// The basic kinds of goals supported by the engine.
    /// </summary>
    public enum GoalType
    {
        /// <summary>
        /// Level is satisfied for this goal when there are ZERO tiles
        /// of the specified tileTypeId on the grid.
        /// Example: "Clear all Water tiles".
        /// </summary>
        ClearAllOfType = 0,

        /// <summary>
        /// Level is satisfied for this goal when there are AT LEAST
        /// targetCount tiles of the specified tileTypeId on the grid.
        /// Example: "Have at least 5 Steam tiles".
        /// </summary>
        HaveAtLeastCountOfType = 1,
    }

    /// <summary>
    /// Author-time description of a single goal.
    /// </summary>
    [Serializable]
    public struct GoalDefinition
    {
        [Tooltip("What kind of goal this is (clear-all, reach-count, etc.).")]
        public GoalType type;

        [Tooltip("Tile type ID this goal is about (matches TileTypeSO.Id).")]
        public int tileTypeId;

        [Tooltip("Target count used for HaveAtLeastCountOfType goals. Ignored for ClearAllOfType.")]
        public int targetCount;

        [Tooltip("Optional label for UI. If empty, a default label can be generated.")]
        public string label;
    }
}

