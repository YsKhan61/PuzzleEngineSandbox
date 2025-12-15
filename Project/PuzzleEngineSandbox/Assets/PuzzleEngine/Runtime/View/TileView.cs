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
        [SerializeField] private SpriteRenderer highlightRenderer;

        public int X { get; private set; }
        public int Y { get; private set; }
        public TileData Data { get; private set; }

        private void Reset()
        {
            if (!spriteRenderer)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (!highlightRenderer)
            {
                // Try find a child called "Highlight" if user created one
                var highlight = transform.Find("TileHighlighter");
                if (highlight != null)
                    highlightRenderer = highlight.GetComponent<SpriteRenderer>();
            }
            
            if (highlightRenderer)
                highlightRenderer.enabled = false;
        }

        public void Initialize(int x, int y, TileData data, Color color)
        {
            X = x;
            Y = y;
            Data = data;

            UpdateVisual(color);
            SetSelected(false);
        }

        public void UpdateFromModel(TileData data, Color color)
        {
            Data = data;
            UpdateVisual(color);
        }
        
        public void SetSelected(bool selected)
        {
            if (highlightRenderer)
                highlightRenderer.enabled = selected;
            else
            {
                // Fallback: small scale bump if no highlight sprite is set
                transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
            }
        }

        private void UpdateVisual(Color color)
        {
            if (!spriteRenderer)
                return;

            spriteRenderer.color = color;
        }
    }
}