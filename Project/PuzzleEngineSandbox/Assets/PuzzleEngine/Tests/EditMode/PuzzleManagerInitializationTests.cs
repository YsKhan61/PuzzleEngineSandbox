using System.Reflection;
using NUnit.Framework;
using PuzzleEngine.Runtime.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PuzzleEngine.Tests.EditMode
{
    public class PuzzleManagerInitializationTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        [Test]
        public void EnsureInitialized_WithDefaultLayout_AppliesLayoutToGrid()
        {
            // We know PuzzleManager.Awake will log one error because gridConfig is null.
            LogAssert.Expect(
                LogType.Error,
                "[PuzzleManager] GridConfigSO is not assigned. Creating a default in-memory asset (6x6)."
            );
            
            // Create PuzzleManager
            var go = new GameObject("PuzzleManager_DefaultLayout_Test");
            var pm = go.AddComponent<PuzzleManager>();

            // Grid config: 2x2 for simplicity
            var gridConfig = ScriptableObject.CreateInstance<GridConfigSO>();
            gridConfig.width = 2;
            gridConfig.height = 2;
            SetPrivateField(pm, "gridConfig", gridConfig);

            // Turn off auto-load initially so we can prepare the default layout
            SetPrivateField(pm, "autoLoadDefaultLayout", false);

            // First init to create a grid
            pm.EnsureInitialized();
            var grid = pm.Grid;
            Assert.IsNotNull(grid, "Grid should be initialized.");

            // Fill grid with a known pattern (TileTypeId = 7)
            var tilePattern = new TileData { tileTypeId = 7 };
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                grid.Set(x, y, tilePattern);
            }

            // Capture this pattern into a LevelLayoutSO
            var defaultLayout = ScriptableObject.CreateInstance<LevelLayoutSO>();
            pm.SaveCurrentLayout(defaultLayout);

            // Wire defaultLayout + enable autoLoadDefaultLayout
            SetPrivateField(pm, "defaultLayout", defaultLayout);
            SetPrivateField(pm, "autoLoadDefaultLayout", true);

            // Now manually change the grid to a different pattern (TileTypeId = 3)
            var otherTile = new TileData { tileTypeId = 3 };
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                grid.Set(x, y, otherTile);
            }

            // Call EnsureInitialized again – this should apply defaultLayout to the existing grid
            pm.EnsureInitialized();
            grid = pm.Grid; // in case it was rebuilt

            // Assert: grid has been overwritten with the default layout pattern (TileTypeId = 7)
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                var cell = grid.Get(x, y);
                Assert.AreEqual(7, cell.TileTypeId,
                    $"Expected cell ({x},{y}) to have TileTypeId 7 from defaultLayout.");
            }
        }
    }
}