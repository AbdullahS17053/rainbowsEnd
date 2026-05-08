using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase4CombatSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string ArrowPrefabPath = "Assets/_Project/Prefabs/PF_BowArrow.prefab";
        private const string ImpactPrefabPath = "Assets/_Project/Prefabs/PF_BowImpact.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 4 Combat Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);

            if (configRegistry?.DefaultWeaponConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry or DefaultWeaponConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            GameObject arrowPrefab = CreateOrUpdateArrowPrefab(placeholderSprite);
            GameObject impactPrefab = CreateOrUpdateImpactPrefab(placeholderSprite);
            CreateOrUpdatePlayerCombatPrefab(configRegistry.DefaultWeaponConfig);
            CreateOrUpdateCombatSceneObjects(placeholderSprite, arrowPrefab, impactPrefab, configRegistry.DefaultWeaponConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 4 combat setup completed.");
        }

        private static GameObject CreateOrUpdateArrowPrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(ArrowPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(ArrowPrefabPath)
                : new GameObject("PF_BowArrow");

            try
            {
                prefabRoot.name = "PF_BowArrow";

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.96f, 0.78f, 0.22f, 1f);
                spriteRenderer.sortingOrder = 24;
                prefabRoot.transform.localScale = new Vector3(0.14f, 0.7f, 1f);

                GetOrAddComponent<ArrowProjectile>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ArrowPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
        }

        private static GameObject CreateOrUpdateImpactPrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(ImpactPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(ImpactPrefabPath)
                : new GameObject("PF_BowImpact");

            try
            {
                prefabRoot.name = "PF_BowImpact";

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(1f, 0.86f, 0.3f, 0.95f);
                spriteRenderer.sortingOrder = 25;
                prefabRoot.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

                GetOrAddComponent<CombatImpactEffect>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ImpactPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(ImpactPrefabPath);
        }

        private static void CreateOrUpdatePlayerCombatPrefab(WeaponConfig weaponConfig)
        {
            if (!File.Exists(GetAbsoluteAssetPath(PlayerPrefabPath)))
            {
                Debug.LogError($"Player prefab not found at {PlayerPrefabPath}. Run Phase 3 player setup first.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                PlayerRoot playerRoot = GetOrAddComponent<PlayerRoot>(prefabRoot);
                PlayerInputReader inputReader = GetOrAddComponent<PlayerInputReader>(prefabRoot);
                PlayerMovementController movementController = GetOrAddComponent<PlayerMovementController>(prefabRoot);
                PlayerAimController aimController = GetOrAddComponent<PlayerAimController>(prefabRoot);
                ProjectileWeaponLauncher projectileLauncher = GetOrAddComponent<ProjectileWeaponLauncher>(prefabRoot);
                WeaponImpactFeedbackSpawner impactSpawner = GetOrAddComponent<WeaponImpactFeedbackSpawner>(prefabRoot);
                WeaponController weaponController = GetOrAddComponent<WeaponController>(prefabRoot);

                Component legacyHitscanResolver = prefabRoot.GetComponent("HitscanWeaponResolver");
                if (legacyHitscanResolver != null)
                {
                    Object.DestroyImmediate(legacyHitscanResolver);
                }

                weaponController.Configure(playerRoot, inputReader, aimController, movementController, projectileLauncher, impactSpawner, weaponConfig);
                playerRoot.ConfigureCombat(weaponController);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateOrUpdateCombatSceneObjects(Sprite placeholderSprite, GameObject arrowPrefab, GameObject impactPrefab, WeaponConfig weaponConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject sceneContextRoot = FindRoot(scene, "SceneContext");
            GameObject spawnPointsRoot = FindRoot(scene, "SpawnPoints");

            if (gameplayRoot == null || sceneContextRoot == null || spawnPointsRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot, SceneContext, or SpawnPoints. Run Phase 2 setup first.");
                return;
            }

            ArenaSceneContext sceneContext = GetOrAddComponent<ArenaSceneContext>(sceneContextRoot);
            GameObject effectsRoot = GetOrCreateRoot(scene, "Effects");
            GameObject arrowPoolObject = GetOrCreateChild(effectsRoot.transform, "ArrowProjectilePool");
            PrefabPool arrowPool = GetOrAddComponent<PrefabPool>(arrowPoolObject);
            arrowPool.Configure(arrowPrefab, 24, arrowPoolObject.transform);

            GameObject impactPoolObject = GetOrCreateChild(effectsRoot.transform, "WeaponImpactPool");
            PrefabPool impactPool = GetOrAddComponent<PrefabPool>(impactPoolObject);
            impactPool.Configure(impactPrefab, 8, impactPoolObject.transform);

            PlaceOrUpdateScenePlayer(scene, gameplayRoot.transform, spawnPointsRoot.transform, weaponConfig, arrowPool, impactPool);

            SpawnPointMarker playerSpawnMarker = FindSpawnPoint(spawnPointsRoot.transform, SpawnPointKind.Player);
            if (playerSpawnMarker != null)
            {
                GameObject targetDummy = GetOrCreateChild(gameplayRoot.transform, "TargetDummy");
                targetDummy.transform.position = playerSpawnMarker.transform.position + new Vector3(0f, 4f, 0f);
                targetDummy.transform.rotation = Quaternion.identity;
                targetDummy.transform.localScale = new Vector3(0.8f, 1.25f, 1f);

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(targetDummy);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.86f, 0.26f, 0.26f, 1f);
                spriteRenderer.sortingOrder = 12;

                BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(targetDummy);
                collider.isTrigger = false;

                Health health = GetOrAddComponent<Health>(targetDummy);
                health.Initialize(Mathf.Max(weaponConfig != null ? weaponConfig.Damage * 4f : 4f, 1f));
            }

            sceneContext.ConfigureEffects(effectsRoot.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PlaceOrUpdateScenePlayer(
            Scene scene,
            Transform gameplayRoot,
            Transform spawnPointsRoot,
            WeaponConfig weaponConfig,
            PrefabPool arrowPool,
            PrefabPool impactPool)
        {
            SpawnPointMarker playerSpawnMarker = FindSpawnPoint(spawnPointsRoot, SpawnPointKind.Player);
            if (playerSpawnMarker == null)
            {
                Debug.LogError("A player spawn marker was not found in MainArena. Run Phase 2 setup first.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"Player prefab missing at {PlayerPrefabPath}. Run Phase 3 player setup first.");
                return;
            }

            PlayerRoot existingPlayer = Object.FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
            GameObject playerInstance = existingPlayer != null
                ? existingPlayer.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);

            playerInstance.name = "Player";
            playerInstance.transform.SetParent(gameplayRoot, true);
            playerInstance.transform.position = playerSpawnMarker.transform.position;
            playerInstance.transform.rotation = Quaternion.identity;

            PlayerRoot playerRoot = GetOrAddComponent<PlayerRoot>(playerInstance);
            PlayerInputReader inputReader = GetOrAddComponent<PlayerInputReader>(playerInstance);
            PlayerMovementController movementController = GetOrAddComponent<PlayerMovementController>(playerInstance);
            PlayerAimController aimController = GetOrAddComponent<PlayerAimController>(playerInstance);
            ProjectileWeaponLauncher projectileLauncher = GetOrAddComponent<ProjectileWeaponLauncher>(playerInstance);
            WeaponImpactFeedbackSpawner impactSpawner = GetOrAddComponent<WeaponImpactFeedbackSpawner>(playerInstance);
            WeaponController weaponController = GetOrAddComponent<WeaponController>(playerInstance);

            projectileLauncher.Configure(arrowPool);
            impactSpawner.Configure(impactPool);
            weaponController.Configure(playerRoot, inputReader, aimController, movementController, projectileLauncher, impactSpawner, weaponConfig);
            playerRoot.ConfigureCombat(weaponController);
        }

        private static SpawnPointMarker FindSpawnPoint(Transform root, SpawnPointKind kind)
        {
            SpawnPointMarker[] markers = root.GetComponentsInChildren<SpawnPointMarker>(true);
            for (int index = 0; index < markers.Length; index++)
            {
                if (markers[index].Kind == kind)
                {
                    return markers[index];
                }
            }

            return null;
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

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
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