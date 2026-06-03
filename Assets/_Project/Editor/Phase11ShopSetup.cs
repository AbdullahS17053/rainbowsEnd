using System.IO;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Shop;
using RainbowsEnd.Gameplay.Spells;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Economy;
using RainbowsEnd.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RainbowsEnd.Editor
{
    public static class Phase11ShopSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string ShopConfigPath = "Assets/_Project/Settings/Balance/SO_ShopConfig.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 11 Shop Setup")]
        public static void Run()
        {
            if (!EnsureTextMeshProEssentials())
            {
                return;
            }

            ShopConfig shopConfig = EnsureShopConfigAsset();
            if (shopConfig == null)
            {
                return;
            }

            CreateOrUpdatePlayerPrefab();
            CreateOrUpdateSceneObjects(shopConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 11 shop setup completed.");
        }

        private static ShopConfig EnsureShopConfigAsset()
        {
            EnsureAssetDirectory(ShopConfigPath);

            ShopConfig shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
            if (shopConfig == null)
            {
                shopConfig = ScriptableObject.CreateInstance<ShopConfig>();
                AssetDatabase.CreateAsset(shopConfig, ShopConfigPath);
            }

            ApplyShopDefaults(shopConfig);
            return shopConfig;
        }

        private static void ApplyShopDefaults(ShopConfig shopConfig)
        {
            SerializedObject serializedObject = new SerializedObject(shopConfig);
            SetString(serializedObject.FindProperty("shopTitle"), "Fae Tree Shop");
            SetFloat(serializedObject.FindProperty("interactionRadius"), 1.75f);

            SerializedProperty upgradesProperty = serializedObject.FindProperty("upgrades");
            if (upgradesProperty != null)
            {
                upgradesProperty.arraySize = 3;
                ConfigureUpgrade(
                    upgradesProperty.GetArrayElementAtIndex(0),
                    "Damage Up",
                    "Increase weapon damage by 25% per purchase.",
                    ShopUpgradeEffectType.WeaponDamage,
                    20,
                    3,
                    0.25f);
                ConfigureUpgrade(
                    upgradesProperty.GetArrayElementAtIndex(1),
                    "Fire Rate Up",
                    "Reduce weapon cooldown by 10% per purchase.",
                    ShopUpgradeEffectType.WeaponFireRate,
                    25,
                    3,
                    0.1f);
                ConfigureUpgrade(
                    upgradesProperty.GetArrayElementAtIndex(2),
                    "Accuracy Up",
                    "Reduce weapon spread by 15% per purchase.",
                    ShopUpgradeEffectType.WeaponAccuracy,
                    15,
                    3,
                    0.15f);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shopConfig);
        }

        private static void ConfigureUpgrade(
            SerializedProperty upgradeProperty,
            string displayName,
            string description,
            ShopUpgradeEffectType effectType,
            int cost,
            int maxPurchaseCount,
            float valuePerPurchase)
        {
            if (upgradeProperty == null)
            {
                return;
            }

            SetString(upgradeProperty.FindPropertyRelative("displayName"), displayName);
            SetString(upgradeProperty.FindPropertyRelative("description"), description);
            SetEnum(upgradeProperty.FindPropertyRelative("effectType"), (int)effectType);
            SetInt(upgradeProperty.FindPropertyRelative("cost"), cost);
            SetInt(upgradeProperty.FindPropertyRelative("maxPurchaseCount"), maxPurchaseCount);
            SetFloat(upgradeProperty.FindPropertyRelative("valuePerPurchase"), valuePerPurchase);
        }

        private static void CreateOrUpdatePlayerPrefab()
        {
            if (!File.Exists(GetAbsoluteAssetPath(PlayerPrefabPath)))
            {
                Debug.LogError($"Player prefab not found at {PlayerPrefabPath}. Run Phase 3 player setup first.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                GetOrAddComponent<PlayerUpgradeRuntime>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateOrUpdateSceneObjects(ShopConfig shopConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject uiRoot = FindRoot(scene, "UI");
            GameObject sceneContextRoot = FindRoot(scene, "SceneContext");

            if (gameplayRoot == null || uiRoot == null || sceneContextRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot, UI, or SceneContext. Run earlier setup phases first.");
                return;
            }

            EnsureEventSystem(scene);

            ArenaSceneContext sceneContext = GetOrAddComponent<ArenaSceneContext>(sceneContextRoot);
            FaeTreeAnchor faeTreeAnchor = sceneContext.FaeTreeAnchor != null
                ? sceneContext.FaeTreeAnchor
                : Object.FindFirstObjectByType<FaeTreeAnchor>(FindObjectsInactive.Include);
            if (faeTreeAnchor == null)
            {
                Debug.LogError("FaeTreeAnchor is missing from MainArena. Run Phase 2 arena setup first.");
                return;
            }

            EconomyState economyState = Object.FindFirstObjectByType<EconomyState>(FindObjectsInactive.Include);
            if (economyState == null)
            {
                Debug.LogError("EconomyState is missing from MainArena. Run Phase 6 pot setup first.");
                return;
            }

            PlayerRoot playerRoot = Object.FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
            if (playerRoot == null)
            {
                Debug.LogError("Player instance is missing from MainArena. Run earlier player setup first.");
                return;
            }

            PlayerUpgradeRuntime upgradeRuntime = GetOrAddComponent<PlayerUpgradeRuntime>(playerRoot.gameObject);
            PlayerPortalController portalController = playerRoot.GetComponent<PlayerPortalController>();

            ShopPanelPresenter panelPresenter = EnsureShopUi(uiRoot.transform);
            if (panelPresenter == null)
            {
                return;
            }

            GameObject shopSystemsRoot = GetOrCreateChild(gameplayRoot.transform, "ShopSystems");
            GameObject shopControllerObject = GetOrCreateChild(shopSystemsRoot.transform, "FaeTreeShop");
            ShopController shopController = GetOrAddComponent<ShopController>(shopControllerObject);
            shopController.Configure(shopConfig, sceneContext, faeTreeAnchor, playerRoot, economyState, portalController, panelPresenter);

            panelPresenter.Configure(shopController);
            panelPresenter.Refresh(shopConfig, economyState, upgradeRuntime);
            panelPresenter.SetShopVisible(false);
            panelPresenter.SetInteractionPromptVisible(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static ShopPanelPresenter EnsureShopUi(Transform uiRoot)
        {
            Transform hudRoot = uiRoot.Find("HudRoot");
            if (hudRoot == null)
            {
                Debug.LogError("HudRoot was not found under UI. Run Phase 2 setup first.");
                return null;
            }

            GameObject promptObject = GetOrCreateChild(hudRoot, "ShopPrompt");
            RectTransform promptTransform = GetOrAddComponent<RectTransform>(promptObject);
            promptTransform.anchorMin = new Vector2(0.5f, 0f);
            promptTransform.anchorMax = new Vector2(0.5f, 0f);
            promptTransform.pivot = new Vector2(0.5f, 0f);
            promptTransform.anchoredPosition = new Vector2(0f, 24f);
            promptTransform.sizeDelta = new Vector2(640f, 44f);
            TextMeshProUGUI promptText = ConfigureText(promptObject, 22f, TextAlignmentOptions.Center, new Color(0.89f, 0.95f, 0.76f, 1f), "Press Interact near the Fae Tree to open the shop.");
            promptText.enableWordWrapping = false;

            GameObject panelObject = GetOrCreateChild(hudRoot, "ShopPanel");
            RectTransform panelTransform = GetOrAddComponent<RectTransform>(panelObject);
            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0.5f);
            panelTransform.anchoredPosition = Vector2.zero;
            panelTransform.sizeDelta = new Vector2(720f, 520f);
            Image panelImage = GetOrAddComponent<Image>(panelObject);
            panelImage.color = new Color(0.07f, 0.1f, 0.07f, 0.96f);

            TextMeshProUGUI titleText = CreatePanelText(panelObject.transform, "TitleText", new Vector2(0f, -28f), new Vector2(640f, 52f), 34f, TextAlignmentOptions.Center, new Color(0.96f, 0.98f, 0.84f, 1f), "Fae Tree Shop");
            TextMeshProUGUI goldText = CreatePanelText(panelObject.transform, "GoldText", new Vector2(0f, -74f), new Vector2(640f, 38f), 24f, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.55f, 1f), "Gold: 0");
            TextMeshProUGUI instructionsText = CreatePanelText(panelObject.transform, "InstructionsText", new Vector2(0f, -112f), new Vector2(640f, 48f), 20f, TextAlignmentOptions.Center, new Color(0.82f, 0.88f, 0.82f, 1f), "Press 1-9 or click an upgrade. Press Interact again to close.");

            ShopUpgradeButtonView[] buttonViews = new ShopUpgradeButtonView[3];
            CreateUpgradeButton(panelObject.transform, "UpgradeButton01", new Vector2(0f, -168f), out buttonViews[0]);
            CreateUpgradeButton(panelObject.transform, "UpgradeButton02", new Vector2(0f, -280f), out buttonViews[1]);
            CreateUpgradeButton(panelObject.transform, "UpgradeButton03", new Vector2(0f, -392f), out buttonViews[2]);

            ShopPanelPresenter panelPresenter = GetOrAddComponent<ShopPanelPresenter>(panelObject);
            panelPresenter.ConfigureView(panelObject, titleText, goldText, instructionsText, promptText, buttonViews);

            panelObject.SetActive(false);
            promptObject.SetActive(false);
            return panelPresenter;
        }

        private static void CreateUpgradeButton(Transform parent, string name, Vector2 anchoredPosition, out ShopUpgradeButtonView buttonView)
        {
            GameObject buttonObject = GetOrCreateChild(parent, name);
            RectTransform buttonTransform = GetOrAddComponent<RectTransform>(buttonObject);
            buttonTransform.anchorMin = new Vector2(0.5f, 1f);
            buttonTransform.anchorMax = new Vector2(0.5f, 1f);
            buttonTransform.pivot = new Vector2(0.5f, 1f);
            buttonTransform.anchoredPosition = anchoredPosition;
            buttonTransform.sizeDelta = new Vector2(640f, 96f);

            Image backgroundImage = GetOrAddComponent<Image>(buttonObject);
            backgroundImage.color = new Color(0.17f, 0.22f, 0.17f, 0.96f);

            Button button = GetOrAddComponent<Button>(buttonObject);
            button.targetGraphic = backgroundImage;

            TextMeshProUGUI titleText = CreateChildText(buttonObject.transform, "TitleText", new Vector2(0f, -10f), new Vector2(580f, 28f), 24f, TextAlignmentOptions.TopLeft, new Color(0.95f, 0.98f, 0.89f, 1f), "1. Upgrade");
            titleText.enableWordWrapping = false;
            TextMeshProUGUI detailText = CreateChildText(buttonObject.transform, "DetailText", new Vector2(0f, -42f), new Vector2(580f, 44f), 18f, TextAlignmentOptions.TopLeft, new Color(0.82f, 0.87f, 0.82f, 1f), "Description\nCost: 0   Level: 0/0");

            buttonView = GetOrAddComponent<ShopUpgradeButtonView>(buttonObject);
            buttonView.ConfigureView(button, backgroundImage, titleText, detailText);
        }

        private static TextMeshProUGUI CreatePanelText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            string text)
        {
            GameObject textObject = GetOrCreateChild(parent, name);
            RectTransform textTransform = GetOrAddComponent<RectTransform>(textObject);
            textTransform.anchorMin = new Vector2(0.5f, 1f);
            textTransform.anchorMax = new Vector2(0.5f, 1f);
            textTransform.pivot = new Vector2(0.5f, 1f);
            textTransform.anchoredPosition = anchoredPosition;
            textTransform.sizeDelta = sizeDelta;
            return ConfigureText(textObject, fontSize, alignment, color, text);
        }

        private static TextMeshProUGUI CreateChildText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            string text)
        {
            GameObject textObject = GetOrCreateChild(parent, name);
            RectTransform textTransform = GetOrAddComponent<RectTransform>(textObject);
            textTransform.anchorMin = new Vector2(0.5f, 1f);
            textTransform.anchorMax = new Vector2(0.5f, 1f);
            textTransform.pivot = new Vector2(0.5f, 1f);
            textTransform.anchoredPosition = anchoredPosition;
            textTransform.sizeDelta = sizeDelta;
            return ConfigureText(textObject, fontSize, alignment, color, text);
        }

        private static TextMeshProUGUI ConfigureText(GameObject target, float fontSize, TextAlignmentOptions alignment, Color color, string text)
        {
            RemoveLegacyText(target);

            TextMeshProUGUI textLabel = GetOrAddComponent<TextMeshProUGUI>(target);
            textLabel.fontSize = fontSize;
            textLabel.alignment = alignment;
            textLabel.color = color;
            textLabel.raycastTarget = false;
            textLabel.enableWordWrapping = true;
            textLabel.text = text;

            if (TMP_Settings.defaultFontAsset != null)
            {
                textLabel.font = TMP_Settings.defaultFontAsset;
            }

            return textLabel;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            GameObject eventSystemObject = FindRoot(scene, "EventSystem");
            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            }

            GetOrAddComponent<EventSystem>(eventSystemObject);
            InputSystemUIInputModule inputModule = GetOrAddComponent<InputSystemUIInputModule>(eventSystemObject);
            inputModule.AssignDefaultActions();

            StandaloneInputModule standaloneInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                Object.DestroyImmediate(standaloneInputModule);
            }
        }

        private static bool EnsureTextMeshProEssentials()
        {
            string[] settingsAssets = AssetDatabase.FindAssets("t:TMP_Settings");
            if (settingsAssets.Length == 0)
            {
                TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
                AssetDatabase.Refresh();
                settingsAssets = AssetDatabase.FindAssets("t:TMP_Settings");
            }

            if (settingsAssets.Length == 0)
            {
                Debug.LogError("TextMesh Pro essential resources could not be imported. Open Window > Text Mesh Pro > Import TMP Essential Resources, then rerun Phase 11 Shop Setup.");
                return false;
            }

            return true;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directoryPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directoryPath) || AssetDatabase.IsValidFolder(directoryPath))
            {
                return;
            }

            string[] segments = directoryPath.Split('/');
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }

        private static void RemoveLegacyText(GameObject target)
        {
            Component legacyText = target.GetComponent("Text");
            if (legacyText != null)
            {
                Object.DestroyImmediate(legacyText);
            }
        }

        private static void SetString(SerializedProperty property, string value)
        {
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetFloat(SerializedProperty property, float value)
        {
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedProperty property, int value)
        {
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetEnum(SerializedProperty property, int value)
        {
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == name)
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            string projectRootPath = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.Combine(projectRootPath, assetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
        }
    }
}