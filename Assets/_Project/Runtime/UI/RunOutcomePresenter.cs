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
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button restartButton;
        [SerializeField] private string restartSceneName = SceneNames.MainArena;

        public void ConfigureView(GameObject root, TMP_Text title, TMP_Text body, Button restart)
        {
            panelRoot = root;
            titleText = title;
            bodyText = body;
            restartButton = restart;
            ConfigureButton();
        }

        public void Show(RunOutcome outcome, int completedWaveCount, int totalWaveCount)
        {
            ConfigureButton();

            if (titleText != null)
            {
                titleText.text = outcome == RunOutcome.Win ? "You Win!" : "You Lose";
            }

            if (bodyText != null)
            {
                bodyText.text = outcome == RunOutcome.Win
                    ? $"All {Mathf.Max(0, totalWaveCount)} waves cleared."
                    : $"All pots were destroyed during wave {Mathf.Clamp(completedWaveCount + 1, 1, Mathf.Max(1, totalWaveCount))} of {Mathf.Max(1, totalWaveCount)}.";
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
