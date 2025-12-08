using System.Collections.Generic;
using PuzzleEngine.Runtime.Core;

namespace PuzzleEngine.Runtime.Simulation
{
    /// <summary>
    /// Deterministic, single-step grid simulator.
    /// Scans the grid in a fixed order, finds valid interactions, and applies them.
    /// </summary>
    public sealed class GridSimulator
    {
        private readonly RuleEngine _ruleEngine;

        // Right and Up = deterministic neighbor order
        private static readonly (int dx, int dy)[] NeighborOffsets =
        {
            (1, 0), // right
            (0, 1), // up   (or down, just keep it consistent)
        };

        public GridSimulator(RuleEngine ruleEngine)
        {
            _ruleEngine = ruleEngine;
        }

        /// <summary>
        /// Performs one simulation step.
        /// Returns true if any tiles changed, false if the grid is stable for this step.
        /// </summary>
        public bool Step(GridModel grid)
        {
            if (_ruleEngine == null || grid == null)
                return false;

            int width  = grid.Width;
            int height = grid.Height;

            // Track which tiles are already "consumed" by an interaction this step
            var used = new bool[width, height];

            // Collect all changes, then apply at the end to avoid mid-scan interference
            var changes = new List<InteractionChange>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (used[x, y])
                        continue;

                    var a = grid.Get(x, y);
                    if (a.IsEmpty)
                        continue;

                    // Try interaction with neighbors in a fixed order
                    foreach (var (dx, dy) in NeighborOffsets)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (!grid.IsInside(nx, ny) || used[nx, ny])
                            continue;

                        var b = grid.Get(nx, ny);
                        if (b.IsEmpty)
                            continue;

                        if (_ruleEngine.TryApply(a, b, out var newA, out var newB))
                        {
                            used[x, y]     = true;
                            used[nx, ny]   = true;

                            changes.Add(new InteractionChange
                            {
                                x1 = x,   y1 = y,   newA = newA,
                                x2 = nx,  y2 = ny,  newB = newB
                            });

                            // Important for determinism: once A interacts with a neighbor,
                            // we don't try other neighbors this step.
                            break;
                        }
                    }
                }
            }

            if (changes.Count == 0)
                return false; // No changes, grid is stable for this step.

            // Apply changes collected.
            foreach (var c in changes)
            {
                grid.Set(c.x1, c.y1, c.newA);
                grid.Set(c.x2, c.y2, c.newB);
            }

            return true;
        }

        private struct InteractionChange
        {
            public int x1, y1;
            public int x2, y2;
            public TileData newA;
            public TileData newB;
        }
    }
}
