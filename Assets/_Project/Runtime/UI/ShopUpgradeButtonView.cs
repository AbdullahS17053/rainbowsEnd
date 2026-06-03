using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RainbowsEnd.UI
{
    public sealed class ShopUpgradeButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Color affordableColor = new Color(0.16f, 0.32f, 0.18f, 0.92f);
        [SerializeField] private Color lockedColor = new Color(0.26f, 0.18f, 0.18f, 0.92f);
        [SerializeField] private Color maxedColor = new Color(0.24f, 0.22f, 0.08f, 0.92f);

        private ShopController _shopController;
        private int _upgradeIndex = -1;

        public void ConfigureView(Button buttonComponent, Image background, TMP_Text titleLabel, TMP_Text detailLabel)
        {
            button = buttonComponent;
            backgroundImage = background;
            titleText = titleLabel;
            detailText = detailLabel;
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        public void Configure(int upgradeIndex, ShopController controller)
        {
            CacheReferences();

            _upgradeIndex = upgradeIndex;
            _shopController = controller;

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        public void Refresh(ShopUpgradeDefinition definition, int currentLevel, int currentGold)
        {
            gameObject.SetActive(true);

            bool isMaxed = currentLevel >= definition.MaxPurchaseCount;
            bool canAfford = currentGold >= definition.Cost;
            bool canPurchase = !isMaxed && canAfford;

            if (button != null)
            {
                button.interactable = canPurchase;
            }

            if (titleText != null)
            {
                titleText.text = $"{_upgradeIndex + 1}. {definition.DisplayName}";
            }

            if (detailText != null)
            {
                detailText.text = $"{definition.Description}\nCost: {definition.Cost}   Level: {currentLevel}/{definition.MaxPurchaseCount}";
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isMaxed
                    ? maxedColor
                    : canAfford
                        ? affordableColor
                        : lockedColor;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleClicked()
        {
            _shopController?.TryPurchaseUpgrade(_upgradeIndex);
        }

        private void CacheReferences()
        {
            button ??= GetComponent<Button>();
            backgroundImage ??= GetComponent<Image>();

            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            if (labels.Length > 0 && titleText == null)
            {
                titleText = labels[0];
            }

            if (labels.Length > 1 && detailText == null)
            {
                detailText = labels[1];
            }
        }
    }
}