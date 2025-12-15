using System.Collections.Generic;
using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Pure helper for evaluating interaction rules.
    /// Contains no MonoBehaviour / input logic so it can be unit tested easily.
    /// </summary>
    public static class InteractionRuleEvaluator
    {
        private static readonly Vector2Int[] OrthoDirs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        private static readonly Vector2Int[] DiagDirs =
        {
            new Vector2Int( 1, 1),
            new Vector2Int( 1,-1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1,-1)
        };

        /// <summary>
        /// Returns true if the player is allowed to interact between these two cells,
        /// based on the given InteractionRuleSO.
        /// </summary>
        public static bool IsSelectionAllowed(Vector2Int a, Vector2Int b, InteractionRuleSO rule)
        {
            if (a == b)
                return false;

            var mode = rule != null ? rule.adjacencyMode : AdjacencyMode.Anywhere;

            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);

            switch (mode)
            {
                case AdjacencyMode.Anywhere:
                    // Any two distinct cells on the board may interact
                    return true;

                case AdjacencyMode.Orthogonal:
                    // Exactly one step in horizontal OR vertical
                    return dx + dy == 1;

                case AdjacencyMode.OrthogonalAndDiagonal:
                    // Any of the 8 neighbours
                    return Mathf.Max(dx, dy) == 1;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Returns neighbour coordinates around a center cell for use in
        /// local cascade modes. The neighbourhood depends on adjacency mode.
        /// </summary>
        public static IEnumerable<Vector2Int> GetNeighborCoords(
            Vector2Int center,
            GridModel grid,
            InteractionRuleSO rule)
        {
            if (grid == null)
                yield break;

            var mode = rule != null ? rule.adjacencyMode : AdjacencyMode.Orthogonal;

            // Always include orthogonal neighbours
            foreach (var dir in OrthoDirs)
            {
                var n = center + dir;
                if (grid.IsInside(n.x, n.y))
                    yield return n;
            }

            // Include diagonals only for Ortho+Diag or Anywhere
            if (mode == AdjacencyMode.OrthogonalAndDiagonal ||
                mode == AdjacencyMode.Anywhere)
            {
                foreach (var dir in DiagDirs)
                {
                    var n = center + dir;
                    if (grid.IsInside(n.x, n.y))
                        yield return n;
                }
            }
        }
    }
}