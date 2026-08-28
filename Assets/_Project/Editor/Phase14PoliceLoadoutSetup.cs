using System.IO;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Systems.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class Phase14PoliceLoadoutSetup
    {
        private const string MainArenaScenePath = "Assets/_Project/Scenes/MainArena_3D.unity";
        private const string ConfigRootPath = "Assets/_Project/ScriptableObjects";
        private const string PoliceMobConfigPath = "Assets/_Project/ScriptableObjects/Defaults/SO_PoliceMob_Default.asset";
        private const string PistolConfigPath = "Assets/_Project/ScriptableObjects/Defaults/SO_Weapon_Pistol_Default.asset";
        private const string ShotgunConfigPath = "Assets/_Project/ScriptableObjects/Defaults/SO_Weapon_Shotgun_Default.asset";
        private const string PolicePrefabPath = "Assets/_Project/Prefabs/PF_Police.prefab";
        private const string ShotgunPrefabPath = "Assets/Synty/PolygonMilitary/Prefabs/Weapons/SM_Wep_Shotgun_01.prefab";
        private const string MilitaryAnimatorPath = "Assets/_Project/Art/Animators/Military.controller";

        [MenuItem("Rainbows End/Setup/Run Phase 14 Police Loadout Setup")]
        public static void Run()
        {
            EnsureFolder(ConfigRootPath, "Defaults");

            WeaponConfig pistolConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(PistolConfigPath);
            WeaponConfig shotgunConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ShotgunConfigPath);
            RuntimeAnimatorController militaryAnimator =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MilitaryAnimatorPath);

            if (pistolConfig == null || shotgunConfig == null || militaryAnimator == null)
            {
                Debug.LogError("Phase 14 requires the existing pistol, shotgun, and Military animator assets.");
                return;
            }

            PoliceMobConfig policeMobConfig = CreatePoliceMobConfig(pistolConfig, shotgunConfig);
            ConfigurePolicePrefab(militaryAnimator);
            ConfigurePoliceSpawner(policeMobConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rainbow's End Phase 14 police loadout setup completed.");
        }

        private static PoliceMobConfig CreatePoliceMobConfig(WeaponConfig pistolConfig, WeaponConfig shotgunConfig)
        {
            PoliceMobConfig config = AssetDatabase.LoadAssetAtPath<PoliceMobConfig>(PoliceMobConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PoliceMobConfig>();
                AssetDatabase.CreateAsset(config, PoliceMobConfigPath);
            }

            SerializedObject serializedObject = new SerializedObject(config);
            SetObject(serializedObject, "<PistolWeapon>k__BackingField", pistolConfig);
            SetObject(serializedObject, "<ShotgunWeapon>k__BackingField", shotgunConfig);
            SetFloat(serializedObject, "<PistolChance>k__BackingField", 0.5f);
            SetFloat(serializedObject, "<AimHoldSeconds>k__BackingField", 0.25f);
            SetFloat(serializedObject, "<RepositionSeconds>k__BackingField", 0.75f);
            SetFloat(serializedObject, "<DeathDropChance>k__BackingField", 0.33f);
            SetFloat(serializedObject, "<WeaponRewardChance>k__BackingField", 0.5f);
            SetInt(serializedObject, "<PistolDropAmmo>k__BackingField", 12);
            SetInt(serializedObject, "<ShotgunDropAmmo>k__BackingField", 6);
            SetInt(serializedObject, "<GoldDropAmount>k__BackingField", 10);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void ConfigurePolicePrefab(RuntimeAnimatorController militaryAnimator)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PolicePrefabPath);
            try
            {
                Transform hand = prefabRoot.transform.Find(
                    "VisualRoot/Character_Male_Police/Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R");
                if (hand == null)
                {
                    Debug.LogError("PF_Police weapon hand hierarchy was not found.");
                    return;
                }

                Transform pistol = hand.Find("SM_Wep_Pistol_01");
                if (pistol == null)
                {
                    Debug.LogError("PF_Police pistol mesh was not found.");
                    return;
                }

                Transform shotgun = hand.Find("SM_Wep_Shotgun_01");
                if (shotgun == null)
                {
                    GameObject shotgunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunPrefabPath);
                    if (shotgunPrefab == null)
                    {
                        Debug.LogError("The Synty shotgun prefab was not found.");
                        return;
                    }

                    GameObject shotgunInstance =
                        (GameObject)PrefabUtility.InstantiatePrefab(shotgunPrefab, hand);
                    shotgunInstance.name = "SM_Wep_Shotgun_01";
                    shotgun = shotgunInstance.transform;
                }

                pistol.gameObject.SetActive(true);
                shotgun.gameObject.SetActive(false);

                Animator animator = prefabRoot.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Debug.LogError("PF_Police character Animator was not found.");
                    return;
                }

                animator.runtimeAnimatorController = militaryAnimator;

                PoliceController controller = prefabRoot.GetComponent<PoliceController>();
                if (controller == null)
                {
                    Debug.LogError("PF_Police PoliceController was not found.");
                    return;
                }

                SerializedObject serializedObject = new SerializedObject(controller);
                SetObject(serializedObject, "pistolVisual", pistol.gameObject);
                SetObject(serializedObject, "shotgunVisual", shotgun.gameObject);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PolicePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ConfigurePoliceSpawner(PoliceMobConfig policeMobConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(MainArenaScenePath, OpenSceneMode.Single);
            PoliceSpawner spawner = FindComponentInScene<PoliceSpawner>(scene);
            PrefabPool goldPickupPool = FindComponentInSceneByName<PrefabPool>(scene, "GoldPickupPool");

            if (spawner == null)
            {
                Debug.LogError("MainArena_3D does not contain a PoliceSpawner.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(spawner);
            SetObject(serializedObject, "policeMobConfig", policeMobConfig);
            if (goldPickupPool != null)
            {
                SetObject(serializedObject, "goldPickupPool", goldPickupPool);
            }
            else
            {
                Debug.LogWarning("GoldPickupPool was not found; Phase 3 death-drop wiring will need scene setup.");
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindComponentInSceneByName<T>(Scene scene, string name) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.gameObject.name == name)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private static T CreateIfMissing<T>(string folderPath, string fileName) where T : ScriptableObject
        {
            string assetPath = Path.Combine(folderPath, fileName).Replace("\\", "/");
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static string EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }

            return folderPath;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
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

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }
    }
}
