using PuzzleEngine.Runtime.Core;
using PuzzleEngine.Runtime.Rules;
using UnityEditor;
using UnityEngine;

namespace PuzzleEngine.EditorTools
{
    public class PuzzleGridEditorWindow : EditorWindow
    {
        private const float CellSize = 32f;
        private const float CellPadding = 4f;
        
        private PuzzleManager _puzzleManager;
        private TileDatabaseSO _tileDatabase;

        private int _selectedTileIndex = -1;
        private Vector2 _scrollPos;
        
        private LevelLayoutSO _currentLayout;

        [MenuItem("PuzzleEngine/Grid Editor")]
        public static void Open()
        {
            var window = GetWindow<PuzzleGridEditorWindow>("Puzzle Grid Editor");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Puzzle Grid Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawReferencesSection();

            if (!_puzzleManager || _puzzleManager.Grid == null)
            {
                EditorGUILayout.HelpBox("Assign a PuzzleManager with a valid Grid to begin editing.", MessageType.Info);
                return;
            }

            if (!_tileDatabase)
            {
                EditorGUILayout.HelpBox("TileDatabase is not set. Assign it on the PuzzleManager.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            DrawTilePalette();
            EditorGUILayout.Space();
            DrawLayoutSection();
            EditorGUILayout.Space();
            DrawGridEditor();
        }

        private void DrawReferencesSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("References", EditorStyles.boldLabel);

                var newManager = (PuzzleManager)EditorGUILayout.ObjectField(
                    "Puzzle Manager",
                    _puzzleManager,
                    typeof(PuzzleManager),
                    true);

                if (newManager != _puzzleManager)
                {
                    _puzzleManager = newManager;
                    if (_puzzleManager)
                    {
                        _puzzleManager.EnsureInitialized();          // ✅ calls the same path as runtime
                        _tileDatabase = _puzzleManager.GetTileDatabaseForDebug();
                    }
                }

                if (_puzzleManager && !_tileDatabase)
                {
                    _tileDatabase = GetTileDatabaseFromManager(_puzzleManager);
                }

                EditorGUILayout.LabelField("Grid Size",
                    _puzzleManager && _puzzleManager.Grid != null
                        ? $"{_puzzleManager.Grid.Width} x {_puzzleManager.Grid.Height}"
                        : "-");
            }
        }

        private TileDatabaseSO GetTileDatabaseFromManager(PuzzleManager manager)
        {
#if UNITY_EDITOR
            // Using debug accessor we added earlier
            return manager ? manager.GetTileDatabaseForDebug() : null;
#else
            return null;
#endif
        }

        private void DrawTilePalette()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);

                if (!_tileDatabase || _tileDatabase.TileTypes == null || _tileDatabase.TileTypes.Count == 0)
                {
                    EditorGUILayout.LabelField("No tile types found in TileDatabase.");
                    return;
                }

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(100));

                for (int i = 0; i < _tileDatabase.TileTypes.Count; i++)
                {
                    var tileType = _tileDatabase.TileTypes[i];
                    if (!tileType)
                        continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var style = new GUIStyle(EditorStyles.miniButton);
                        if (i == _selectedTileIndex)
                            style.normal.textColor = Color.green;

                        if (GUILayout.Button($"{tileType.Id} - {tileType.DisplayName}", style))
                        {
                            _selectedTileIndex = i;
                        }

                        var colorRect = GUILayoutUtility.GetRect(32, 16);
                        EditorGUI.DrawRect(colorRect, tileType.DebugColor);
                    }
                }

                EditorGUILayout.EndScrollView();

                if (_selectedTileIndex < 0)
                {
                    EditorGUILayout.HelpBox("Select a tile type to paint with.", MessageType.Info);
                }
            }
        }
        
        private void DrawLayoutSection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);

                _currentLayout = (LevelLayoutSO)EditorGUILayout.ObjectField(
                    "Level Layout",
                    _currentLayout,
                    typeof(LevelLayoutSO),
                    false);

                if (!_currentLayout)
                {
                    EditorGUILayout.HelpBox(
                        "Assign a LevelLayout asset to save or load grid state.",
                        MessageType.Info);
                    return;
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Load Layout → Grid"))
                {
                    if (!_puzzleManager)
                    {
                        Debug.LogWarning("[PuzzleGridEditor] No PuzzleManager assigned.");
                    }
                    else
                    {
                        _puzzleManager.EnsureInitialized();
                        _puzzleManager.LoadLayout(_currentLayout);
                        SceneView.RepaintAll();
                        Repaint();
                    }
                }

                if (GUILayout.Button("Save Grid → Layout"))
                {
                    if (!_puzzleManager)
                    {
                        Debug.LogWarning("[PuzzleGridEditor] No PuzzleManager assigned.");
                    }
                    else
                    {
                        _puzzleManager.EnsureInitialized();
                        _puzzleManager.SaveCurrentLayout(_currentLayout);
                        AssetDatabase.SaveAssets();
                        SceneView.RepaintAll();
                        Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }


        private void DrawGridEditor()
        {
            var grid = _puzzleManager.Grid;
            if (grid == null)
                return;

            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

            float totalWidth = grid.Width * (CellSize + CellPadding);
            float totalHeight = grid.Height * (CellSize + CellPadding);

            var rect = GUILayoutUtility.GetRect(totalWidth, totalHeight, GUILayout.ExpandWidth(false));

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var cellRect = new Rect(
                        rect.x + x * (CellSize + CellPadding),
                        rect.y + y * (CellSize + CellPadding),
                        CellSize,
                        CellSize);

                    var tile = grid.Get(x, y);

                    // Background
                    Color bg = new Color(0.15f, 0.15f, 0.15f, 1f);
                    if (!tile.IsEmpty)
                    {
                        var t = FindTileType(tile.TileTypeId);
                        bg = t ? t.DebugColor : new Color(0.3f, 0.3f, 0.3f, 1f);
                    }

                    EditorGUI.DrawRect(cellRect, bg);

                    // Label: id or .
                    string label = tile.IsEmpty ? "." : tile.TileTypeId.ToString();
                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(cellRect, label, style);

                    // Handle click
                    if (Event.current.type == EventType.MouseDown &&
                        Event.current.button == 0 &&
                        cellRect.Contains(Event.current.mousePosition))
                    {
                        OnCellClicked(x, y);
                        Event.current.Use();
                    }
                }
            }
        }

        private TileTypeSO FindTileType(int id)
        {
            if (!_tileDatabase) return null;

            foreach (var t in _tileDatabase.TileTypes)
            {
                if (t && t.Id == id)
                    return t;
            }

            return null;
        }

        private void OnCellClicked(int x, int y)
        {
            if (_selectedTileIndex < 0 ||
                !_tileDatabase ||
                _selectedTileIndex >= _tileDatabase.TileTypes.Count)
            {
                return;
            }

            var tileType = _tileDatabase.TileTypes[_selectedTileIndex];
            if (!tileType)
                return;

            var tile = new TileData(tileType.Id, level: 1);
            _puzzleManager.TrySetTile(x, y, tile);

            // Ensure Scene view repaints
            SceneView.RepaintAll();
            Repaint();
        }
    }
}
