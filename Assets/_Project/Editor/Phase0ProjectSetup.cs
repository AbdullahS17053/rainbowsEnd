using System.IO;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase0ProjectSetup
    {
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena.unity";
        private const string InputActionsPath = "Assets/_Project/Input/IA_Gameplay.inputactions";

        [MenuItem("Rainbows End/Setup/Run Phase 0 Bootstrap")]
        public static void Run()
        {
            EnsureScenes();
            EnsureBuildSettings();
            EnsureInputActionsAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 0 bootstrap assets generated.");
        }

        private static void EnsureScenes()
        {
            EnsureBootScene();
            EnsureMainArenaScene();
        }

        private static void EnsureBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneNames.Boot;

            GameObject bootRoot = new GameObject("BootRoot");
            bootRoot.AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, BootScenePath);
        }

        private static void EnsureMainArenaScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneNames.MainArena;

            GameObject sceneContextRoot = CreateRoot("SceneContext");
            GameObject world = CreateRoot("World");

            GameObject environment = CreateRoot("Environment");
            CreateChild(environment.transform, "Ground");
            CreateChild(environment.transform, "Cover");
            CreateChild(environment.transform, "Concealment");
            CreateChild(environment.transform, "Props");

            GameObject gameplayRoot = CreateRoot("GameplayRoot");
            GameObject spawnPoints = CreateRoot("SpawnPoints");
            GameObject ui = CreateRoot("UI");

            GameObject cameraRoot = CreateRoot("CameraRoot");
            Camera cameraComponent = cameraRoot.AddComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 7f;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.11f, 0.13f, 0.15f, 1f);
            cameraRoot.transform.position = new Vector3(0f, 0f, -10f);

            ArenaSceneContext arenaSceneContext = sceneContextRoot.AddComponent<ArenaSceneContext>();
            arenaSceneContext.Configure(world.transform, environment.transform, gameplayRoot.transform, spawnPoints.transform, ui.transform, cameraComponent);

            EditorSceneManager.SaveScene(scene, MainArenaScenePath);
        }

        private static void EnsureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(MainArenaScenePath, true)
            };
        }

        private static void EnsureInputActionsAsset()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();

            InputActionMap gameplay = new InputActionMap("Gameplay");
            InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            AddMovementBindings(move);

            InputAction aim = gameplay.AddAction("Aim", InputActionType.Value, expectedControlLayout: "Vector2");
            aim.AddBinding("<Mouse>/position");
            aim.AddBinding("<Gamepad>/rightStick");

            InputAction fire = gameplay.AddAction("Fire", InputActionType.Button);
            fire.AddBinding("<Mouse>/leftButton");
            fire.AddBinding("<Gamepad>/rightTrigger");

            InputAction interact = gameplay.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            InputAction castPortal = gameplay.AddAction("CastPortal", InputActionType.Button);
            castPortal.AddBinding("<Mouse>/rightButton");
            castPortal.AddBinding("<Gamepad>/leftTrigger");

            InputAction pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            InputActionMap ui = new InputActionMap("UI");
            InputAction navigate = ui.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            AddMovementBindings(navigate);
            InputAction submit = ui.AddAction("Submit", InputActionType.Button);
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Gamepad>/buttonSouth");
            InputAction cancel = ui.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");

            asset.AddActionMap(gameplay);
            asset.AddActionMap(ui);

            string json = asset.ToJson();
            File.WriteAllText(InputActionsPath, json);
            AssetDatabase.ImportAsset(InputActionsPath);
        }

        private static void AddMovementBindings(InputAction action)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            action.AddBinding("<Gamepad>/leftStick");
        }

        private static GameObject CreateRoot(string name)
        {
            return new GameObject(name);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}