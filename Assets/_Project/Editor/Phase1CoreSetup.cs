using System.IO;
using RainbowsEnd.Core.Config;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Editor
{
    public static class Phase1CoreSetup
    {
        private const string ConfigRootPath = "Assets/_Project/ScriptableObjects";
        private const string DefaultsFolderName = "Defaults";
        private const string SettingsRootPath = "Assets/_Project/Settings";
        private const string ResourcesFolderName = "Resources";
        private const string ProjectConfigRegistryAssetName = "ProjectConfigRegistry.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 1 Core Setup")]
        public static void Run()
        {
            string defaultsPath = EnsureFolder(ConfigRootPath, DefaultsFolderName);
            string resourcesPath = EnsureFolder(SettingsRootPath, ResourcesFolderName);

            PlayerConfig playerConfig = CreateIfMissing<PlayerConfig>(defaultsPath, "SO_Player_Default.asset");
            EnemyConfig travellerConfig = CreateIfMissing<EnemyConfig>(defaultsPath, "SO_Enemy_Traveller_Default.asset");
            UnicornConfig unicornConfig = CreateIfMissing<UnicornConfig>(defaultsPath, "SO_Unicorn_Default.asset");
            PotConfig potConfig = CreateIfMissing<PotConfig>(defaultsPath, "SO_Pot_Default.asset");
            WeaponConfig weaponConfig = CreateIfMissing<WeaponConfig>(defaultsPath, "SO_Weapon_Bow_Default.asset");
            PortalConfig portalConfig = CreateIfMissing<PortalConfig>(defaultsPath, "SO_Portal_Default.asset");
            EconomyConfig economyConfig = CreateIfMissing<EconomyConfig>(defaultsPath, "SO_Economy_Default.asset");
            WaveConfig waveConfig = CreateIfMissing<WaveConfig>(defaultsPath, "SO_Wave_Default.asset");

            ProjectConfigRegistry projectConfigRegistry = CreateIfMissing<ProjectConfigRegistry>(resourcesPath, ProjectConfigRegistryAssetName);
            projectConfigRegistry.Configure(
                playerConfig,
                travellerConfig,
                unicornConfig,
                potConfig,
                weaponConfig,
                portalConfig,
                economyConfig,
                waveConfig);
            EditorUtility.SetDirty(projectConfigRegistry);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 1 core assets generated.");
        }

        private static T CreateIfMissing<T>(string folderPath, string fileName) where T : ScriptableObject
        {
            string assetPath = Path.Combine(folderPath, fileName).Replace("\\", "/");
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existingAsset != null)
            {
                return existingAsset;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
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
    }
}