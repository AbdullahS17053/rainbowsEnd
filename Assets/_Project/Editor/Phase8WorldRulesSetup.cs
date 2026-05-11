using System.Collections.Generic;
using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Economy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase8WorldRulesSetup
    {
        private static readonly Color DrainAlertColor = new Color(0.96f, 0.28f, 0.18f, 0.95f);

        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PotPrefabPath = "Assets/_Project/Prefabs/PF_Pot.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 8 World Rules Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);
            if (configRegistry?.PotConfig == null || configRegistry.EconomyConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry, PotConfig, or EconomyConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            if (!File.Exists(GetAbsoluteAssetPath(PotPrefabPath)))
            {
                Debug.LogError($"Pot prefab missing at {PotPrefabPath}. Run Phase 6 pot setup first.");
                return;
            }

            CreateOrUpdatePotPrefabWorldRules(placeholderSprite);
            CreateOrUpdateSceneWorldRules(configRegistry.PotConfig, configRegistry.EconomyConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 8 world rules setup completed.");
        }

        private static void CreateOrUpdatePotPrefabWorldRules(Sprite placeholderSprite)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PotPrefabPath);
            try
            {
                GameObject drainAlertRoot = GetOrCreateChild(prefabRoot.transform, "DrainAlert");
                drainAlertRoot.transform.localPosition = new Vector3(0f, 0.74f, 0f);
                drainAlertRoot.transform.localScale = new Vector3(0.18f, 0.18f, 1f);

                SpriteRenderer alertRenderer = GetOrAddComponent<SpriteRenderer>(drainAlertRoot);
                alertRenderer.sprite = placeholderSprite;
                alertRenderer.color = DrainAlertColor;
                alertRenderer.sortingOrder = 18;
                alertRenderer.enabled = false;

                PotDrainFeedbackPresenter drainFeedbackPresenter = GetOrAddComponent<PotDrainFeedbackPresenter>(drainAlertRoot);
                drainFeedbackPresenter.Configure(alertRenderer);
                drainFeedbackPresenter.SetVisible(false);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PotPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateOrUpdateSceneWorldRules(PotConfig potConfig, EconomyConfig economyConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject spawnPointsRoot = FindRoot(scene, "SpawnPoints");

            if (gameplayRoot == null || spawnPointsRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot or SpawnPoints. Run Phase 2 setup first.");
                return;
            }

            EconomyState economyState = GetOrAddComponent<EconomyState>(GetOrCreateChild(gameplayRoot.transform, "EconomyState"));
            economyState.Configure(economyConfig);

            GetOrAddComponent<PotRegistry>(GetOrCreateChild(gameplayRoot.transform, "PotRegistry"));
            PlaceOrUpdateScenePots(scene, gameplayRoot.transform, spawnPointsRoot.transform, potConfig);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PlaceOrUpdateScenePots(Scene scene, Transform gameplayRoot, Transform spawnPointsRoot, PotConfig potConfig)
        {
            List<SpawnPointMarker> potSpawnMarkers = FindPotSpawnMarkers(spawnPointsRoot);
            if (potSpawnMarkers.Count == 0)
            {
                Debug.LogError("Pot spawn markers were not found in MainArena. Run Phase 2 setup first.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PotPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"Pot prefab missing at {PotPrefabPath}.");
                return;
            }

            PotController[] existingPots = Object.FindObjectsByType<PotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Dictionary<string, PotController> namedPots = new Dictionary<string, PotController>();
            PotController legacyPot = null;
            for (int index = 0; index < existingPots.Length; index++)
            {
                PotController existingPot = existingPots[index];
                if (existingPot == null)
                {
                    continue;
                }

                if (existingPot.name == "Pot")
                {
                    legacyPot ??= existingPot;
                    continue;
                }

                namedPots[existingPot.name] = existingPot;
            }

            HashSet<int> assignedIds = new HashSet<int>();
            for (int index = 0; index < potSpawnMarkers.Count; index++)
            {
                SpawnPointMarker marker = potSpawnMarkers[index];
                string potName = $"Pot_{Mathf.Max(1, marker.Order):00}";

                PotController potController = namedPots.TryGetValue(potName, out PotController namedPot)
                    ? namedPot
                    : ResolveLegacyPotForMarker(marker, legacyPot, assignedIds);

                if (potController == null)
                {
                    GameObject potInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
                    potController = potInstance.GetComponent<PotController>();
                }

                assignedIds.Add(potController.GetInstanceID());

                GameObject potObject = potController.gameObject;
                potObject.name = potName;
                potObject.transform.SetParent(gameplayRoot, true);
                potObject.transform.position = marker.transform.position;
                potObject.transform.rotation = Quaternion.identity;

                potController.Configure(
                    potConfig,
                    potObject.GetComponent<Rigidbody2D>(),
                    potObject.GetComponent<Collider2D>(),
                    potObject.GetComponent<Health>(),
                    potObject.GetComponentInChildren<PotTrailCaptureSensor>(true),
                    potObject.GetComponentInChildren<SpriteRenderer>());
            }

            for (int index = 0; index < existingPots.Length; index++)
            {
                PotController existingPot = existingPots[index];
                if (existingPot == null || assignedIds.Contains(existingPot.GetInstanceID()))
                {
                    continue;
                }

                Object.DestroyImmediate(existingPot.gameObject);
            }
        }

        private static PotController ResolveLegacyPotForMarker(SpawnPointMarker marker, PotController legacyPot, HashSet<int> assignedIds)
        {
            if (legacyPot == null || assignedIds.Contains(legacyPot.GetInstanceID()))
            {
                return null;
            }

            return marker.Order <= 1 ? legacyPot : null;
        }

        private static List<SpawnPointMarker> FindPotSpawnMarkers(Transform root)
        {
            SpawnPointMarker[] markers = root.GetComponentsInChildren<SpawnPointMarker>(true);
            List<SpawnPointMarker> result = new List<SpawnPointMarker>();
            for (int index = 0; index < markers.Length; index++)
            {
                if (markers[index].Kind == SpawnPointKind.Pot)
                {
                    result.Add(markers[index]);
                }
            }

            result.Sort((left, right) => left.Order.CompareTo(right.Order));
            return result;
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