using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Systems.Economy;
using TMPro;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class EconomyHudPresenter : MonoBehaviour
    {
        [SerializeField] private EconomyState economyState;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private string labelPrefix = "Gold: ";

        private void Awake()
        {
            ResolveReferences();
            RefreshLabel();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (economyState != null)
            {
                economyState.GoldChanged += HandleGoldChanged;
            }

            RefreshLabel();
        }

        private void OnDisable()
        {
            if (economyState != null)
            {
                economyState.GoldChanged -= HandleGoldChanged;
            }
        }

        public void Configure(EconomyState state, TMP_Text label)
        {
            if (economyState != null)
            {
                economyState.GoldChanged -= HandleGoldChanged;
            }

            economyState = state;
            goldText = label;

            if (isActiveAndEnabled && economyState != null)
            {
                economyState.GoldChanged += HandleGoldChanged;
            }

            RefreshLabel();
        }

        private void ResolveReferences()
        {
            goldText ??= GetComponent<TMP_Text>();
            if (economyState == null && ProjectContext.Instance?.Services.TryGet(out EconomyState service) == true)
            {
                economyState = service;
            }

            economyState ??= FindFirstObjectByType<EconomyState>(FindObjectsInactive.Include);
        }

        private void HandleGoldChanged(int currentGold)
        {
            RefreshLabel(currentGold);
        }

        private void RefreshLabel()
        {
            RefreshLabel(economyState != null ? economyState.CurrentGold : 0);
        }

        private void RefreshLabel(int currentGold)
        {
            if (goldText == null)
            {
                return;
            }

            goldText.text = $"{labelPrefix}{currentGold}";
        }
    }
}