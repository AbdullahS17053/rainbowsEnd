using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Economy;
using RainbowsEnd.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase6PotSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PotPrefabPath = "Assets/_Project/Prefabs/PF_Pot.prefab";
        private const string TrailSegmentPrefabPath = "Assets/_Project/Prefabs/PF_RainbowTrailSegment.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 6 Pot Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);

            if (configRegistry?.PotConfig == null || configRegistry.EconomyConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry, PotConfig, or EconomyConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            if (!EnsureTextMeshProEssentials())
            {
                return;
            }

            EnsureTrailSegmentCaptureCollider();
            CreateOrUpdatePotPrefab(placeholderSprite, configRegistry.PotConfig);
            CreateOrUpdateScenePotAndEconomy(configRegistry.PotConfig, configRegistry.EconomyConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 6 pot setup completed.");
        }

        private static void EnsureTrailSegmentCaptureCollider()
        {
            if (!File.Exists(GetAbsoluteAssetPath(TrailSegmentPrefabPath)))
            {
                Debug.LogError($"Trail segment prefab missing at {TrailSegmentPrefabPath}. Run Phase 5 unicorn setup first.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(TrailSegmentPrefabPath);
            try
            {
                CircleCollider2D collider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                collider.isTrigger = true;
                collider.radius = 0.16f;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, TrailSegmentPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateOrUpdatePotPrefab(Sprite placeholderSprite, PotConfig potConfig)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(PotPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(PotPrefabPath)
                : new GameObject("PF_Pot");

            try
            {
                prefabRoot.name = "PF_Pot";

                Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(prefabRoot);
                CircleCollider2D bodyCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                bodyCollider.radius = 0.42f;
                bodyCollider.offset = Vector2.zero;
                bodyCollider.isTrigger = false;

                Health health = GetOrAddComponent<Health>(prefabRoot);
                PotController potController = GetOrAddComponent<PotController>(prefabRoot);

                GameObject visualRoot = GetOrCreateChild(prefabRoot.transform, "VisualRoot");
                SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualRoot);
                visualRenderer.sprite = placeholderSprite;
                visualRenderer.color = new Color(0.74f, 0.49f, 0.18f, 1f);
                visualRenderer.sortingOrder = 11;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                GameObject captureSensorRoot = GetOrCreateChild(prefabRoot.transform, "TrailCaptureSensor");
                captureSensorRoot.transform.localPosition = Vector3.zero;
                PotTrailCaptureSensor captureSensor = GetOrAddComponent<PotTrailCaptureSensor>(captureSensorRoot);
                CircleCollider2D captureSensorCollider = GetOrAddComponent<CircleCollider2D>(captureSensorRoot);
                captureSensorCollider.isTrigger = true;

                potController.Configure(potConfig, body, bodyCollider, health, captureSensor, visualRenderer);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PotPrefabPath);
            }
            finally
            {
                if (prefabExists)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                else
                {
                    Object.DestroyImmediate(prefabRoot);
                }
            }
        }

        private static void CreateOrUpdateScenePotAndEconomy(PotConfig potConfig, EconomyConfig economyConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject spawnPointsRoot = FindRoot(scene, "SpawnPoints");
            GameObject uiRoot = FindRoot(scene, "UI");

            if (gameplayRoot == null || spawnPointsRoot == null || uiRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot, SpawnPoints, or UI. Run Phase 2 setup first.");
                return;
            }

            EconomyState economyState = GetOrAddComponent<EconomyState>(GetOrCreateChild(gameplayRoot.transform, "EconomyState"));
            PotRegistry potRegistry = GetOrAddComponent<PotRegistry>(GetOrCreateChild(gameplayRoot.transform, "PotRegistry"));
            economyState.Configure(economyConfig);

            EnsureHudBinding(uiRoot.transform, potRegistry);
            PlaceOrUpdateScenePot(scene, gameplayRoot.transform, spawnPointsRoot.transform, potConfig);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureHudBinding(Transform uiRoot, PotRegistry potRegistry)
        {
            Transform hudRoot = uiRoot.Find("HudRoot");
            if (hudRoot == null)
            {
                Debug.LogError("HudRoot was not found under UI. Run Phase 2 setup first.");
                return;
            }

            GameObject goldCounterPlaceholder = GetOrCreateChild(hudRoot, "GoldCounterPlaceholder");
            RectTransform goldCounterTransform = GetOrAddComponent<RectTransform>(goldCounterPlaceholder);
            goldCounterTransform.anchorMin = new Vector2(0f, 1f);
            goldCounterTransform.anchorMax = new Vector2(0f, 1f);
            goldCounterTransform.pivot = new Vector2(0f, 1f);
            goldCounterTransform.anchoredPosition = new Vector2(24f, -24f);
            goldCounterTransform.sizeDelta = new Vector2(240f, 64f);

            RemoveLegacyText(goldCounterPlaceholder);

            TextMeshProUGUI goldText = GetOrAddComponent<TextMeshProUGUI>(goldCounterPlaceholder);
            goldText.fontSize = 28f;
            goldText.alignment = TextAlignmentOptions.MidlineLeft;
            goldText.color = new Color(1f, 0.94f, 0.58f, 1f);
            goldText.raycastTarget = false;
            goldText.enableWordWrapping = false;
            goldText.text = "Gold: 0";

            if (TMP_Settings.defaultFontAsset != null)
            {
                goldText.font = TMP_Settings.defaultFontAsset;
            }

            EconomyHudPresenter presenter = GetOrAddComponent<EconomyHudPresenter>(goldCounterPlaceholder);
            presenter.Configure(potRegistry, goldText);
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
                Debug.LogError("TextMesh Pro essential resources could not be imported. Open Window > Text Mesh Pro > Import TMP Essential Resources, then rerun Phase 6 Pot Setup.");
                return false;
            }

            return true;
        }

        private static void RemoveLegacyText(GameObject target)
        {
            Component legacyText = target.GetComponent("Text");
            if (legacyText != null)
            {
                Object.DestroyImmediate(legacyText);
            }
        }

        private static void PlaceOrUpdateScenePot(Scene scene, Transform gameplayRoot, Transform spawnPointsRoot, PotConfig potConfig)
        {
            SpawnPointMarker potSpawnMarker = FindFirstPotSpawnMarker(spawnPointsRoot);
            if (potSpawnMarker == null)
            {
                Debug.LogError("A pot spawn marker was not found in MainArena. Run Phase 2 setup first.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PotPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"Pot prefab missing at {PotPrefabPath}.");
                return;
            }

            PotController existingPot = Object.FindFirstObjectByType<PotController>(FindObjectsInactive.Include);
            GameObject potInstance = existingPot != null
                ? existingPot.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);

            potInstance.name = "Pot";
            potInstance.transform.SetParent(gameplayRoot, true);
            potInstance.transform.position = potSpawnMarker.transform.position;
            potInstance.transform.rotation = Quaternion.identity;

            PotController potController = GetOrAddComponent<PotController>(potInstance);
            potController.Configure(
                potConfig,
                potInstance.GetComponent<Rigidbody2D>(),
                potInstance.GetComponent<Collider2D>(),
                potInstance.GetComponent<Health>(),
                potInstance.GetComponentInChildren<PotTrailCaptureSensor>(true),
                potInstance.GetComponentInChildren<SpriteRenderer>());
        }

        private static SpawnPointMarker FindFirstPotSpawnMarker(Transform root)
        {
            SpawnPointMarker[] markers = root.GetComponentsInChildren<SpawnPointMarker>(true);
            SpawnPointMarker bestMarker = null;
            for (int index = 0; index < markers.Length; index++)
            {
                if (markers[index].Kind != SpawnPointKind.Pot)
                {
                    continue;
                }

                if (bestMarker == null || markers[index].Order < bestMarker.Order)
                {
                    bestMarker = markers[index];
                }
            }

            return bestMarker;
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

        private static Sprite EnsurePlaceholderSprite()
        {
            string absolutePath = GetAbsoluteAssetPath(PlaceholderSpritePath);
            if (!File.Exists(absolutePath))
            {
                Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[32 * 32];
                for (int index = 0; index < pixels.Length; index++)
                {
                    pixels[index] = new Color32(255, 255, 255, 255);
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(PlaceholderSpritePath) is TextureImporter textureImporter)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.spritePixelsPerUnit = 32f;
                textureImporter.alphaIsTransparency = true;
                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.wrapMode = TextureWrapMode.Clamp;
                textureImporter.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            string projectRootPath = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.Combine(projectRootPath, assetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
        }
    }
}