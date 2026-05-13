using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Economy;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase7EnemySetup
    {
        private static readonly Color TravellerDrainColor = new Color(0.95f, 0.33f, 0.24f, 1f);

        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string TravellerPrefabPath = "Assets/_Project/Prefabs/PF_Traveller.prefab";
        private const string GoldPickupPrefabPath = "Assets/_Project/Prefabs/PF_GoldPickup.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 7 Enemy Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);
            if (configRegistry?.TravellerConfig == null || configRegistry.WaveConfig == null || configRegistry.EconomyConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry, TravellerConfig, WaveConfig, or EconomyConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            GameObject goldPickupPrefab = CreateOrUpdateGoldPickupPrefab(placeholderSprite);
            GameObject travellerPrefab = CreateOrUpdateTravellerPrefab(placeholderSprite, configRegistry.TravellerConfig);
            CreateOrUpdateSceneObjects(travellerPrefab, goldPickupPrefab, configRegistry.TravellerConfig, configRegistry.WaveConfig, configRegistry.EconomyConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 7 enemy setup completed.");
        }

        private static GameObject CreateOrUpdateGoldPickupPrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(GoldPickupPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(GoldPickupPrefabPath)
                : new GameObject("PF_GoldPickup");

            try
            {
                prefabRoot.name = "PF_GoldPickup";

                CircleCollider2D collider2D = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                collider2D.isTrigger = true;
                collider2D.radius = 0.26f;

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(1f, 0.84f, 0.22f, 1f);
                spriteRenderer.sortingOrder = 20;
                prefabRoot.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

                GetOrAddComponent<GoldPickup>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, GoldPickupPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(GoldPickupPrefabPath);
        }

        private static GameObject CreateOrUpdateTravellerPrefab(Sprite placeholderSprite, EnemyConfig travellerConfig)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(TravellerPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(TravellerPrefabPath)
                : new GameObject("PF_Traveller");

            try
            {
                prefabRoot.name = "PF_Traveller";

                Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(prefabRoot);
                CircleCollider2D bodyCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                bodyCollider.radius = 0.32f;
                bodyCollider.offset = Vector2.zero;
                bodyCollider.isTrigger = true;

                Health health = GetOrAddComponent<Health>(prefabRoot);
                PotDrain potDrain = GetOrAddComponent<PotDrain>(prefabRoot);
                TravellerController travellerController = GetOrAddComponent<TravellerController>(prefabRoot);

                GameObject visualRoot = GetOrCreateChild(prefabRoot.transform, "VisualRoot");
                SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualRoot);
                visualRenderer.sprite = placeholderSprite;
                visualRenderer.color = new Color(0.88f, 0.45f, 0.24f, 1f);
                visualRenderer.sortingOrder = 15;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localScale = new Vector3(0.72f, 0.88f, 1f);

                travellerController.Configure(travellerConfig, body, bodyCollider, health, potDrain, visualRenderer);
                travellerController.ConfigureVisualFeedback(TravellerDrainColor);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, TravellerPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(TravellerPrefabPath);
        }

        private static void CreateOrUpdateSceneObjects(
            GameObject travellerPrefab,
            GameObject goldPickupPrefab,
            EnemyConfig travellerConfig,
            WaveConfig waveConfig,
            EconomyConfig economyConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject spawnPointsRoot = FindRoot(scene, "SpawnPoints");

            if (gameplayRoot == null || spawnPointsRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot or SpawnPoints. Run Phase 2 setup first.");
                return;
            }

            GameObject economyStateObject = GetOrCreateChild(gameplayRoot.transform, "EconomyState");
            EconomyState economyState = GetOrAddComponent<EconomyState>(economyStateObject);
            economyState.Configure(economyConfig);

            GameObject enemySystemsRoot = GetOrCreateChild(gameplayRoot.transform, "EnemySystems");

            GameObject travellerPoolObject = GetOrCreateChild(enemySystemsRoot.transform, "TravellerPool");
            PrefabPool travellerPool = GetOrAddComponent<PrefabPool>(travellerPoolObject);
            travellerPool.Configure(travellerPrefab, Mathf.Max(4, waveConfig.MaxConcurrentEnemies), travellerPoolObject.transform);

            GameObject goldPickupPoolObject = GetOrCreateChild(enemySystemsRoot.transform, "GoldPickupPool");
            PrefabPool goldPickupPool = GetOrAddComponent<PrefabPool>(goldPickupPoolObject);
            goldPickupPool.Configure(goldPickupPrefab, Mathf.Max(4, waveConfig.MaxConcurrentEnemies), goldPickupPoolObject.transform);

            GameObject enemySpawnerObject = GetOrCreateChild(enemySystemsRoot.transform, "EnemySpawner");
            EnemySpawner enemySpawner = GetOrAddComponent<EnemySpawner>(enemySpawnerObject);
            enemySpawner.Configure(travellerConfig, waveConfig, economyConfig, travellerPool, goldPickupPool, spawnPointsRoot.transform, CreateDefaultGoldDropOptions(economyConfig));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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

        private static GoldDropOption[] CreateDefaultGoldDropOptions(EconomyConfig economyConfig)
        {
            if (economyConfig == null)
            {
                return new GoldDropOption[0];
            }

            int minGold = Mathf.Max(0, economyConfig.EnemyDropGoldMin);
            int maxGold = Mathf.Max(minGold, economyConfig.EnemyDropGoldMax);
            GoldDropOption[] options = new GoldDropOption[maxGold - minGold + 1];
            for (int amount = minGold; amount <= maxGold; amount++)
            {
                options[amount - minGold] = new GoldDropOption(amount);
            }

            return options;
        }
    }
}