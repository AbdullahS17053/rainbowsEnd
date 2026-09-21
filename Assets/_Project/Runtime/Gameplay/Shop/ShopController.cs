using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Spells;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RainbowsEnd.Gameplay.Shop
{
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private ShopConfig shopConfig;
        [SerializeField] private ArenaSceneContext sceneContext;
        [SerializeField] private FaeTreeAnchor faeTreeAnchor;
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerUpgradeRuntime upgradeRuntime;
        [SerializeField] private PotRegistry potRegistry;
        [SerializeField] private PlayerPortalController portalController;
        [SerializeField] private ShopPanelPresenter panelPresenter;
        [SerializeField] private GameObject potPrefab;
private bool _isShopOpen;
        private bool _wasMovementControllerEnabled;
        private bool _wasAimControllerEnabled;
        private bool _wasPotCarryControllerEnabled;
        private bool _wasWeaponControllerEnabled;
        private bool _wasPortalControllerEnabled;
        private float _previousTimeScale = 1f;

        public bool IsShopOpen => _isShopOpen;

        public void Configure(
            ShopConfig config,
            ArenaSceneContext arenaSceneContext,
            FaeTreeAnchor treeAnchor,
            PlayerRoot root,
            PotRegistry registry,
            PlayerPortalController playerPortalController,
            ShopPanelPresenter presenter)
        {
            shopConfig = config;
            sceneContext = arenaSceneContext;
            faeTreeAnchor = treeAnchor;
            playerRoot = root;
            inputReader = root != null ? root.InputReader : null;
            upgradeRuntime = root != null ? root.GetComponent<PlayerUpgradeRuntime>() : null;
            potRegistry = registry;
            portalController = playerPortalController;
            panelPresenter = presenter;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            SetShopPresentation(false, false);
        }

        private void OnDisable()
        {
            if (_isShopOpen)
            {
                _isShopOpen = false;
                RestorePlayerGameplay();
            }

            SetShopPresentation(false, false);
        }

private void Update()
        {
            ResolveReferences();

            if (!_isShopOpen)
            {
                panelPresenter?.SetInteractionPromptVisible(false);

                if (inputReader != null && inputReader.ShopInteractPressedThisFrame)
                {
                    OpenShop();
                }

                return;
            }

            if (inputReader != null && (inputReader.ShopInteractPressedThisFrame || inputReader.PausePressedThisFrame))
            {
                CloseShop();
                return;
            }

            HandleKeyboardShortcuts();
            RefreshShopPanel();
        }

        public bool TryPurchaseUpgrade(int upgradeIndex)
        {
            if (!_isShopOpen || shopConfig == null || potRegistry == null || upgradeRuntime == null)
            {
                return false;
            }

            ShopUpgradeDefinition[] upgrades = shopConfig.Upgrades;
            if (upgradeIndex < 0 || upgradeIndex >= upgrades.Length)
            {
                return false;
            }

            ShopUpgradeDefinition definition = upgrades[upgradeIndex];
            if (!upgradeRuntime.CanPurchase(definition) || !potRegistry.TrySpendGold(definition.Cost))
            {
                RefreshShopPanel();
                return false;
            }

            if (!upgradeRuntime.ApplyPurchase(definition))
            {
                potRegistry.StoreGold(definition.Cost);
                RefreshShopPanel();
                return false;
            }

            UI.FloatingNumberSpawner.Instance?.SpawnGoldNumber(transform.position, definition.Cost, isGain: false);

            if (definition.EffectType == ShopUpgradeEffectType.PotPurchase2
                || definition.EffectType == ShopUpgradeEffectType.PotPurchase3)
            {
                Vector2 spawnPosition = faeTreeAnchor != null
                    ? (Vector2)faeTreeAnchor.transform.position + Random.insideUnitCircle * 1.5f
                    : (Vector2)transform.position + Random.insideUnitCircle * 1.5f;
                Object.Instantiate(potPrefab, spawnPosition, Quaternion.identity);
            }

            RefreshShopPanel();
            return true;
        }

        private void OpenShop()
        {
            if (!CanOpenShop())
            {
                return;
            }

            _isShopOpen = true;
            LockPlayerGameplay();
            RefreshShopPanel();
            SetShopPresentation(true, false);
        }

        private void CloseShop() {
            if (!_isShopOpen)
            {
                return;
            }

            _isShopOpen = false;
            RestorePlayerGameplay();
            SetShopPresentation(false, false);
        }

        private void RefreshShopPanel()
        {
            if (panelPresenter == null)
            {
                return;
            }

            panelPresenter.Configure(this);
            panelPresenter.Refresh(shopConfig, potRegistry, upgradeRuntime);
        }

        private void ResolveReferences()
        {
            if (sceneContext == null && ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaService) == true)
            {
                sceneContext = arenaService;
            }

            sceneContext ??= FindFirstObjectByType<ArenaSceneContext>(FindObjectsInactive.Include);
            faeTreeAnchor ??= sceneContext != null ? sceneContext.FaeTreeAnchor : FindFirstObjectByType<FaeTreeAnchor>(FindObjectsInactive.Include);

            if (potRegistry == null && ProjectContext.Instance?.Services.TryGet(out PotRegistry potService) == true)
            {
                potRegistry = potService;
            }

            potRegistry ??= FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
            playerRoot ??= FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
            panelPresenter ??= FindFirstObjectByType<ShopPanelPresenter>(FindObjectsInactive.Include);

            if (playerRoot != null)
            {
                inputReader ??= playerRoot.InputReader;
                upgradeRuntime ??= playerRoot.GetComponent<PlayerUpgradeRuntime>();
                portalController ??= playerRoot.GetComponent<PlayerPortalController>();
            }
        }

        private bool CanOpenShop()
        {
            return shopConfig != null
                && panelPresenter != null
                && playerRoot != null
                && playerRoot.CanOpenUpgradeMenu
                && inputReader != null
                && upgradeRuntime != null
                && potRegistry != null
                && shopConfig.Upgrades.Length > 0;
        }

        private void HandleKeyboardShortcuts()
        {
            if (shopConfig == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var numberKeys = new[]
            {
                keyboard.digit1Key,
                keyboard.digit2Key,
                keyboard.digit3Key,
                keyboard.digit4Key,
                keyboard.digit5Key,
                keyboard.digit6Key,
                keyboard.digit7Key,
                keyboard.digit8Key,
                keyboard.digit9Key
            };
            var numpadKeys = new[]
            {
                keyboard.numpad1Key,
                keyboard.numpad2Key,
                keyboard.numpad3Key,
                keyboard.numpad4Key,
                keyboard.numpad5Key,
                keyboard.numpad6Key,
                keyboard.numpad7Key,
                keyboard.numpad8Key,
                keyboard.numpad9Key
            };

            int upgradeCount = Mathf.Min(shopConfig.Upgrades.Length, numberKeys.Length);
            for (int index = 0; index < upgradeCount; index++)
            {
                bool shortcutPressed = numberKeys[index].wasPressedThisFrame || numpadKeys[index].wasPressedThisFrame;
                if (!shortcutPressed)
                {
                    continue;
                }

                TryPurchaseUpgrade(index);
                break;
            }
        }

        private void LockPlayerGameplay()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (playerRoot?.Body != null)
            {
                playerRoot.Body.linearVelocity = Vector2.zero;
                playerRoot.Body.angularVelocity = 0f;
            }

            if (playerRoot?.MovementController != null)
            {
                _wasMovementControllerEnabled = playerRoot.MovementController.enabled;
                playerRoot.MovementController.enabled = false;
            }

            if (playerRoot?.AimController != null)
            {
                _wasAimControllerEnabled = playerRoot.AimController.enabled;
                playerRoot.AimController.enabled = false;
            }

            if (playerRoot?.PotCarryController != null)
            {
                _wasPotCarryControllerEnabled = playerRoot.PotCarryController.enabled;
                playerRoot.PotCarryController.enabled = false;
            }

            if (playerRoot?.WeaponController != null)
            {
                _wasWeaponControllerEnabled = playerRoot.WeaponController.enabled;
                playerRoot.WeaponController.enabled = false;
            }

            if (portalController != null)
            {
                _wasPortalControllerEnabled = portalController.enabled;
                portalController.enabled = false;
            }
        }

        private void RestorePlayerGameplay()
        {
            Time.timeScale = _previousTimeScale;

            if (playerRoot?.MovementController != null)
            {
                playerRoot.MovementController.enabled = _wasMovementControllerEnabled;
            }

            if (playerRoot?.AimController != null)
            {
                playerRoot.AimController.enabled = _wasAimControllerEnabled;
            }

            if (playerRoot?.PotCarryController != null)
            {
                playerRoot.PotCarryController.enabled = _wasPotCarryControllerEnabled;
            }

            if (playerRoot?.WeaponController != null)
            {
                playerRoot.WeaponController.enabled = _wasWeaponControllerEnabled;
            }

            if (portalController != null)
            {
                portalController.enabled = _wasPortalControllerEnabled;
            }
        }

        private void SetShopPresentation(bool shopVisible, bool promptVisible)
        {
            if (panelPresenter == null)
            {
                return;
            }

            panelPresenter.SetShopVisible(shopVisible);
            panelPresenter.SetInteractionPromptVisible(promptVisible);
        }
    }
}