using System.IO;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Orchestration;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RainbowsEnd.Editor
{
    public static class Phase12LevelOrchestrationSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string WaveConfigPath = "Assets/_Project/ScriptableObjects/Defaults/SO_Wave_Default.asset";
        private const string ProjectConfigRegistryPath = "Assets/_Project/Settings/Resources/ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 12 Level Orchestration Setup")]
        public static void Run()
        {
            if (!EnsureTextMeshProEssentials())
            {
                return;
            }

            WaveConfig waveConfig = EnsureWaveConfigAsset();
            EnsureRegistryUsesWaveConfig(waveConfig);
            CreateOrUpdateSceneObjects(waveConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 12 level orchestration setup completed.");
        }

        private static WaveConfig EnsureWaveConfigAsset()
        {
            EnsureAssetDirectory(WaveConfigPath);

            WaveConfig waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
            if (waveConfig == null)
            {
                waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
                AssetDatabase.CreateAsset(waveConfig, WaveConfigPath);
            }

            ApplyWaveDefaults(waveConfig);
            return waveConfig;
        }

        private static void ApplyWaveDefaults(WaveConfig waveConfig)
        {
            SerializedObject serializedObject = new SerializedObject(waveConfig);
            SerializedProperty wavesProperty = serializedObject.FindProperty("waves");
            wavesProperty.arraySize = 5;

            SetWaveDefinition(wavesProperty.GetArrayElementAtIndex(0), "Wave 1", 0.75f, 1.8f, 3, 3, 0);
            SetWaveDefinition(wavesProperty.GetArrayElementAtIndex(1), "Wave 2", 0.75f, 1.6f, 4, 5, 0);
            SetWaveDefinition(wavesProperty.GetArrayElementAtIndex(2), "Wave 3", 0.75f, 1.5f, 5, 4, 1);
            SetWaveDefinition(wavesProperty.GetArrayElementAtIndex(3), "Wave 4", 0.75f, 1.3f, 6, 5, 2);
            SetWaveDefinition(wavesProperty.GetArrayElementAtIndex(4), "Wave 5", 0.75f, 1.2f, 7, 6, 3);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(waveConfig);
        }

        private static void SetWaveDefinition(
            SerializedProperty waveProperty,
            string waveName,
            float startDelaySeconds,
            float spawnIntervalSeconds,
            int maxConcurrentEnemies,
            int travellerCount,
            int policeCount)
        {
            waveProperty.FindPropertyRelative("waveName").stringValue = waveName;
            waveProperty.FindPropertyRelative("startDelaySeconds").floatValue = startDelaySeconds;
            waveProperty.FindPropertyRelative("spawnIntervalSeconds").floatValue = spawnIntervalSeconds;
            waveProperty.FindPropertyRelative("maxConcurrentEnemies").intValue = maxConcurrentEnemies;
            waveProperty.FindPropertyRelative("travellerCount").intValue = travellerCount;
            waveProperty.FindPropertyRelative("policeCount").intValue = policeCount;
        }

        private static void EnsureRegistryUsesWaveConfig(WaveConfig waveConfig)
        {
            ProjectConfigRegistry registry = AssetDatabase.LoadAssetAtPath<ProjectConfigRegistry>(ProjectConfigRegistryPath);
            if (registry == null)
            {
                Debug.LogWarning($"ProjectConfigRegistry was not found at {ProjectConfigRegistryPath}. Run Phase 1 Core Setup before entering play mode.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(registry);
            SerializedProperty waveProperty = serializedObject.FindProperty("waveConfig");
            waveProperty.objectReferenceValue = waveConfig;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        private static void CreateOrUpdateSceneObjects(WaveConfig waveConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            GameObject gameplayRoot = FindRoot(scene, "GameplayRoot");
            GameObject uiRoot = FindRoot(scene, "UI");

            if (gameplayRoot == null || uiRoot == null)
            {
                Debug.LogError("MainArena is missing GameplayRoot or UI. Run earlier setup phases first.");
                return;
            }

            EnemySpawner enemySpawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
            PoliceSpawner policeSpawner = Object.FindFirstObjectByType<PoliceSpawner>(FindObjectsInactive.Include);
            PotRegistry potRegistry = Object.FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
            RunOutcomePresenter outcomePresenter = EnsureOutcomeUi(uiRoot.transform);
            if (outcomePresenter == null)
            {
                return;
            }

            GameObject runSystemsRoot = GetOrCreateChild(gameplayRoot.transform, "RunSystems");
            GameObject directorObject = GetOrCreateChild(runSystemsRoot.transform, "RunDirector");
            RunDirector runDirector = GetOrAddComponent<RunDirector>(directorObject);
            runDirector.Configure(waveConfig, enemySpawner, policeSpawner, potRegistry, outcomePresenter);

            outcomePresenter.Hide();
            EnsureEventSystem(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static RunOutcomePresenter EnsureOutcomeUi(Transform uiRoot)
        {
            Transform hudRoot = uiRoot.Find("HudRoot");
            if (hudRoot == null)
            {
                Debug.LogError("HudRoot was not found under UI. Run Phase 2 setup first.");
                return null;
            }

            GameObject panelObject = GetOrCreateChild(hudRoot, "RunOutcomePanel");
            RectTransform panelTransform = GetOrAddComponent<RectTransform>(panelObject);
            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0.5f);
            panelTransform.anchoredPosition = Vector2.zero;
            panelTransform.sizeDelta = new Vector2(620f, 340f);

            Image panelImage = GetOrAddComponent<Image>(panelObject);
            panelImage.color = new Color(0.06f, 0.08f, 0.06f, 0.96f);

            TextMeshProUGUI titleText = CreatePanelText(panelObject.transform, "TitleText", new Vector2(0f, -36f), new Vector2(560f, 64f), 42f, TextAlignmentOptions.Center, new Color(0.96f, 0.98f, 0.84f, 1f), "You Win!");
            TextMeshProUGUI bodyText = CreatePanelText(panelObject.transform, "BodyText", new Vector2(0f, -118f), new Vector2(540f, 92f), 24f, TextAlignmentOptions.Center, new Color(0.84f, 0.9f, 0.82f, 1f), "All waves cleared.");
            Button restartButton = CreateRestartButton(panelObject.transform);

            RunOutcomePresenter presenter = GetOrAddComponent<RunOutcomePresenter>(panelObject);
            presenter.ConfigureView(panelObject, titleText, bodyText, restartButton);
            panelObject.SetActive(false);
            return presenter;
        }

        private static Button CreateRestartButton(Transform parent)
        {
            GameObject buttonObject = GetOrCreateChild(parent, "RestartButton");
            RectTransform buttonTransform = GetOrAddComponent<RectTransform>(buttonObject);
            buttonTransform.anchorMin = new Vector2(0.5f, 1f);
            buttonTransform.anchorMax = new Vector2(0.5f, 1f);
            buttonTransform.pivot = new Vector2(0.5f, 1f);
            buttonTransform.anchoredPosition = new Vector2(0f, -238f);
            buttonTransform.sizeDelta = new Vector2(260f, 64f);

            Image buttonImage = GetOrAddComponent<Image>(buttonObject);
            buttonImage.color = new Color(0.18f, 0.3f, 0.18f, 1f);

            Button button = GetOrAddComponent<Button>(buttonObject);
            button.targetGraphic = buttonImage;

            TextMeshProUGUI label = CreatePanelText(buttonObject.transform, "Label", Vector2.zero, new Vector2(240f, 54f), 26f, TextAlignmentOptions.Center, new Color(0.96f, 0.98f, 0.9f, 1f), "Restart");
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = Vector2.zero;

            return button;
        }

        private static TextMeshProUGUI CreatePanelText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            string text)
        {
            GameObject textObject = GetOrCreateChild(parent, name);
            RectTransform textTransform = GetOrAddComponent<RectTransform>(textObject);
            textTransform.anchorMin = new Vector2(0.5f, 1f);
            textTransform.anchorMax = new Vector2(0.5f, 1f);
            textTransform.pivot = new Vector2(0.5f, 1f);
            textTransform.anchoredPosition = anchoredPosition;
            textTransform.sizeDelta = sizeDelta;

            TextMeshProUGUI textLabel = GetOrAddComponent<TextMeshProUGUI>(textObject);
            textLabel.fontSize = fontSize;
            textLabel.alignment = alignment;
            textLabel.color = color;
            textLabel.raycastTarget = false;
            textLabel.textWrappingMode = TextWrappingModes.Normal;
            textLabel.text = text;

            if (TMP_Settings.defaultFontAsset != null)
            {
                textLabel.font = TMP_Settings.defaultFontAsset;
            }

            return textLabel;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            GameObject eventSystemObject = FindRoot(scene, "EventSystem");
            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            }

            GetOrAddComponent<EventSystem>(eventSystemObject);
            InputSystemUIInputModule inputModule = GetOrAddComponent<InputSystemUIInputModule>(eventSystemObject);
            inputModule.AssignDefaultActions();

            StandaloneInputModule standaloneInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                Object.DestroyImmediate(standaloneInputModule);
            }
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
                Debug.LogError("TextMesh Pro essential resources could not be imported. Open Window > Text Mesh Pro > Import TMP Essential Resources, then rerun Phase 12 Level Orchestration Setup.");
                return false;
            }

            return true;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directoryPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directoryPath) || AssetDatabase.IsValidFolder(directoryPath))
            {
                return;
            }

            string[] segments = directoryPath.Split('/');
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
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
    }
}
