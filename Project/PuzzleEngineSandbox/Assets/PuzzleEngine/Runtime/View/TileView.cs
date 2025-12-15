using PuzzleEngine.Runtime.Core;
using UnityEngine;

namespace PuzzleEngine.Runtime.View
{
    /// <summary>
    /// Simple visual representation of one tile in the grid.
    /// Keeps track of its grid coordinates and updates sprite/color.
    /// </summary>
    [DisallowMultipleComponent]
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        /// <summary>Grid X coordinate.</summary>
        public int X { get; private set; }

        /// <summary>Grid Y coordinate.</summary>
        public int Y { get; private set; }

        /// <summary>Latest tile data this view represents.</summary>
        public TileData Data { get; private set; }

        private void Reset()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(int x, int y, TileData data)
        {
            X = x;
            Y = y;
            Data = data;

            UpdateVisual();
        }

        public void UpdateFromModel(TileData data)
        {
            Data = data;
            UpdateVisual();
        }

        /// <summary>
        /// Temporary color logic: maps TypeId & Level to a color.
        /// You can later swap this for a proper TileVisualConfigSO.
        /// </summary>
        private void UpdateVisual()
        {
            if (spriteRenderer == null)
                return;

            // Example color mapping:
            // hue from TypeId, brightness from Level
            float hue = Mathf.Repeat(Data.TileTypeId * 0.13f, 1f);
            float value = Mathf.Clamp01(0.5f + Data.Level * 0.1f);

            spriteRenderer.color = Color.HSVToRGB(hue, 0.8f, value);

            // Optional: scale by level
            float scale = 0.9f + Data.Level * 0.05f;
            transform.localScale = Vector3.one * scale;
        }
    }
}