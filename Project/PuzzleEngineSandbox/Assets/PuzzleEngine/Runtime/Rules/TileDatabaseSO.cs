using System.Collections.Generic;
using UnityEngine;

namespace PuzzleEngine.Runtime.Rules
{
    /// <summary>
    /// Central registry of all TileTypeSO assets used by the puzzle engine.
    /// At runtime, the RuleEngine builds lookups from this database.
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleEngine/Tile Database", fileName = "TileDatabase")]
    public class TileDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private List<TileTypeSO> tileTypes = new List<TileTypeSO>();

        /// <summary>
        /// All registered tile types in this database.
        /// </summary>
        public IReadOnlyList<TileTypeSO> TileTypes => tileTypes;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetTileTypesForTests(System.Collections.Generic.List<TileTypeSO> types)
        {
            tileTypes = types;
        }
#endif
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Basic uniqueness check on IDs to avoid runtime surprises.
            var seen = new HashSet<int>();

            foreach (var t in tileTypes)
            {
                if (t == null)
                    continue;

                if (seen.Contains(t.Id))
                {
                    Debug.LogError(
                        $"[TileDatabaseSO] Duplicate tile ID detected: {t.Id} on TileType '{t.name}'. " +
                        "Each TileTypeSO must have a unique Id.",
                        this);
                }
                else
                {
                    seen.Add(t.Id);
                }
            }
        }
#endif
    }
}