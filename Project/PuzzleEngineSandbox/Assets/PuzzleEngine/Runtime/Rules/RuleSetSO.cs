using System.Collections.Generic;
using UnityEngine;

namespace PuzzleEngine.Runtime.Rules
{
    /// <summary>
    /// Set of interaction rules used by the puzzle engine.
    /// PuzzleManager will reference exactly one of these at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleEngine/Rule Set", fileName = "RuleSet")]
    public class RuleSetSO : ScriptableObject
    {
        [SerializeField]
        private List<MergeRulesSO> rules = new List<MergeRulesSO>();

        public IReadOnlyList<MergeRulesSO> Rules => rules;
        
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRulesForTests(List<MergeRulesSO> list)
        {
            rules = list;
        }
#endif

    }
}