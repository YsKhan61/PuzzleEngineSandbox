using System;
using System.Collections.Generic;
using PuzzleEngine.Runtime.Core;

namespace PuzzleEngine.Runtime.Goals
{
    /// <summary>
    /// Evaluates a set of goals against the current GridModel state.
    /// Pure runtime class, no UnityEngine dependencies.
    /// </summary>
    public sealed class GoalEvaluator
    {
        private readonly LevelGoalsSO _config;
        private readonly GoalProgress[] _progress;

        public IReadOnlyList<GoalProgress> Goals => _progress;

        /// <summary>
        /// True when ALL configured goals are currently satisfied.
        /// </summary>
        public bool AllComplete
        {
            get
            {
                if (_progress == null || _progress.Length == 0)
                    return false;

                for (int i = 0; i < _progress.Length; i++)
                {
                    if (!_progress[i].IsComplete)
                        return false;
                }

                return true;
            }
        }

        public GoalEvaluator(LevelGoalsSO config)
        {
            _config = config;

            if (_config != null && _config.goals != null)
            {
                _progress = new GoalProgress[_config.goals.Length];
                for (int i = 0; i < _progress.Length; i++)
                {
                    _progress[i] = new GoalProgress
                    {
                        Definition  = _config.goals[i],
                        CurrentCount = 0
                    };
                }
            }
            else
            {
                _progress = Array.Empty<GoalProgress>();
            }
        }

        /// <summary>
        /// Recomputes all goal progress by scanning the given grid.
        /// This is O(width*height * numberOfGoals), which is fine for small boards.
        /// </summary>
        public void Evaluate(GridModel grid)
        {
            if (grid == null || _progress == null)
                return;

            // Reset counts
            for (int i = 0; i < _progress.Length; i++)
            {
                _progress[i].CurrentCount = 0;
            }

            // Count matching tiles for each goal
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var tile = grid.Get(x, y);
                    if (tile.IsEmpty)
                        continue;

                    int id = tile.TileTypeId;

                    for (int i = 0; i < _progress.Length; i++)
                    {
                        if (_progress[i].Definition.tileTypeId == id)
                        {
                            _progress[i].CurrentCount++;
                        }
                    }
                }
            }
        }
    }
}