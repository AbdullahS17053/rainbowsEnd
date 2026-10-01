using System.IO;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Unicorn;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase5UnicornSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string UnicornPrefabPath = "Assets/_Project/Prefabs/PF_Unicorn.prefab";
        private const string TrailSegmentPrefabPath = "Assets/_Project/Prefabs/PF_RainbowTrailSegment.prefab";
        private const string TailTrailMaterialPath = "Assets/_Project/Art/Materials/M_UnicornTailTrail.mat";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 5 Unicorn Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);

            if (configRegistry?.UnicornConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry or UnicornConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            GameObject trailSegmentPrefab = CreateOrUpdateTrailSegmentPrefab(placeholderSprite);
            CreateOrUpdateUnicornPrefab(placeholderSprite, configRegistry.UnicornConfig);
            CreateOrUpdateSceneObjects(trailSegmentPrefab, configRegistry.UnicornConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 5 unicorn setup completed.");
        }

        private static GameObject CreateOrUpdateTrailSegmentPrefab(Sprite placeholderSprite)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(TrailSegmentPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(TrailSegmentPrefabPath)
                : new GameObject("PF_RainbowTrailSegment");

            try
            {
                prefabRoot.name = "PF_RainbowTrailSegment";

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(prefabRoot);
                spriteRenderer.sprite = placeholderSprite;
                spriteRenderer.color = new Color(0.48f, 0.9f, 1f, 0.75f);
                spriteRenderer.sortingOrder = 4;
                prefabRoot.transform.localScale = new Vector3(0.32f, 0.32f, 1f);

                GetOrAddComponent<RainbowTrailSegment>(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, TrailSegmentPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(TrailSegmentPrefabPath);
        }

        private static void CreateOrUpdateUnicornPrefab(Sprite placeholderSprite, UnicornConfig unicornConfig)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(UnicornPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(UnicornPrefabPath)
                : new GameObject("PF_Unicorn");

            try
            {
                prefabRoot.name = "PF_Unicorn";

                Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(prefabRoot);
                CircleCollider2D bodyCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                bodyCollider.radius = 0.4f;
                bodyCollider.offset = Vector2.zero;
                bodyCollider.isTrigger = true;

                RainbowTrailEmitter trailEmitter = GetOrAddComponent<RainbowTrailEmitter>(prefabRoot);
                UnicornController unicornController = GetOrAddComponent<UnicornController>(prefabRoot);

                GameObject visualRoot = GetOrCreateChild(prefabRoot.transform, "VisualRoot");
                SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualRoot);
                visualRenderer.sprite = placeholderSprite;
                visualRenderer.color = new Color(1f, 0.68f, 0.88f, 1f);
                visualRenderer.sortingOrder = 14;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localScale = new Vector3(0.95f, 1.2f, 1f);

                Material tailTrailMaterial = EnsureTailTrailMaterial();
                UnicornTailTrail tailTrail = GetOrAddComponent<UnicornTailTrail>(prefabRoot);
                Transform tailAnchor = tailTrail.ResolveTailAnchor();
                ParticleSystem tailPs = tailTrail.ResolveParticleSystem();
                tailTrail.Configure(tailAnchor, tailPs, tailTrailMaterial);

                unicornController.Configure(unicornConfig, body, bodyCollider, visualRenderer, trailEmitter, tailTrail);
                trailEmitter.Configure(unicornController);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, UnicornPrefabPath);
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

        private static void CreateOrUpdateSceneObjects(GameObject trailSegmentPrefab, UnicornConfig unicornConfig)
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
            GameObject effectsRoot = FindRoot(scene, "Effects") ?? GetOrCreateRoot(scene, "Effects");
            GameObject trailRoot = GetOrCreateChild(effectsRoot.transform, "TrailRoot");
            GameObject rainbowTrailPoolObject = GetOrCreateChild(effectsRoot.transform, "RainbowTrailPool");
            PrefabPool rainbowTrailPool = GetOrAddComponent<PrefabPool>(rainbowTrailPoolObject);
            rainbowTrailPool.Configure(trailSegmentPrefab, 48, trailRoot.transform);

            PlaceOrUpdateSceneUnicorn(scene, gameplayRoot.transform, spawnPointsRoot.transform, unicornConfig, rainbowTrailPool);

            sceneContext.ConfigureEffects(effectsRoot.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PlaceOrUpdateSceneUnicorn(Scene scene, Transform gameplayRoot, Transform spawnPointsRoot, UnicornConfig unicornConfig, PrefabPool rainbowTrailPool)
        {
            SpawnPointMarker unicornSpawnMarker = FindSpawnPoint(spawnPointsRoot, SpawnPointKind.Unicorn);
            if (unicornSpawnMarker == null)
            {
                Debug.LogError("A unicorn spawn marker was not found in MainArena. Run Phase 2 setup first.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnicornPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"Unicorn prefab missing at {UnicornPrefabPath}.");
                return;
            }

            UnicornController existingUnicorn = Object.FindFirstObjectByType<UnicornController>(FindObjectsInactive.Include);
            GameObject unicornInstance = existingUnicorn != null
                ? existingUnicorn.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);

            unicornInstance.name = "Unicorn";
            unicornInstance.transform.SetParent(gameplayRoot, true);
            unicornInstance.transform.position = unicornSpawnMarker.transform.position;
            unicornInstance.transform.rotation = Quaternion.identity;

            if (unicornInstance.TryGetComponent(out UnicornController unicornController))
            {
                RainbowTrailEmitter trailEmitter = unicornInstance.GetComponent<RainbowTrailEmitter>();
                UnicornTailTrail tailTrail = unicornInstance.GetComponent<UnicornTailTrail>();
                if (tailTrail != null)
                {
                    Material tailTrailMaterial = EnsureTailTrailMaterial();
                    tailTrail.Configure(tailTrail.ResolveTailAnchor(), tailTrail.ResolveParticleSystem(), tailTrailMaterial);
                }

                unicornController.Configure(
                    unicornConfig,
                    unicornInstance.GetComponent<Rigidbody2D>(),
                    unicornInstance.GetComponent<Collider2D>(),
                    unicornInstance.GetComponentInChildren<SpriteRenderer>(),
                    trailEmitter,
                    tailTrail);
                trailEmitter?.ConfigurePool(rainbowTrailPool);
            }
        }

        private static Material EnsureTailTrailMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(TailTrailMaterialPath);
            if (material != null)
            {
                return material;
            }

            string absolutePath = GetAbsoluteAssetPath(TailTrailMaterialPath);
            string parentDirectory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(parentDirectory) && !Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Sprites/Default");
            material = new Material(shader)
            {
                name = "M_UnicornTailTrail"
            };

            AssetDatabase.CreateAsset(material, TailTrailMaterialPath);
            return material;
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