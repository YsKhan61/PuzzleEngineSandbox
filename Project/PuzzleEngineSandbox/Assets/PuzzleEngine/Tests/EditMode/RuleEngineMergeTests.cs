using System.Collections.Generic;
using NUnit.Framework;
using PuzzleEngine.Runtime.Core;
using PuzzleEngine.Runtime.Rules;
using UnityEngine;

namespace PuzzleEngine.Tests.Editmode
{
    public class RuleEngineMergeTests
    {
        [Test]
        public void MergeRule_IncreasesLevel_ForSameTileType()
        {
            // Arrange
            // Create tile type
            var wood = ScriptableObject.CreateInstance<TileTypeSO>();
            typeof(TileTypeSO).GetField("id", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(wood, 1);

            typeof(TileTypeSO).GetField("displayName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(wood, "Wood");

            // Create database
            var db = ScriptableObject.CreateInstance<TileDatabaseSO>();
            db.SetTileTypesForTests(new List<TileTypeSO> { wood });

            // Create merge rule: Wood + Wood -> level +1
            var rule = ScriptableObject.CreateInstance<MergeRulesSO>();
            rule.tileA = wood;
            rule.tileB = wood;
            rule.unordered = true;
            rule.isMergeRule = true;
            rule.levelDelta = 1;
            rule.resultMode = MergeRulesSO.RuleResultMode.ReplaceBoth;

            var ruleSet = ScriptableObject.CreateInstance<RuleSetSO>();
            ruleSet.SetRulesForTests(new List<MergeRulesSO> { rule });

            var engine = new RuleEngine(db, ruleSet);

            // Two identical tiles at level 1
            var a = new TileData(tileTypeId: 1, level: 1);
            var b = new TileData(tileTypeId: 1, level: 1);

            // Act
            var applied = engine.TryApply(a, b, out var newA, out var newB);

            // Assert
            Assert.IsTrue(applied, "RuleEngine should apply merge rule for two wood tiles.");
            Assert.AreEqual(2, newA.Level, "Merged tile level should have increased to 2.");
            Assert.AreEqual(2, newB.Level, "Both tiles are replaced and should have the same level.");
        }
    }
}
