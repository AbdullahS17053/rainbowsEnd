using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Gameplay.Pots;
using TMPro;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class EconomyHudPresenter : MonoBehaviour
    {
        [SerializeField] private PotRegistry potRegistry;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private string labelPrefix = "Gold: ";

        private int _lastDisplayedGold = int.MinValue;

        private void Awake()
        {
            ResolveReferences();
            RefreshLabel();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshLabel();
        }

        private void Update()
        {
            RefreshLabel();
        }

        public void Configure(PotRegistry registry, TMP_Text label)
        {
            potRegistry = registry;
            goldText = label;
            _lastDisplayedGold = int.MinValue;
            RefreshLabel();
        }

        private void ResolveReferences()
        {
            goldText ??= GetComponent<TMP_Text>();
            if (potRegistry == null && ProjectContext.Instance?.Services.TryGet(out PotRegistry service) == true)
            {
                potRegistry = service;
            }

            potRegistry ??= FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
        }

        private void RefreshLabel()
        {
            if (goldText == null)
            {
                return;
            }

            int displayedGold = potRegistry != null ? potRegistry.TotalStoredGold : 0;
            if (displayedGold == _lastDisplayedGold)
            {
                return;
            }

            _lastDisplayedGold = displayedGold;
            goldText.text = $"{labelPrefix}{displayedGold}";
        }
    }
}