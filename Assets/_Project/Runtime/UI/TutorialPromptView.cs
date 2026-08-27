using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RainbowsEnd.UI
{
    public sealed class TutorialPromptView : MonoBehaviour
    {
        public const string OptOutLabel = "Don't show tutorial prompts again";

        [SerializeField] private GameObject cardRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text optOutLabelText;
        [SerializeField] private Toggle optOutToggle;

        private UnityAction _optOutHandler;

        private void Awake()
        {
            SetOptOutLabel();
        }

        public void Configure(UnityAction optOutHandler)
        {
            _optOutHandler = optOutHandler;
            SetOptOutLabel();

            if (optOutToggle == null)
            {
                return;
            }

            optOutToggle.onValueChanged.RemoveListener(HandleOptOutChanged);
            optOutToggle.onValueChanged.AddListener(HandleOptOutChanged);
        }

        public void Show(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }

            optOutToggle?.SetIsOnWithoutNotify(false);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void HandleOptOutChanged(bool isOn)
        {
            if (isOn)
            {
                _optOutHandler?.Invoke();
            }
        }

        private void SetOptOutLabel()
        {
            if (optOutLabelText != null)
            {
                optOutLabelText.text = OptOutLabel;
            }
        }

        private void SetVisible(bool isVisible)
        {
            GameObject target = cardRoot != null ? cardRoot : gameObject;
            if (target.activeSelf != isVisible)
            {
                target.SetActive(isVisible);
            }
        }

        private void OnDestroy()
        {
            optOutToggle?.onValueChanged.RemoveListener(HandleOptOutChanged);
        }
    }
}