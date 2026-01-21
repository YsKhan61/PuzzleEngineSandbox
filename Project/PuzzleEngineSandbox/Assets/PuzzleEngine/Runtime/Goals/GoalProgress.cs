using System;

namespace PuzzleEngine.Runtime.Goals
{
    /// <summary>
    /// Runtime snapshot of progress for a single goal.
    /// </summary>
    [Serializable]
    public struct GoalProgress
    {
        public GoalDefinition Definition;
        public int CurrentCount;

        public bool IsComplete
        {
            get
            {
                switch (Definition.type)
                {
                    case GoalType.ClearAllOfType:
                        // Complete when there are zero tiles of that type
                        return CurrentCount == 0;

                    case GoalType.HaveAtLeastCountOfType:
                        // Complete when we have at least the target count
                        return CurrentCount >= Definition.targetCount;

                    default:
                        return false;
                }
            }
        }
    }
}