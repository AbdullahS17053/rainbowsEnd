using RainbowsEnd.Core.Config;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Editor
{
    public static class Phase15PoliceRewardsSetup
    {
        private const string WaveConfigPath =
            "Assets/_Project/ScriptableObjects/Defaults/SO_Wave_Default.asset";

        [MenuItem("Rainbows End/Setup/Run Phase 15 Police Rewards Setup")]
        public static void Run()
        {
            WaveConfig waveConfig =
                AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
            if (waveConfig == null)
            {
                Debug.LogError($"Wave config is missing at {WaveConfigPath}.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(waveConfig);
            SerializedProperty wavesProperty = serializedObject.FindProperty("waves");
            if (wavesProperty == null || wavesProperty.arraySize < 5)
            {
                Debug.LogError("The default wave config must contain at least five waves.");
                return;
            }

            int[] policeCounts = { 0, 0, 3, 4, 5 };
            for (int index = 0; index < policeCounts.Length; index++)
            {
                SerializedProperty waveProperty =
                    wavesProperty.GetArrayElementAtIndex(index);
                SerializedProperty policeCountProperty =
                    waveProperty.FindPropertyRelative("policeCount");
                if (policeCountProperty == null)
                {
                    Debug.LogError($"Wave {index + 1} has no policeCount field.");
                    return;
                }

                policeCountProperty.intValue = policeCounts[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(waveConfig);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Rainbow's End Phase 15 police rewards setup completed. " +
                "Police counts are now 0, 0, 3, 4, 5.");
        }
    }
}
