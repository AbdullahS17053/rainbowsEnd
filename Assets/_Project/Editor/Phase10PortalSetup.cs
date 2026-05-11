using System.IO;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Spells;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase10PortalSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string PortalPrefabPath = "Assets/_Project/Prefabs/PF_Portal.prefab";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 10 Portal Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);
            if (configRegistry?.PortalConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry or PortalConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            ApplyPortalConfigDefaults(configRegistry.PortalConfig);

            GameObject portalPrefab = CreateOrUpdatePortalPrefab(placeholderSprite);
            CreateOrUpdatePlayerPrefab();
            CreateOrUpdateSceneObjects(configRegistry.PortalConfig, portalPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 10 portal setup completed.");
        }

        private static GameObject CreateOrUpdatePortalPrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(PortalPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(PortalPrefabPath)
                : new GameObject("PF_Portal");

            try
            {
                prefabRoot.name = "PF_Portal";

                CircleCollider2D triggerCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                triggerCollider.isTrigger = true;
                triggerCollider.radius = 0.45f;

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.22f, 0.6f, 0.72f, 0.9f);
                spriteRenderer.sortingOrder = 14;
                prefabRoot.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                GetOrAddComponent<PortalInstance>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PortalPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
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
                GetOrAddComponent<PlayerPortalController>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateOrUpdateSceneObjects(PortalConfig portalConfig, GameObject portalPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject sceneContextRoot = FindRoot(scene, "SceneContext");

            if (gameplayRoot == null || sceneContextRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot or SceneContext. Run earlier setup phases first.");
                return;
            }

            ArenaSceneContext sceneContext = GetOrAddComponent<ArenaSceneContext>(sceneContextRoot);
            GameObject effectsRoot = GetOrCreateRoot(scene, "Effects");
            PrefabPool portalPool = ConfigurePool(GetOrCreateChild(effectsRoot.transform, "PortalPool"), portalPrefab, Mathf.Max(2, portalConfig.MaxActivePortals + 1));

            sceneContext.ConfigureEffects(effectsRoot.transform);

            GameObject spellSystemsRoot = GetOrCreateChild(gameplayRoot.transform, "SpellSystems");
            GameObject portalRegistryObject = GetOrCreateChild(spellSystemsRoot.transform, "PortalRegistry");
            PortalRegistry portalRegistry = GetOrAddComponent<PortalRegistry>(portalRegistryObject);
            portalRegistry.Configure(portalConfig, portalPool);

            PlayerRoot playerRoot = Object.FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
            if (playerRoot == null)
            {
                Debug.LogError("Player instance is missing from MainArena. Run earlier player setup first.");
                return;
            }

            GetOrAddComponent<PlayerPortalController>(playerRoot.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyPortalConfigDefaults(PortalConfig portalConfig)
        {
            SerializedObject serializedObject = new SerializedObject(portalConfig);
            SetFloat(serializedObject, "<CooldownSeconds>k__BackingField", 0.5f);
            SetInt(serializedObject, "<MaxActivePortals>k__BackingField", 2);
            SetFloat(serializedObject, "<PlacementDistance>k__BackingField", 1.5f);
            SetFloat(serializedObject, "<ExitOffsetDistance>k__BackingField", 0.85f);
            SetFloat(serializedObject, "<ReentryLockoutSeconds>k__BackingField", 0.25f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(portalConfig);
        }

        private static PrefabPool ConfigurePool(GameObject poolObject, GameObject prefab, int prewarmCount)
        {
            PrefabPool prefabPool = GetOrAddComponent<PrefabPool>(poolObject);
            prefabPool.Configure(prefab, prewarmCount, poolObject.transform);
            return prefabPool;
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