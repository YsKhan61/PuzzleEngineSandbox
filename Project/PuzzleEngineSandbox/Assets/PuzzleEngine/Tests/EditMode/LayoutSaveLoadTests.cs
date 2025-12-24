using System.Reflection;
using NUnit.Framework;
using PuzzleEngine.Runtime.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PuzzleEngine.Tests.EditMode
{
    public class LayoutSaveLoadTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private PuzzleManager CreateManagerWithGrid(int width, int height)
        {
            // We know PuzzleManager.Awake will log one error because gridConfig is null.
            LogAssert.Expect(
                LogType.Error,
                "[PuzzleManager] GridConfigSO is not assigned. Creating a default in-memory asset (6x6)."
            );
            
            var go = new GameObject("PuzzleManager_Test");
            var pm = go.AddComponent<PuzzleManager>();

            // Create a GridConfigSO
            var gridConfig = ScriptableObject.CreateInstance<GridConfigSO>();
            gridConfig.width = width;
            gridConfig.height = height;

            // Assign private field gridConfig via reflection
            SetPrivateField(pm, "gridConfig", gridConfig);

            // Disable auto-load of default layout for this test
            SetPrivateField(pm, "autoLoadDefaultLayout", false);

            // Initialize grid
            pm.EnsureInitialized();

            Assert.IsNotNull(pm.Grid, "Grid should be initialized.");
            Assert.AreEqual(width, pm.Grid.Width);
            Assert.AreEqual(height, pm.Grid.Height);

            return pm;
        }

        [Test]
        public void SaveAndLoadLayout_Roundtrip_PreservesGridState()
        {
            const int width = 3;
            const int height = 3;

            var pm = CreateManagerWithGrid(width, height);
            var grid = pm.Grid;

            // Arrange: write a simple pattern into the grid
            // Assumes TileData has an int TileTypeId field
            var tileA = new TileData { tileTypeId = 1 };
            var tileB = new TileData { tileTypeId = 2 };

            grid.Set(0, 0, tileA);
            grid.Set(1, 1, tileB);
            grid.Set(2, 2, tileA);

            var layout = ScriptableObject.CreateInstance<LevelLayoutSO>();

            // Act: save to layout
            pm.SaveCurrentLayout(layout);

            // Overwrite grid with something else
            grid.Clear();

            // Load back from layout
            pm.LoadLayout(layout);

            // Assert: pattern was restored
            Assert.AreEqual(tileA.TileTypeId, grid.Get(0, 0).TileTypeId);
            Assert.AreEqual(tileB.TileTypeId, grid.Get(1, 1).TileTypeId);
            Assert.AreEqual(tileA.TileTypeId, grid.Get(2, 2).TileTypeId);

            // sanity: other cells should not throw / be in-bounds
            Assert.DoesNotThrow(() => { var _ = grid.Get(0, 1); });
        }
    }
}