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

        public int X { get; private set; }
        public int Y { get; private set; }
        public TileData Data { get; private set; }

        private void Reset()
        {
            if (!spriteRenderer)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(int x, int y, TileData data, Color color)
        {
            X = x;
            Y = y;
            Data = data;

            UpdateVisual(color);
        }

        public void UpdateFromModel(TileData data, Color color)
        {
            Data = data;
            UpdateVisual(color);
        }

        private void UpdateVisual(Color color)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.color = color;
        }
    }
}