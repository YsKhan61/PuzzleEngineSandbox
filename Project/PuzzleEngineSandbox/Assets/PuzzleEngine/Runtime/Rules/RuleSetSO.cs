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
    }
}