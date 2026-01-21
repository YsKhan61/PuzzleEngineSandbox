using UnityEngine;

namespace PuzzleEngine.Runtime.Goals
{
    /// <summary>
    /// ScriptableObject that bundles all goals for a given level.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelGoals",
        menuName = "PuzzleEngine/Level Goals",
        order = 100)]
    public class LevelGoalsSO : ScriptableObject
    {
        [Tooltip("List of goals that must all be satisfied to complete the level.")]
        public GoalDefinition[] goals;
    }
}