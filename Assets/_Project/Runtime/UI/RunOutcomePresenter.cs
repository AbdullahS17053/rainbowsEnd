using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Orchestration;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RainbowsEnd.UI
{
    public sealed class RunOutcomePresenter : MonoBehaviour
    {
        public const string HighScorePlayerPrefsKey = "RainbowsEnd.HighScore";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private string restartSceneName = SceneNames.MainArena;

        public TMP_Text TitleText => titleText;
        public TMP_Text BodyText => bodyText;
        public TMP_Text ScoreText => scoreText;
        public TMP_Text HighScoreText => highScoreText;

        public static int GetSavedHighScore()
        {
            return PlayerPrefs.GetInt(HighScorePlayerPrefsKey, 0);
        }

        public static bool TryUpdateHighScore(int score, out int currentHighScore)
        {
            int previousHighScore = GetSavedHighScore();
            if (score > previousHighScore)
            {
                PlayerPrefs.SetInt(HighScorePlayerPrefsKey, score);
                PlayerPrefs.Save();
                currentHighScore = score;
                return true;
            }

            currentHighScore = previousHighScore;
            return false;
        }

        public static void ResetHighScore()
        {
            PlayerPrefs.DeleteKey(HighScorePlayerPrefsKey);
            PlayerPrefs.Save();
        }

        public void ConfigureView(
            GameObject root,
            TMP_Text title,
            TMP_Text body,
            Button restart,
            TMP_Text score = null,
            TMP_Text highScore = null)
        {
            panelRoot = root;
            titleText = title;
            bodyText = body;
            restartButton = restart;
            if (score != null)
            {
                scoreText = score;
            }
            if (highScore != null)
            {
                highScoreText = highScore;
            }
            ConfigureButton();
        }

        public void Show(RunOutcome outcome, int completedWaveCount, int totalWaveCount)
        {
            Show(outcome, completedWaveCount, totalWaveCount, 0);
        }

        public void Show(RunOutcome outcome, int completedWaveCount, int totalWaveCount, int score)
        {
            ConfigureButton();

            bool isNewHighScore = TryUpdateHighScore(score, out int displayHighScore);

            if (titleText != null)
            {
                titleText.text = outcome == RunOutcome.Win ? "You Win!" : "You Lose";
            }

            string baseBody = outcome == RunOutcome.Win
                ? $"All {Mathf.Max(0, totalWaveCount)} waves cleared."
                : $"All pots were destroyed during wave {Mathf.Clamp(completedWaveCount + 1, 1, Mathf.Max(1, totalWaveCount))} of {Mathf.Max(1, totalWaveCount)}.";

            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            if (highScoreText != null)
            {
                highScoreText.text = isNewHighScore && score > 0
                    ? $"High Score: {displayHighScore} (New Best!)"
                    : $"High Score: {displayHighScore}";
            }

            if (bodyText != null)
            {
                if (scoreText == null && highScoreText == null)
                {
                    string newBestBadge = isNewHighScore && score > 0 ? " (New Best!)" : "";
                    bodyText.text = $"{baseBody}\n\nScore: {score}    High Score: {displayHighScore}{newBestBadge}";
                }
                else
                {
                    bodyText.text = baseBody;
                }
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void RestartRun()
        {
            Time.timeScale = 1f;
            string sceneName = string.IsNullOrWhiteSpace(restartSceneName) ? SceneNames.MainArena : restartSceneName;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            ConfigureButton();
            Hide();
        }

        private void OnEnable()
        {
            ConfigureButton();
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartRun);
            }
        }

        private void CacheReferences()
        {
            panelRoot ??= gameObject;
            restartButton ??= GetComponentInChildren<Button>(true);
            if (scoreText == null)
            {
                scoreText = FindChildText("ScoreText");
            }
            if (highScoreText == null)
            {
                highScoreText = FindChildText("HighScoreText");
            }
        }

        private TMP_Text FindChildText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private void ConfigureButton()
        {
            if (restartButton == null)
            {
                return;
            }

            restartButton.onClick.RemoveListener(RestartRun);
            restartButton.onClick.AddListener(RestartRun);
        }

        private void SetVisible(bool isVisible)
        {
            GameObject target = panelRoot != null ? panelRoot : gameObject;
            if (target.activeSelf != isVisible)
            {
                target.SetActive(isVisible);
            }
        }
    }
}
