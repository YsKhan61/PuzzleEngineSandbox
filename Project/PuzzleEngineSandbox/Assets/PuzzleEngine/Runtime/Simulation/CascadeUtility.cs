using PuzzleEngine.Runtime.Core;

namespace PuzzleEngine.Runtime.Simulation
{
    /// <summary>
    /// Helper utilities for applying cascade behaviour on top of GridModel + RuleEngine.
    /// </summary>
    public static class CascadeUtility
    {
        /// <summary>
        /// After a successful interaction between two tile types (originalTypeA, originalTypeB),
        /// apply the same rule globally to all tiles on the board that match those original types.
        ///
        /// Example:
        ///   Fire (1) + Water (2) => Steam (3,3)
        ///   This will convert every Fire or Water tile on the grid into Steam.
        /// </summary>
        public static void ApplyGlobalMatchingPairs(
            GridModel grid,
            RuleEngine ruleEngine,
            int originalTypeA,
            int originalTypeB)
        {
            if (grid == null || ruleEngine == null)
                return;

            // Build sample tiles with the original types so we can ask RuleEngine
            // what the result of A+B is, WITHOUT touching the real grid.
            var a = new TileData { tileTypeId = originalTypeA };
            var b = new TileData { tileTypeId = originalTypeB };

            if (!ruleEngine.TryApply(a, b, out var resultA, out var resultB))
                return; // no rule? then nothing to cascade.

            // Now sweep the whole board and rewrite tiles that match either source type.
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.Get(x, y);

                    if (cell.IsEmpty)
                        continue;

                    if (cell.TileTypeId == originalTypeA)
                    {
                        grid.Set(x, y, resultA);
                    }
                    else if (cell.TileTypeId == originalTypeB)
                    {
                        grid.Set(x, y, resultB);
                    }
                }
            }
        }
    }
}