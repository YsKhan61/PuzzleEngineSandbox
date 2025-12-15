using System;
using System.Collections.Generic;
using PuzzleEngine.Runtime.Rules;
using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// Runtime rule lookup and application.
    /// Built from a TileDatabase + RuleSet SOs.
    /// </summary>
    public sealed class RuleEngine
    {
        private readonly Dictionary<RuleKey, MergeRulesSO> _ruleLookup;
        private readonly Dictionary<int, TileTypeSO> _tileById;

        private readonly struct RuleKey : IEquatable<RuleKey>
        {
            public readonly int A;
            public readonly int B;

            public RuleKey(int a, int b)
            {
                A = a;
                B = b;
            }

            public bool Equals(RuleKey other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is RuleKey other && Equals(other);
            public override int GetHashCode() => unchecked((A * 397) ^ B);
        }

        public RuleEngine(TileDatabaseSO tileDatabase, RuleSetSO ruleSet)
        {
            if (tileDatabase == null) throw new ArgumentNullException(nameof(tileDatabase));
            if (ruleSet == null)      throw new ArgumentNullException(nameof(ruleSet));

            _tileById   = BuildTileLookup(tileDatabase);
            _ruleLookup = BuildRuleLookup(ruleSet);
        }

        private static Dictionary<int, TileTypeSO> BuildTileLookup(TileDatabaseSO db)
        {
            var dict = new Dictionary<int, TileTypeSO>();
            foreach (var t in db.TileTypes)
            {
                if (t == null) continue;

                if (dict.ContainsKey(t.Id))
                {
                    Debug.LogError($"[RuleEngine] Duplicate tile id in database: {t.Id} ({t.name})");
                    continue;
                }
                dict.Add(t.Id, t);
            }
            return dict;
        }

        private static Dictionary<RuleKey, MergeRulesSO> BuildRuleLookup(RuleSetSO ruleSet)
        {
            var dict = new Dictionary<RuleKey, MergeRulesSO>();

            foreach (var rule in ruleSet.Rules)
            {
                if (rule == null || rule.tileA == null || rule.tileB == null)
                    continue;

                var idA = rule.tileA.Id;
                var idB = rule.tileB.Id;

                if (rule.unordered)
                {
                    var min = Math.Min(idA, idB);
                    var max = Math.Max(idA, idB);
                    idA = min;
                    idB = max;
                }

                var key = new RuleKey(idA, idB);
                if (dict.ContainsKey(key))
                {
                    Debug.LogError($"[RuleEngine] Duplicate rule for pair ({idA},{idB}) in RuleSet.", rule);
                    continue;
                }

                dict.Add(key, rule);
            }

            return dict;
        }

        private bool TryGetRule(TileData a, TileData b, out MergeRulesSO rule)
        {
            rule = null;

            if (a.IsEmpty || b.IsEmpty)
                return false;

            var idA = a.TileTypeId;
            var idB = b.TileTypeId;

            var key = new RuleKey(idA, idB);
            if (_ruleLookup.TryGetValue(key, out rule))
                return true;

            // Try swapped order for unordered rules
            key = new RuleKey(idB, idA);
            return _ruleLookup.TryGetValue(key, out rule);
        }

        /// <summary>
        /// Compute the result of applying a rule to two tiles, if any.
        /// Returns true if a rule existed and outputs new tiles; false otherwise.
        /// </summary>
        public bool TryApply(TileData a, TileData b, out TileData newA, out TileData newB)
        {
            newA = a;
            newB = b;

            if (!TryGetRule(a, b, out var rule))
                return false;

            TileData result;

            if (rule.isMergeRule && a.TileTypeId == b.TileTypeId)
            {
                // Merge rule: same type, increase level
                result = a;
                result.Level = Math.Min(result.Level + rule.levelDelta, GetMaxLevel(result.TileTypeId));
            }
            else
            {
                // Combination rule: use fixed result type/level
                if (rule.resultType == null)
                {
                    Debug.LogError("[RuleEngine] Combination rule has no resultType assigned.", rule);
                    return false;
                }

                result = new TileData(
                    tileTypeId: rule.resultType.Id,
                    level:      rule.fixedResultLevel,
                    state:      0);
            }

            switch (rule.resultMode)
            {
                case MergeRulesSO.RuleResultMode.ReplaceBoth:
                    newA = result;
                    newB = result;
                    break;

                case MergeRulesSO.RuleResultMode.ReplaceFirst:
                    newA = result;
                    newB = TileData.Empty;
                    break;

                case MergeRulesSO.RuleResultMode.ReplaceSecond:
                    newA = TileData.Empty;
                    newB = result;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private int GetMaxLevel(int tileTypeId)
        {
            if (_tileById.TryGetValue(tileTypeId, out var t))
                return t.MaxLevel;

            return int.MaxValue;
        }
    }
}