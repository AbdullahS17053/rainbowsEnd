using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using TMPro;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class EconomyHudPresenter : MonoBehaviour
    {
        [SerializeField] private PotRegistry potRegistry;
        [SerializeField] private PlayerWallet playerWallet;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text bankedGoldText;
        [SerializeField] private TMP_Text potGoldText;
        [SerializeField] private string labelPrefix = "Banked: ";

        private int _lastBankedGold = int.MinValue;
        private int _lastPotGold = int.MinValue;

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
            Configure(registry, null, label);
        }

        public void Configure(PotRegistry registry, PlayerWallet wallet, TMP_Text label)
        {
            potRegistry = registry;
            playerWallet = wallet;
            goldText = label;
            _lastBankedGold = int.MinValue;
            _lastPotGold = int.MinValue;
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
            playerWallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        }

        private void RefreshLabel()
        {
            int bankedGold = playerWallet != null ? playerWallet.SpendableGold : 0;
            int potGold = potRegistry != null ? potRegistry.TotalStoredGold : 0;

            if (bankedGold == _lastBankedGold && potGold == _lastPotGold)
            {
                return;
            }

            _lastBankedGold = bankedGold;
            _lastPotGold = potGold;

            if (bankedGoldText != null)
            {
                bankedGoldText.text = $"Banked: {bankedGold}";
            }

            if (potGoldText != null)
            {
                potGoldText.text = $"Pots: {potGold}";
            }

            if (goldText != null && goldText != bankedGoldText && goldText != potGoldText)
            {
                goldText.text = $"{labelPrefix}{bankedGold}  |  Pots: {potGold}";
            }
        }
    }
}