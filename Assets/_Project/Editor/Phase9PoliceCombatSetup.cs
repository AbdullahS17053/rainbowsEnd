using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase9PoliceCombatSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string ConfigRootPath = "Assets/_Project/ScriptableObjects";
        private const string DefaultsFolderName = "Defaults";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string PolicePrefabPath = "Assets/_Project/Prefabs/PF_Police.prefab";
        private const string PoliceProjectilePrefabPath = "Assets/_Project/Prefabs/PF_PoliceProjectile.prefab";
        private const string WeaponPickupPrefabPath = "Assets/_Project/Prefabs/PF_WeaponPickup.prefab";
        private const string BowArrowPrefabPath = "Assets/_Project/Prefabs/PF_BowArrow.prefab";
        private const string ImpactPrefabPath = "Assets/_Project/Prefabs/PF_BowImpact.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 9 Police Combat Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);
            if (configRegistry?.DefaultWeaponConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry or DefaultWeaponConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 and Phase 4 setup first.");
                return;
            }

            GameObject bowArrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BowArrowPrefabPath);
            GameObject impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactPrefabPath);
            if (bowArrowPrefab == null || impactPrefab == null)
            {
                Debug.LogError("Phase 4 combat prefabs are missing. Run Phase 4 combat setup first.");
                return;
            }

            string defaultsPath = EnsureFolder(ConfigRootPath, DefaultsFolderName);

            EnemyConfig policeConfig = CreateIfMissing<EnemyConfig>(defaultsPath, "SO_Enemy_Police_Default.asset", CreatePoliceConfigDefaults);
            WeaponConfig pistolConfig = CreateIfMissing<WeaponConfig>(defaultsPath, "SO_Weapon_Pistol_Default.asset", CreatePistolConfigDefaults);
            WeaponConfig shotgunConfig = CreateIfMissing<WeaponConfig>(defaultsPath, "SO_Weapon_Shotgun_Default.asset", CreateShotgunConfigDefaults);
            CreatePoliceConfigDefaults(policeConfig);
            CreatePistolConfigDefaults(pistolConfig);
            CreateShotgunConfigDefaults(shotgunConfig);

            GameObject policeProjectilePrefab = CreateOrUpdatePoliceProjectilePrefab(placeholderSprite);
            GameObject weaponPickupPrefab = CreateOrUpdateWeaponPickupPrefab(placeholderSprite);
            GameObject policePrefab = CreateOrUpdatePolicePrefab(placeholderSprite, policeConfig, pistolConfig);

            CreateOrUpdatePlayerPrefab(configRegistry.DefaultWeaponConfig);
            CreateOrUpdateSceneObjects(policePrefab, policeProjectilePrefab, weaponPickupPrefab, bowArrowPrefab, impactPrefab, configRegistry.DefaultWeaponConfig, policeConfig, pistolConfig, shotgunConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 9 police combat setup completed.");
        }

        private static GameObject CreateOrUpdatePoliceProjectilePrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(PoliceProjectilePrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(PoliceProjectilePrefabPath)
                : new GameObject("PF_PoliceProjectile");

            try
            {
                prefabRoot.name = "PF_PoliceProjectile";

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.48f, 0.78f, 0.98f, 1f);
                spriteRenderer.sortingOrder = 24;
                prefabRoot.transform.localScale = new Vector3(0.1f, 0.48f, 1f);

                GetOrAddComponent<ArrowProjectile>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PoliceProjectilePrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(PoliceProjectilePrefabPath);
        }

        private static GameObject CreateOrUpdateWeaponPickupPrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(WeaponPickupPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(WeaponPickupPrefabPath)
                : new GameObject("PF_WeaponPickup");

            try
            {
                prefabRoot.name = "PF_WeaponPickup";

                CircleCollider2D collider2D = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                collider2D.isTrigger = true;
                collider2D.radius = 0.28f;

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.28f, 0.62f, 1f, 1f);
                spriteRenderer.sortingOrder = 21;
                prefabRoot.transform.localScale = new Vector3(0.38f, 0.38f, 1f);

                GetOrAddComponent<WeaponPickup>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, WeaponPickupPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPickupPrefabPath);
        }

        private static GameObject CreateOrUpdatePolicePrefab(Sprite placeholderSprite, EnemyConfig policeConfig, WeaponConfig pistolConfig)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(PolicePrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(PolicePrefabPath)
                : new GameObject("PF_Police");

            try
            {
                prefabRoot.name = "PF_Police";

                Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(prefabRoot);
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                CircleCollider2D bodyCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                bodyCollider.radius = 0.34f;
                bodyCollider.isTrigger = true;

                Health health = GetOrAddComponent<Health>(prefabRoot);
                ProjectileWeaponLauncher projectileLauncher = GetOrAddComponent<ProjectileWeaponLauncher>(prefabRoot);
                WeaponImpactFeedbackSpawner impactSpawner = GetOrAddComponent<WeaponImpactFeedbackSpawner>(prefabRoot);
                RangedAttackController rangedAttackController = GetOrAddComponent<RangedAttackController>(prefabRoot);
                PoliceController policeController = GetOrAddComponent<PoliceController>(prefabRoot);
                HealthStageFeedbackPresenter healthFeedback = GetOrAddComponent<HealthStageFeedbackPresenter>(prefabRoot);

                GameObject visualRoot = GetOrCreateChild(prefabRoot.transform, "VisualRoot");
                SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualRoot);
                visualRenderer.sprite = placeholderSprite;
                visualRenderer.color = new Color(0.18f, 0.54f, 0.92f, 1f);
                visualRenderer.sortingOrder = 16;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localScale = new Vector3(0.76f, 0.9f, 1f);

                policeController.Configure(policeConfig, pistolConfig, visualRoot.transform, visualRenderer, body, bodyCollider, health, projectileLauncher, impactSpawner, rangedAttackController);
                healthFeedback.Configure(health, visualRenderer);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PolicePrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(PolicePrefabPath);
        }

        private static void CreateOrUpdatePlayerPrefab(WeaponConfig defaultWeaponConfig)
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
                PlayerWeaponInventory weaponInventory = GetOrAddComponent<PlayerWeaponInventory>(prefabRoot);
                ProjectileWeaponLauncher projectileLauncher = GetOrAddComponent<ProjectileWeaponLauncher>(prefabRoot);
                WeaponImpactFeedbackSpawner impactSpawner = GetOrAddComponent<WeaponImpactFeedbackSpawner>(prefabRoot);
                WeaponController weaponController = GetOrAddComponent<WeaponController>(prefabRoot);
                Health health = GetOrAddComponent<Health>(prefabRoot);
                HealthStageFeedbackPresenter healthFeedback = GetOrAddComponent<HealthStageFeedbackPresenter>(prefabRoot);

                Transform visualRoot = playerRoot.VisualRoot != null ? playerRoot.VisualRoot : prefabRoot.transform.Find("VisualRoot");
                SpriteRenderer visualRenderer = playerRoot.VisualRenderer != null ? playerRoot.VisualRenderer : visualRoot?.GetComponent<SpriteRenderer>();

                weaponInventory.ConfigureDefaultWeapon(defaultWeaponConfig);
                weaponController.Configure(playerRoot, inputReader, aimController, movementController, projectileLauncher, impactSpawner, defaultWeaponConfig);
                playerRoot.ConfigureCombat(weaponController);
                healthFeedback.Configure(health, visualRenderer);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateOrUpdateSceneObjects(
            GameObject policePrefab,
            GameObject policeProjectilePrefab,
            GameObject weaponPickupPrefab,
            GameObject bowArrowPrefab,
            GameObject impactPrefab,
            WeaponConfig defaultWeaponConfig,
            EnemyConfig policeConfig,
            WeaponConfig pistolConfig,
            WeaponConfig shotgunConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject spawnPointsRoot = FindRoot(scene, "SpawnPoints");
            GameObject sceneContextRoot = FindRoot(scene, "SceneContext");

            if (gameplayRoot == null || spawnPointsRoot == null || sceneContextRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot, SpawnPoints, or SceneContext. Run earlier setup phases first.");
                return;
            }

            ArenaSceneContext sceneContext = GetOrAddComponent<ArenaSceneContext>(sceneContextRoot);
            GameObject effectsRoot = GetOrCreateRoot(scene, "Effects");
            GameObject enemySystemsRoot = GetOrCreateChild(gameplayRoot.transform, "EnemySystems");

            PrefabPool bowArrowPool = ConfigurePool(GetOrCreateChild(effectsRoot.transform, "ArrowProjectilePool"), bowArrowPrefab, 24);
            PrefabPool policeProjectilePool = ConfigurePool(GetOrCreateChild(effectsRoot.transform, "PoliceProjectilePool"), policeProjectilePrefab, 16);
            PrefabPool impactPool = ConfigurePool(GetOrCreateChild(effectsRoot.transform, "WeaponImpactPool"), impactPrefab, 8);
            PrefabPool policePool = ConfigurePool(GetOrCreateChild(enemySystemsRoot.transform, "PolicePool"), policePrefab, 2);
            PrefabPool weaponPickupPool = ConfigurePool(GetOrCreateChild(enemySystemsRoot.transform, "WeaponPickupPool"), weaponPickupPrefab, 4);

            sceneContext.ConfigureEffects(effectsRoot.transform);
            CreateOrUpdateScenePlayer(defaultWeaponConfig, bowArrowPool, impactPool);

            GameObject policeSpawnerObject = GetOrCreateChild(enemySystemsRoot.transform, "PoliceSpawner");
            PoliceSpawner policeSpawner = GetOrAddComponent<PoliceSpawner>(policeSpawnerObject);
            policeSpawner.Configure(policeConfig, pistolConfig, shotgunConfig, 6, spawnPointsRoot.transform, policePool, policeProjectilePool, impactPool, weaponPickupPool);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateOrUpdateScenePlayer(WeaponConfig defaultWeaponConfig, PrefabPool arrowPool, PrefabPool impactPool)
        {
            PlayerRoot playerRoot = Object.FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
            if (playerRoot == null)
            {
                Debug.LogError("Player instance is missing from MainArena. Run Phase 3 and Phase 4 setup first.");
                return;
            }

            PlayerInputReader inputReader = GetOrAddComponent<PlayerInputReader>(playerRoot.gameObject);
            PlayerMovementController movementController = GetOrAddComponent<PlayerMovementController>(playerRoot.gameObject);
            PlayerAimController aimController = GetOrAddComponent<PlayerAimController>(playerRoot.gameObject);
            PlayerWeaponInventory weaponInventory = GetOrAddComponent<PlayerWeaponInventory>(playerRoot.gameObject);
            ProjectileWeaponLauncher projectileLauncher = GetOrAddComponent<ProjectileWeaponLauncher>(playerRoot.gameObject);
            WeaponImpactFeedbackSpawner impactSpawner = GetOrAddComponent<WeaponImpactFeedbackSpawner>(playerRoot.gameObject);
            WeaponController weaponController = GetOrAddComponent<WeaponController>(playerRoot.gameObject);
            HealthStageFeedbackPresenter healthFeedback = GetOrAddComponent<HealthStageFeedbackPresenter>(playerRoot.gameObject);

            projectileLauncher.Configure(arrowPool);
            impactSpawner.Configure(impactPool);
            weaponInventory.ConfigureDefaultWeapon(defaultWeaponConfig);
            weaponController.Configure(playerRoot, inputReader, aimController, movementController, projectileLauncher, impactSpawner, defaultWeaponConfig);
            playerRoot.ConfigureCombat(weaponController);
            healthFeedback.Configure(playerRoot.Health, playerRoot.VisualRenderer);
        }

        private static PrefabPool ConfigurePool(GameObject poolObject, GameObject prefab, int prewarmCount)
        {
            PrefabPool prefabPool = GetOrAddComponent<PrefabPool>(poolObject);
            prefabPool.Configure(prefab, prewarmCount, poolObject.transform);
            return prefabPool;
        }

        private static T CreateIfMissing<T>(string folderPath, string fileName, System.Action<T> configureDefaults) where T : ScriptableObject
        {
            string assetPath = Path.Combine(folderPath, fileName).Replace("\\", "/");
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existingAsset != null)
            {
                return existingAsset;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            configureDefaults?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void CreatePoliceConfigDefaults(EnemyConfig config)
        {
            SerializedObject serializedObject = new SerializedObject(config);
            SetFloat(serializedObject, "<MaxHealth>k__BackingField", 5f);
            SetFloat(serializedObject, "<MoveSpeed>k__BackingField", 3.25f);
            SetFloat(serializedObject, "<ContactDamage>k__BackingField", 1f);
            SetFloat(serializedObject, "<TargetRefreshSeconds>k__BackingField", 0.2f);
            SetFloat(serializedObject, "<EngagementRange>k__BackingField", 20f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void CreatePistolConfigDefaults(WeaponConfig config)
        {
            SerializedObject serializedObject = new SerializedObject(config);
            SetFloat(serializedObject, "<Damage>k__BackingField", 1.25f);
            SetInt(serializedObject, "<ProjectileCount>k__BackingField", 1);
            SetFloat(serializedObject, "<CooldownSeconds>k__BackingField", 0.75f);
            SetFloat(serializedObject, "<Range>k__BackingField", 10f);
            SetFloat(serializedObject, "<ProjectileSpeed>k__BackingField", 22f);
            SetFloat(serializedObject, "<MuzzleOffset>k__BackingField", 0.55f);
            SetFloat(serializedObject, "<MinSpreadDegrees>k__BackingField", 0.5f);
            SetFloat(serializedObject, "<MaxSpreadDegrees>k__BackingField", 4f);
            SetFloat(serializedObject, "<VolleySpreadDegrees>k__BackingField", 0f);
            SetFloat(serializedObject, "<ImpactEffectLifetimeSeconds>k__BackingField", 0.1f);
            SetFloat(serializedObject, "<ImpactEffectScale>k__BackingField", 0.25f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void CreateShotgunConfigDefaults(WeaponConfig config)
        {
            SerializedObject serializedObject = new SerializedObject(config);
            SetFloat(serializedObject, "<Damage>k__BackingField", 1.9f);
            SetInt(serializedObject, "<ProjectileCount>k__BackingField", 6);
            SetFloat(serializedObject, "<CooldownSeconds>k__BackingField", 0.45f);
            SetFloat(serializedObject, "<Range>k__BackingField", 7.5f);
            SetFloat(serializedObject, "<ProjectileSpeed>k__BackingField", 20f);
            SetFloat(serializedObject, "<MuzzleOffset>k__BackingField", 0.55f);
            SetFloat(serializedObject, "<MinSpreadDegrees>k__BackingField", 8f);
            SetFloat(serializedObject, "<MaxSpreadDegrees>k__BackingField", 18f);
            SetFloat(serializedObject, "<VolleySpreadDegrees>k__BackingField", 24f);
            SetFloat(serializedObject, "<ImpactEffectLifetimeSeconds>k__BackingField", 0.12f);
            SetFloat(serializedObject, "<ImpactEffectScale>k__BackingField", 0.32f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
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

        private static string EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = $"{parentPath}/{folderName}";
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return folderPath;
            }

            AssetDatabase.CreateFolder(parentPath, folderName);
            return folderPath;
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