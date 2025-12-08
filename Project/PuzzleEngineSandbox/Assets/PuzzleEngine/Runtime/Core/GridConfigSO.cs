using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Scriptable configuration for grid dimensions (and later, global puzzle settings).
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleEngine/Grid Config", fileName = "GridConfig")]
    public class GridConfigSO : ScriptableObject
    {
        [Min(1)]
        public int width = 6;

        [Min(1)]
        public int height = 6;

        // Extend later with global knobs (diagonals, max merges, etc).
    }
}