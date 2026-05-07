using System.IO;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase3PlayerSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string InputActionsPath = "Assets/_Project/Input/IA_Gameplay.inputactions";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";
        private const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png";

        [MenuItem("Rainbows End/Setup/Run Phase 3 Player Setup")]
        public static void Run()
        {
            Sprite placeholderSprite = EnsurePlaceholderSprite();
            InputActionAsset inputActionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            ProjectConfigRegistry configRegistry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);

            if (inputActionsAsset == null)
            {
                Debug.LogError($"Input action asset not found at {InputActionsPath}. Run Phase 0 bootstrap first.");
                return;
            }

            if (configRegistry?.PlayerConfig == null)
            {
                Debug.LogError($"ProjectConfigRegistry or PlayerConfig is missing at {ProjectConfigRegistryPath}. Run Phase 1 core setup first.");
                return;
            }

            CreateOrUpdatePlayerPrefab(placeholderSprite, inputActionsAsset, configRegistry.PlayerConfig);
            PlaceOrUpdateScenePlayer();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 3 player setup completed.");
        }

        private static void CreateOrUpdatePlayerPrefab(Sprite placeholderSprite, InputActionAsset inputActionsAsset, PlayerConfig playerConfig)
        {
            bool prefabExists = File.Exists(GetAbsoluteAssetPath(PlayerPrefabPath));
            GameObject prefabRoot = prefabExists
                ? PrefabUtility.LoadPrefabContents(PlayerPrefabPath)
                : new GameObject("PF_Player");

            try
            {
                prefabRoot.name = "PF_Player";

                Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(prefabRoot);
                CircleCollider2D bodyCollider = GetOrAddComponent<CircleCollider2D>(prefabRoot);
                bodyCollider.radius = 0.35f;
                bodyCollider.offset = Vector2.zero;

                Health health = GetOrAddComponent<Health>(prefabRoot);
                PlayerInputReader inputReader = GetOrAddComponent<PlayerInputReader>(prefabRoot);
                PlayerMovementController movementController = GetOrAddComponent<PlayerMovementController>(prefabRoot);
                PlayerAimController aimController = GetOrAddComponent<PlayerAimController>(prefabRoot);
                PlayerRoot playerRoot = GetOrAddComponent<PlayerRoot>(prefabRoot);

                GameObject visualRoot = GetOrCreateChild(prefabRoot.transform, "VisualRoot");
                SpriteRenderer visualRenderer = GetOrAddComponent<SpriteRenderer>(visualRoot);
                visualRenderer.sprite = placeholderSprite;
                visualRenderer.color = new Color(0.2f, 0.85f, 0.3f, 1f);
                visualRenderer.sortingOrder = 10;
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localScale = new Vector3(0.8f, 1f, 1f);

                playerRoot.Configure(playerConfig, inputActionsAsset, visualRoot.transform, visualRenderer, body, bodyCollider, health, inputReader, movementController, aimController);
                movementController.Configure(playerRoot, inputReader, body);
                aimController.Configure(playerRoot, inputReader, visualRoot.transform, null);

                if (prefabExists)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                }
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

        private static void PlaceOrUpdateScenePlayer()
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            if (gameplayRoot == null)
            {
                Debug.LogError("GameplayRoot was not found in MainArena. Run Phase 2 setup first.");
                return;
            }

            SpawnPointMarker[] markers = Object.FindObjectsByType<SpawnPointMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SpawnPointMarker playerSpawnMarker = null;
            for (int index = 0; index < markers.Length; index++)
            {
                if (markers[index].Kind == SpawnPointKind.Player)
                {
                    playerSpawnMarker = markers[index];
                    break;
                }
            }

            if (playerSpawnMarker == null)
            {
                Debug.LogError("A player spawn marker was not found in MainArena. Run Phase 2 setup first.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"Player prefab missing at {PlayerPrefabPath}.");
                return;
            }

            PlayerRoot existingPlayer = Object.FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
            GameObject playerInstance = existingPlayer != null
                ? existingPlayer.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);

            playerInstance.name = "Player";
            playerInstance.transform.SetParent(gameplayRoot.transform, true);
            playerInstance.transform.position = playerSpawnMarker.transform.position;
            playerInstance.transform.rotation = Quaternion.identity;

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

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }

            return component;
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