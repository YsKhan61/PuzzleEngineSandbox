using UnityEngine;

namespace PuzzleEngine.Rules
{
    /// <summary>
    /// Scriptable definition of a binary interaction rule:
    /// Tile A + Tile B -> Result tiles.
    /// Can represent merges (A + A) or combines (A + B -> C).
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleEngine/Interaction Rule", fileName = "InteractionRule")]
    public class InteractionRuleSO : ScriptableObject
    {
        [Header("Inputs")]
        public TileTypeSO tileA;
        public TileTypeSO tileB;

        [Tooltip("If true, (A,B) and (B,A) are treated as the same interaction.")]
        public bool unordered = true;

        [Header("Result")]
        [Tooltip("How this rule updates the two tiles involved.")]
        public RuleResultMode resultMode = RuleResultMode.ReplaceBoth;

        [Tooltip("Result tile type. For merge rules, typically same as input.")]
        public TileTypeSO resultType;

        [Tooltip("If true and A == B and CanMerge, level will be increased by levelDelta.")]
        public bool isMergeRule = false;

        [Min(1)]
        public int levelDelta = 1;

        [Tooltip("Fixed level to assign to the result (used when not using merge semantics).")]
        [Min(1)]
        public int fixedResultLevel = 1;

        public enum RuleResultMode
        {
            ReplaceBoth,   // Both tiles become the result
            ReplaceFirst,  // Only A becomes result, B becomes empty
            ReplaceSecond, // Only B becomes result, A becomes empty
        }
    }
}