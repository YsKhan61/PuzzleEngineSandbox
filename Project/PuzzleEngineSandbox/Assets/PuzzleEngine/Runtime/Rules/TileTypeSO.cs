using UnityEngine;

namespace PuzzleEngine.Runtime.Rules
{
    /// <summary>
    /// Scriptable definition of a tile type (e.g., Fire, Water, Wood).
    /// Referenced from TileData by numeric id for DOTS friendliness.
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleEngine/Tile Type", fileName = "TileType")]
    public class TileTypeSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique numeric ID used in TileData.tileTypeId. Must be unique across all TileTypes.")]
        [SerializeField] private int id = -1;

        [Tooltip("Human-readable name for debugging and tools.")]
        [SerializeField] private string displayName = "New Tile Type";

        [Header("Visuals (for debug / later rendering)")]
        [SerializeField] private Color debugColor = Color.white;
        [SerializeField] private Sprite icon;

        [Header("Behavior Flags")]
        [Tooltip("Can this tile participate in merge rules (A + A -> higher level)?")]
        [SerializeField] private bool canMerge = true;

        [Tooltip("Maximum level this tile can reach when merging.")]
        [Min(1)]
        [SerializeField] private int maxLevel = 3;

        public int Id           => id;
        public string DisplayName => displayName;
        public Color DebugColor => debugColor;
        public Sprite Icon      => icon;
        public bool CanMerge    => canMerge;
        public int MaxLevel     => maxLevel;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;
        }
#endif
    }
}