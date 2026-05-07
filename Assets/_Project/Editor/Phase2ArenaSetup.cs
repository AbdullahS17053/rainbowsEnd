using System.IO;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RainbowsEnd.Editor
{
    public static class Phase2ArenaSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";
        private static readonly Color GroundColor = new Color(0.63f, 0.72f, 0.84f, 1f);
        private static readonly Color CoverColor = new Color(0.45f, 0.31f, 0.18f, 1f);
        private static readonly Color ConcealmentColor = new Color(0.22f, 0.56f, 0.26f, 0.55f);
        private static readonly Color FaeTreeColor = new Color(0.36f, 0.79f, 0.42f, 1f);

        [MenuItem("Rainbows End/Setup/Run Phase 2 Arena Setup")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            Sprite placeholderSprite = EnsurePlaceholderSprite();

            GameObject sceneContextRoot = GetOrCreateRoot(scene, "SceneContext");
            GameObject worldRoot = GetOrCreateRoot(scene, "World");
            GameObject environmentRoot = GetOrCreateRoot(scene, "Environment");
            GameObject gameplayRoot = GetOrCreateRoot(scene, "GameplayRoot");
            GameObject spawnPointsRoot = GetOrCreateRoot(scene, "SpawnPoints");
            GameObject uiRoot = GetOrCreateRoot(scene, "UI");
            GameObject cameraRoot = GetOrCreateRoot(scene, "CameraRoot");

            Camera mainCamera = GetOrAddComponent<Camera>(cameraRoot);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 7f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.11f, 0.13f, 0.15f, 1f);
            cameraRoot.transform.position = new Vector3(0f, 0f, -10f);

            ArenaSceneContext sceneContext = GetOrAddComponent<ArenaSceneContext>(sceneContextRoot);

            GameObject groundRoot = GetOrCreateChild(environmentRoot.transform, "Ground");
            GameObject coverRoot = GetOrCreateChild(environmentRoot.transform, "Cover");
            GameObject concealmentRoot = GetOrCreateChild(environmentRoot.transform, "Concealment");
            GameObject propsRoot = GetOrCreateChild(environmentRoot.transform, "Props");

            ArenaBoundsAuthoring arenaBounds = GetOrAddComponent<ArenaBoundsAuthoring>(GetOrCreateChild(worldRoot.transform, "ArenaBounds"));
            arenaBounds.Configure(Vector2.zero, new Vector2(18f, 10f));

            CreateOrUpdateGround(groundRoot.transform, placeholderSprite);
            CreateOrUpdateCoverBlocker(coverRoot.transform, "CoverBlocker_A", new Vector3(-4f, 1.25f, 0f), new Vector3(2.5f, 1f, 1f), placeholderSprite);
            CreateOrUpdateCoverBlocker(coverRoot.transform, "CoverBlocker_B", new Vector3(4f, -1.5f, 0f), new Vector3(2.25f, 1.25f, 1f), placeholderSprite);
            CreateOrUpdateConcealmentZone(concealmentRoot.transform, "ConcealmentZone_A", new Vector3(-5.5f, -2.25f, 0f), new Vector3(2f, 1.5f, 1f), placeholderSprite);
            CreateOrUpdateConcealmentZone(concealmentRoot.transform, "ConcealmentZone_B", new Vector3(5.5f, 2.5f, 0f), new Vector3(2f, 1.5f, 1f), placeholderSprite);
            FaeTreeAnchor faeTreeAnchor = CreateOrUpdateFaeTreeAnchor(propsRoot.transform, new Vector3(0f, -3.5f, 0f), placeholderSprite);

            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "PlayerSpawn", SpawnPointKind.Player, "player", new Vector3(-2.5f, -3f, 0f));
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "UnicornSpawn", SpawnPointKind.Unicorn, "unicorn", new Vector3(0f, 2.75f, 0f));
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "PotSpawn_01", SpawnPointKind.Pot, "pot-01", new Vector3(-4f, -2f, 0f), 1);
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "PotSpawn_02", SpawnPointKind.Pot, "pot-02", new Vector3(0f, -2f, 0f), 2);
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "PotSpawn_03", SpawnPointKind.Pot, "pot-03", new Vector3(4f, -2f, 0f), 3);
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "EnemySpawn_North", SpawnPointKind.EnemyEdge, "enemy-north", new Vector3(0f, 4.6f, 0f));
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "EnemySpawn_South", SpawnPointKind.EnemyEdge, "enemy-south", new Vector3(0f, -4.6f, 0f));
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "EnemySpawn_East", SpawnPointKind.EnemyEdge, "enemy-east", new Vector3(8.4f, 0f, 0f));
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "EnemySpawn_West", SpawnPointKind.EnemyEdge, "enemy-west", new Vector3(-8.4f, 0f, 0f));
            CreateOrUpdateSpawnPoint(spawnPointsRoot.transform, "FaeTreeMarker", SpawnPointKind.FaeTree, "fae-tree", faeTreeAnchor.transform.position);

            EnsureUiShell(uiRoot);

            sceneContext.Configure(worldRoot.transform, environmentRoot.transform, gameplayRoot.transform, spawnPointsRoot.transform, uiRoot.transform, mainCamera);
            sceneContext.ConfigureWorld(arenaBounds, faeTreeAnchor);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 2 arena authoring completed.");
        }

        private static void CreateOrUpdateGround(Transform parent, Sprite placeholderSprite)
        {
            GameObject groundPlane = GetOrCreateSpriteObject(parent, "GroundPlane", placeholderSprite, GroundColor, -10);
            groundPlane.transform.localPosition = Vector3.zero;
            groundPlane.transform.localScale = new Vector3(18f, 10f, 1f);
        }

        private static void CreateOrUpdateCoverBlocker(Transform parent, string name, Vector3 position, Vector3 scale, Sprite placeholderSprite)
        {
            GameObject blocker = GetOrCreateSpriteObject(parent, name, placeholderSprite, CoverColor, 0);
            blocker.transform.localPosition = position;
            blocker.transform.localScale = scale;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(blocker);
            collider.isTrigger = false;
            GetOrAddComponent<CoverBlocker>(blocker);
        }

        private static void CreateOrUpdateConcealmentZone(Transform parent, string name, Vector3 position, Vector3 scale, Sprite placeholderSprite)
        {
            GameObject zone = GetOrCreateSpriteObject(parent, name, placeholderSprite, ConcealmentColor, 1);
            zone.transform.localPosition = position;
            zone.transform.localScale = scale;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(zone);
            collider.isTrigger = true;
            GetOrAddComponent<ConcealmentZone>(zone);
        }

        private static FaeTreeAnchor CreateOrUpdateFaeTreeAnchor(Transform parent, Vector3 position, Sprite placeholderSprite)
        {
            GameObject faeTree = GetOrCreateSpriteObject(parent, "FaeTreeAnchor", placeholderSprite, FaeTreeColor, 2);
            faeTree.transform.localPosition = position;
            faeTree.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            return GetOrAddComponent<FaeTreeAnchor>(faeTree);
        }

        private static void CreateOrUpdateSpawnPoint(Transform parent, string name, SpawnPointKind kind, string markerId, Vector3 position, int order = 0)
        {
            GameObject markerObject = GetOrCreateChild(parent, name);
            markerObject.transform.localPosition = position;
            SpawnPointMarker marker = GetOrAddComponent<SpawnPointMarker>(markerObject);
            marker.Configure(kind, markerId, order);
        }

        private static void EnsureUiShell(GameObject uiRootObject)
        {
            Canvas canvas = GetOrAddComponent<Canvas>(uiRootObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            GetOrAddComponent<CanvasScaler>(uiRootObject);
            GetOrAddComponent<GraphicRaycaster>(uiRootObject);

            Transform uiRoot = uiRootObject.transform;

            GameObject hudRoot = GetOrCreateChild(uiRoot, "HudRoot");
            RectTransform hudRootTransform = GetOrAddComponent<RectTransform>(hudRoot);
            hudRootTransform.anchorMin = Vector2.zero;
            hudRootTransform.anchorMax = Vector2.one;
            hudRootTransform.offsetMin = Vector2.zero;
            hudRootTransform.offsetMax = Vector2.zero;

            GameObject goldCounterPlaceholder = GetOrCreateChild(hudRoot.transform, "GoldCounterPlaceholder");
            RectTransform goldCounterTransform = GetOrAddComponent<RectTransform>(goldCounterPlaceholder);
            goldCounterTransform.anchorMin = new Vector2(0f, 1f);
            goldCounterTransform.anchorMax = new Vector2(0f, 1f);
            goldCounterTransform.pivot = new Vector2(0f, 1f);
            goldCounterTransform.anchoredPosition = new Vector2(24f, -24f);
            goldCounterTransform.sizeDelta = new Vector2(240f, 64f);
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == name)
                {
                    return roots[index];
                }
            }

            GameObject root = new GameObject(name);
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

        private static GameObject GetOrCreateSpriteObject(Transform parent, string name, Sprite placeholderSprite, Color color, int sortingOrder)
        {
            Transform child = parent.Find(name);
            GameObject target = child != null ? child.gameObject : new GameObject(name);

            if (child == null)
            {
                target.transform.SetParent(parent, false);
            }

            target.name = name;
            RemoveLegacyQuadComponents(target);

            SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(target);
            spriteRenderer.sprite = placeholderSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;

            return target;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static void RemoveLegacyQuadComponents(GameObject target)
        {
            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Object.DestroyImmediate(meshFilter);
            }

            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Object.DestroyImmediate(meshRenderer);
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Sprite EnsurePlaceholderSprite()
        {
            string projectRootPath = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string absolutePath = Path.Combine(projectRootPath, PlaceholderSpritePath);

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

                byte[] bytes = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                File.WriteAllBytes(absolutePath, bytes);
            }

            AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter textureImporter = AssetImporter.GetAtPath(PlaceholderSpritePath) as TextureImporter;
            if (textureImporter != null)
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
    }
}