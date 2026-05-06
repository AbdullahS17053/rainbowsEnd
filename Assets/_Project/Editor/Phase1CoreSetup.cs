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

        [MenuItem("Rainbows End/Setup/Run Phase 1 Core Setup")]
        public static void Run()
        {
            string defaultsPath = EnsureFolder(ConfigRootPath, DefaultsFolderName);

            CreateIfMissing<PlayerConfig>(defaultsPath, "SO_Player_Default.asset");
            CreateIfMissing<EnemyConfig>(defaultsPath, "SO_Enemy_Traveller_Default.asset");
            CreateIfMissing<UnicornConfig>(defaultsPath, "SO_Unicorn_Default.asset");
            CreateIfMissing<PotConfig>(defaultsPath, "SO_Pot_Default.asset");
            CreateIfMissing<WeaponConfig>(defaultsPath, "SO_Weapon_Bow_Default.asset");
            CreateIfMissing<PortalConfig>(defaultsPath, "SO_Portal_Default.asset");
            CreateIfMissing<EconomyConfig>(defaultsPath, "SO_Economy_Default.asset");
            CreateIfMissing<WaveConfig>(defaultsPath, "SO_Wave_Default.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Rainbow's End Phase 1 core assets generated.");
        }

        private static void CreateIfMissing<T>(string folderPath, string fileName) where T : ScriptableObject
        {
            string assetPath = Path.Combine(folderPath, fileName).Replace("\\", "/");
            if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
            {
                return;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
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