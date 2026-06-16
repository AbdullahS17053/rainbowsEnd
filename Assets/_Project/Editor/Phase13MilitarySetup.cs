using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Orchestration;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase13MilitarySetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string ConfigRootPath = "Assets/_Project/ScriptableObjects";
        private const string DefaultsFolderName = "Defaults";
        private const string MilitaryPrefabPath = "Assets/_Project/Prefabs/PF_Military.prefab";
        private const string MilitaryProjectilePrefabPath = "Assets/_Project/Prefabs/PF_MilitaryProjectile.prefab";
        private const string WeaponPickupPrefabPath = "Assets/_Project/Prefabs/PF_WeaponPickup.prefab";
        private const string GoldPickupPrefabPath = "Assets/_Project/Prefabs/PF_GoldPickup.prefab";
        private const string BowArrowPrefabPath = "Assets/_Project/Prefabs/PF_BowArrow.prefab";
        private const string ImpactPrefabPath = "Assets/_Project/Prefabs/PF_BowImpact.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 13 Military Setup")]
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
            GameObject goldPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldPickupPrefabPath);
            if (bowArrowPrefab == null || impactPrefab == null)
            {
                Debug.LogError("Phase 4 combat prefabs are missing. Run Phase 4 combat setup first.");
                return;
            }

            string defaultsPath = EnsureFolder(ConfigRootPath, DefaultsFolderName);

            EnemyConfig militaryConfig = CreateIfMissing<EnemyConfig>(defaultsPath, "SO_Enemy_Military_Default.asset", CreateMilitaryConfigDefaults);
            WeaponConfig assaultRifleConfig = CreateIfMissing<WeaponConfig>(defaultsPath, "SO_Weapon_AssaultRifle_Default.asset", CreateAssaultRifleConfigDefaults);
            CreateMilitaryConfigDefaults(militaryConfig);
            CreateAssaultRifleConfigDefaults(assaultRifleConfig);

            SetProjectConfigRegistryMilitary(configRegistry, militaryConfig);

            GameObject militaryProjectilePrefab = CreateOrUpdateMilitaryProjectilePrefab(placeholderSprite);
            GameObject militaryPrefab = CreateOrUpdateMilitaryPrefab(placeholderSprite, militaryConfig, assaultRifleConfig);

            CreateOrUpdateSceneObjects(militaryPrefab, militaryProjectilePrefab, assaultRifleConfig, militaryConfig, goldPickupPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 13 military setup completed.");
        }

        private static GameObject CreateOrUpdateMilitaryProjectilePrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(MilitaryProjectilePrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(MilitaryProjectilePrefabPath)
                : new GameObject("PF_MilitaryProjectile");

            try
            {
                prefabRoot.name = "PF_MilitaryProjectile";

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.89f, 0.24f, 0.18f, 1f);
                spriteRenderer.sortingOrder = 25;
                prefabRoot.transform.localScale = new Vector3(0.12f, 0.52f, 1f);

                GetOrAddComponent<ArrowProjectile>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MilitaryProjectilePrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(MilitaryProjectilePrefabPath);
        }

        private static GameObject CreateOrUpdateMilitaryPrefab(Sprite placeholderSprite, EnemyConfig militaryConfig, WeaponConfig assaultRifleConfig)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(MilitaryPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(MilitaryPrefabPath)
                : new GameObject("PF_Military");

            try
            {
                prefabRoot.name = "PF_Military";

                Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(prefabRoot);
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                CircleCollider2D bodyCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                bodyCollider.radius = 0.38f;
                bodyCollider.isTrigger = true;

                Health health = GetOrAddComponent<Health>(prefabRoot);
                ProjectileWeaponLauncher projectileLauncher = GetOrAddComponent<ProjectileWeaponLauncher>(prefabRoot);
                WeaponImpactFeedbackSpawner impactSpawner = GetOrAddComponent<WeaponImpactFeedbackSpawner>(prefabRoot);
                RangedAttackController rangedAttackController = GetOrAddComponent<RangedAttackController>(prefabRoot);
                MilitaryController militaryController = GetOrAddComponent<MilitaryController>(prefabRoot);
                HealthStageFeedbackPresenter healthFeedback = GetOrAddComponent<HealthStageFeedbackPresenter>(prefabRoot);

                GameObject visualRoot = GetOrCreateChild(prefabRoot.transform, "VisualRoot");
                SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualRoot);
                visualRenderer.sprite = placeholderSprite;
                visualRenderer.color = new Color(0.62f, 0.12f, 0.18f, 1f);
                visualRenderer.sortingOrder = 17;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localScale = new Vector3(0.84f, 0.96f, 1f);

                militaryController.Configure(militaryConfig, assaultRifleConfig, visualRoot.transform, visualRenderer, body, bodyCollider, health, projectileLauncher, impactSpawner, rangedAttackController);
                healthFeedback.Configure(health, visualRenderer);

                SerializedObject healthFeedbackSo = new SerializedObject(healthFeedback);
                healthFeedbackSo.FindProperty("useCurrentColorAsHealthy").boolValue = false;
                healthFeedbackSo.FindProperty("healthyColor").colorValue = new Color(0.62f, 0.12f, 0.18f, 1f);
                healthFeedbackSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MilitaryPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(MilitaryPrefabPath);
        }

        private static void SetProjectConfigRegistryMilitary(ProjectConfigRegistry registry, EnemyConfig militaryConfig)
        {
            SerializedObject serializedObject = new SerializedObject(registry);
            SerializedProperty militaryProperty = serializedObject.FindProperty("militaryConfig");
            if (militaryProperty != null)
            {
                militaryProperty.objectReferenceValue = militaryConfig;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        private static void CreateOrUpdateSceneObjects(
            GameObject militaryPrefab,
            GameObject militaryProjectilePrefab,
            WeaponConfig assaultRifleConfig,
            EnemyConfig militaryConfig,
            GameObject goldPickupPrefab)
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

            PrefabPool militaryProjectilePool = ConfigurePool(GetOrCreateChild(effectsRoot.transform, "MilitaryProjectilePool"), militaryProjectilePrefab, 16);
            PrefabPool impactPool = FindPoolInParent(effectsRoot.transform, "WeaponImpactPool");
            PrefabPool militaryPool = ConfigurePool(GetOrCreateChild(enemySystemsRoot.transform, "MilitaryPool"), militaryPrefab, 2);
            PrefabPool weaponPickupPool = FindPoolInParent(enemySystemsRoot.transform, "WeaponPickupPool");
            PrefabPool goldPickupPool = FindPoolInParent(enemySystemsRoot.transform, "GoldPickupPool");

            sceneContext.ConfigureEffects(effectsRoot.transform);

            GameObject militarySpawnerObject = GetOrCreateChild(enemySystemsRoot.transform, "MilitarySpawner");
            MilitarySpawner militarySpawner = GetOrAddComponent<MilitarySpawner>(militarySpawnerObject);
            militarySpawner.Configure(militaryConfig, assaultRifleConfig, spawnPointsRoot.transform, militaryPool, militaryProjectilePool, impactPool, weaponPickupPool, goldPickupPool, CreateDefaultWeaponDropOptions(assaultRifleConfig));

            SerializedObject spawnerSo = new SerializedObject(militarySpawner);
            SerializedProperty goldOptionsProp = spawnerSo.FindProperty("goldDropOptions");
            goldOptionsProp.arraySize = 1;
            goldOptionsProp.GetArrayElementAtIndex(0).FindPropertyRelative("goldAmount").intValue = 10;
            SerializedProperty defaultGoldAmountProp = spawnerSo.FindProperty("defaultGoldDropAmount");
            if (defaultGoldAmountProp != null)
            {
                defaultGoldAmountProp.intValue = 10;
            }
            spawnerSo.FindProperty("legacyDropGoldAmount").intValue = 10;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(militarySpawner);

            WireMilitaryToRunDirector(scene, militarySpawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireMilitaryToRunDirector(Scene scene, MilitarySpawner militarySpawner)
        {
            RunDirector runDirector = Object.FindFirstObjectByType<RunDirector>(FindObjectsInactive.Include);
            if (runDirector == null)
            {
                Debug.LogWarning("RunDirector not found. MilitarySpawner will not be wired to wave orchestration. Run Phase 12 setup first.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(runDirector);
            SerializedProperty militarySpawnerProperty = serializedObject.FindProperty("militarySpawner");
            if (militarySpawnerProperty != null)
            {
                militarySpawnerProperty.objectReferenceValue = militarySpawner;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runDirector);
        }

        private static PrefabPool ConfigurePool(GameObject poolObject, GameObject prefab, int prewarmCount)
        {
            PrefabPool prefabPool = GetOrAddComponent<PrefabPool>(poolObject);
            prefabPool.Configure(prefab, prewarmCount, poolObject.transform);
            return prefabPool;
        }

        private static PrefabPool FindPoolInParent(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<PrefabPool>() : null;
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

        private static void CreateMilitaryConfigDefaults(EnemyConfig config)
        {
            SerializedObject serializedObject = new SerializedObject(config);
            SetFloat(serializedObject, "<MaxHealth>k__BackingField", 12f);
            SetFloat(serializedObject, "<MoveSpeed>k__BackingField", 2.75f);
            SetFloat(serializedObject, "<ContactDamage>k__BackingField", 1f);
            SetFloat(serializedObject, "<TargetRefreshSeconds>k__BackingField", 0.2f);
            SetFloat(serializedObject, "<EngagementRange>k__BackingField", 24f);
            SetFloat(serializedObject, "<SeparationRadius>k__BackingField", 0.7f);
            SetFloat(serializedObject, "<SeparationStrength>k__BackingField", 2.5f);
            SetFloat(serializedObject, "<FleeSpeedMultiplier>k__BackingField", 1.5f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void CreateAssaultRifleConfigDefaults(WeaponConfig config)
        {
            SerializedObject serializedObject = new SerializedObject(config);
            SetFloat(serializedObject, "<Damage>k__BackingField", 1.6f);
            SetInt(serializedObject, "<ProjectileCount>k__BackingField", 1);
            SetFloat(serializedObject, "<CooldownSeconds>k__BackingField", 0.25f);
            SetFloat(serializedObject, "<Range>k__BackingField", 14f);
            SetFloat(serializedObject, "<AutoFireConeDegrees>k__BackingField", 32f);
            SetFloat(serializedObject, "<ProjectileSpeed>k__BackingField", 28f);
            SetFloat(serializedObject, "<MuzzleOffset>k__BackingField", 0.55f);
            SetFloat(serializedObject, "<MinSpreadDegrees>k__BackingField", 2f);
            SetFloat(serializedObject, "<MaxSpreadDegrees>k__BackingField", 8f);
            SetFloat(serializedObject, "<VolleySpreadDegrees>k__BackingField", 0f);
            SetFloat(serializedObject, "<ImpactEffectLifetimeSeconds>k__BackingField", 0.1f);
            SetFloat(serializedObject, "<ImpactEffectScale>k__BackingField", 0.28f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static WeaponDropOption[] CreateDefaultWeaponDropOptions(WeaponConfig assaultRifleConfig)
        {
            if (assaultRifleConfig != null)
            {
                return new[] { new WeaponDropOption(assaultRifleConfig, 24) };
            }

            return new WeaponDropOption[0];
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
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
