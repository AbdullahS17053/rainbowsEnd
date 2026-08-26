using System;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Shop;
using RainbowsEnd.Systems.Economy;
using TMPro;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class ShopPanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text instructionsText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private ShopUpgradeButtonView[] upgradeButtonViews = Array.Empty<ShopUpgradeButtonView>();

        private ShopController _shopController;

        public void ConfigureView(
            GameObject shopPanelRoot,
            TMP_Text panelTitleText,
            TMP_Text panelGoldText,
            TMP_Text panelInstructionsText,
            TMP_Text interactionPromptText,
            ShopUpgradeButtonView[] buttonViews)
        {
            panelRoot = shopPanelRoot;
            titleText = panelTitleText;
            goldText = panelGoldText;
            instructionsText = panelInstructionsText;
            promptText = interactionPromptText;
            upgradeButtonViews = buttonViews ?? Array.Empty<ShopUpgradeButtonView>();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        public void Configure(ShopController controller)
        {
            _shopController = controller;

            for (int index = 0; index < upgradeButtonViews.Length; index++)
            {
                upgradeButtonViews[index]?.Configure(index, _shopController);
            }
        }

        public void Refresh(ShopConfig shopConfig, EconomyState economyState, PlayerUpgradeRuntime upgradeRuntime)
        {
            if (shopConfig == null)
            {
                return;
            }

            int currentGold = economyState != null ? economyState.CurrentGold : 0;
            ShopUpgradeDefinition[] upgrades = shopConfig.Upgrades;

            if (titleText != null)
            {
                titleText.text = shopConfig.ShopTitle;
            }

            if (goldText != null)
            {
                goldText.text = $"Gold: {currentGold}";
            }

            if (instructionsText != null)
            {
                
            }

            for (int index = 0; index < upgradeButtonViews.Length; index++)
            {
                ShopUpgradeButtonView buttonView = upgradeButtonViews[index];
                if (buttonView == null)
                {
                    continue;
                }

                if (index >= upgrades.Length)
                {
                    buttonView.Hide();
                    continue;
                }

                ShopUpgradeDefinition definition = upgrades[index];
                int currentLevel = upgradeRuntime != null ? upgradeRuntime.GetPurchasedLevel(definition.EffectType) : 0;
                buttonView.Configure(index, _shopController);
                buttonView.Refresh(definition, currentLevel, currentGold);
            }
        }

        public void SetShopVisible(bool isVisible)
        {
            GameObject target = panelRoot != null ? panelRoot : gameObject;
            if (target.activeSelf != isVisible)
            {
                target.SetActive(isVisible);
            }
        }

        public void SetInteractionPromptVisible(bool isVisible)
        {
            if (promptText != null && promptText.gameObject.activeSelf != isVisible)
            {
                promptText.gameObject.SetActive(isVisible);
            }
        }

        private void CacheReferences()
        {
            panelRoot ??= gameObject;

            if (upgradeButtonViews == null || upgradeButtonViews.Length == 0)
            {
                upgradeButtonViews = GetComponentsInChildren<ShopUpgradeButtonView>(true);
            }
        }
    }
}