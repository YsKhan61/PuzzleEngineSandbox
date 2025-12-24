using System.Collections;
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
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;

        [Header("Invalid Selection Feedback")]
        [SerializeField] private float invalidFlashDuration = 0.15f;
        [SerializeField] private Color invalidFlashColor = Color.red;

        public int X { get; private set; }
        public int Y { get; private set; }
        public TileData Data { get; private set; }

        private Color _baseColor = Color.white;
        private Coroutine _invalidFlashRoutine;

        private void Reset()
        {
            if (!spriteRenderer)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (!highlightRenderer)
            {
                // Try find a child called "TileHighlighter" if user created one
                var highlight = transform.Find("TileHighlighter");
                if (highlight != null)
                    highlightRenderer = highlight.GetComponent<SpriteRenderer>();
            }

            if (highlightRenderer)
                highlightRenderer.enabled = false;

            if (spriteRenderer)
                _baseColor = spriteRenderer.color;
        }

        private void Awake()
        {
            // Ensure base color is in sync on play
            if (spriteRenderer)
                _baseColor = spriteRenderer.color;

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
            {
                highlightRenderer.enabled = selected;
            }
            else
            {
                // Fallback: small scale bump if no highlight sprite is set
                transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
            }
        }

        /// <summary>
        /// Triggers a short visual flash to indicate an invalid selection.
        /// Intended to be called when a second click is not allowed
        /// by the current InteractionRule.
        /// </summary>
        public void ShowInvalidSelection()
        {
            if (!spriteRenderer)
                return;

            if (_invalidFlashRoutine != null)
                StopCoroutine(_invalidFlashRoutine);

            _invalidFlashRoutine = StartCoroutine(InvalidFlashRoutine());
        }

        private IEnumerator InvalidFlashRoutine()
        {
            float elapsed = 0f;

            while (elapsed < invalidFlashDuration)
            {
                // Simple ping-pong blend between base and invalid colors
                float t = Mathf.PingPong(elapsed * 10f, 1f);
                spriteRenderer.color = Color.Lerp(_baseColor, invalidFlashColor, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            spriteRenderer.color = _baseColor;
            _invalidFlashRoutine = null;
        }

        private void UpdateVisual(Color color)
        {
            if (!spriteRenderer)
                return;

            _baseColor = color;
            spriteRenderer.color = color;
        }
    }
}
