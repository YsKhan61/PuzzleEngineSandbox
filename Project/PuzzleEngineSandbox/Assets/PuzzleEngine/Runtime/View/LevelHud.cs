using PuzzleEngine.Runtime.Core;
using TMPro;
using UnityEngine;

namespace PuzzleEngine.Runtime.View
{
    /// <summary>
    /// Minimal HUD that shows remaining moves and win/lose panels.
    /// </summary>
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzleManager;

        [Header("UI")]
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        private void Awake()
        {
            if (puzzleManager == null)
                puzzleManager = FindObjectOfType<PuzzleManager>();
        }

        private void OnEnable()
        {
            if (puzzleManager == null)
                return;

            puzzleManager.SessionChanged += HandleSessionChanged;
            puzzleManager.LevelCompletedEvent += HandleLevelCompleted;
            puzzleManager.LevelFailedEvent += HandleLevelFailed;

            RefreshAll();
        }

        private void OnDisable()
        {
            if (puzzleManager == null)
                return;

            puzzleManager.SessionChanged -= HandleSessionChanged;
            puzzleManager.LevelCompletedEvent -= HandleLevelCompleted;
            puzzleManager.LevelFailedEvent -= HandleLevelFailed;
        }

        private void RefreshAll()
        {
            HandleSessionChanged();
        }

        private void HandleSessionChanged()
        {
            if (movesText != null)
                movesText.text = puzzleManager.RemainingMoves.ToString();

            if (winPanel != null)
                winPanel.SetActive(puzzleManager.LevelCompleted);

            if (losePanel != null)
                losePanel.SetActive(puzzleManager.LevelFailed);
        }

        private void HandleLevelCompleted()
        {
            if (winPanel != null)
                winPanel.SetActive(true);

            if (losePanel != null)
                losePanel.SetActive(false);
        }

        private void HandleLevelFailed()
        {
            if (losePanel != null)
                losePanel.SetActive(true);

            if (winPanel != null)
                winPanel.SetActive(false);
        }
    }
}