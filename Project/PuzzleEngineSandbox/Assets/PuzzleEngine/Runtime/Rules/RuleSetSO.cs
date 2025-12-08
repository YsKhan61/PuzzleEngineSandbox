using System.Collections.Generic;
using UnityEngine;

namespace PuzzleEngine.Rules
{
    /// <summary>
    /// Set of interaction rules used by the puzzle engine.
    /// PuzzleManager will reference exactly one of these at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleEngine/Rule Set", fileName = "RuleSet")]
    public class RuleSetSO : ScriptableObject
    {
        [SerializeField]
        private List<InteractionRuleSO> rules = new List<InteractionRuleSO>();

        public IReadOnlyList<InteractionRuleSO> Rules => rules;
        
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRulesForTests(List<InteractionRuleSO> list)
        {
            rules = list;
        }
#endif

    }
}