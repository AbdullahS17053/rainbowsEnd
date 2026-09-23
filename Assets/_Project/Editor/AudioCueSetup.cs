using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RainbowsEnd.Audio;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Editor
{
    public static class AudioCueSetup
    {
        private const string CuesDirectory = "Assets/_Project/Audio/Cues";
        private const string SoundDirectory = "Assets/Epic Toon FX/Sound";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("AudioCuesSetup_v1", false))
                {
                    SessionState.SetBool("AudioCuesSetup_v1", true);
                    SetupAllAudioCues();
                }
            };
        }

        [MenuItem("Rainbows End/Audio/Setup All Audio Cues")]
        public static void SetupAllAudioCues()
        {
            if (!Directory.Exists(CuesDirectory))
            {
                Directory.CreateDirectory(CuesDirectory);
                AssetDatabase.Refresh();
            }

            Debug.Log("[AudioCueSetup] Creating and configuring AudioCueSO assets...");

            // 1. Create all AudioCueSO assets
            AudioCueSO fireCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_WeaponFire_FaeBolt.asset",
                AudioLayer.PlayerAction,
                0.85f,
                new Vector2(0.92f, 1.08f),
                false,
                "etfx_shoot_magic.wav",
                "etfx_shoot_energy01.wav",
                "etfx_shoot_energy02.wav"
            );

            AudioCueSO impactCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_WeaponImpact_Hit.asset",
                AudioLayer.EarshotGated,
                0.85f,
                new Vector2(0.90f, 1.10f),
                false,
                "etfx_target_hit.wav",
                "etfx_impact_metal01.wav",
                "etfx_impact_metal02.wav"
            );

            AudioCueSO harvestStartCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Pot_HarvestStart.asset",
                AudioLayer.EarshotGated,
                0.95f,
                new Vector2(0.95f, 1.05f),
                false,
                "etfx_explosion_sparkle2.wav",
                "etfx_explosion_sparkle3.wav",
                "etfx_explosion_poof.wav"
            );

            AudioCueSO tierUpgradeCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Pot_TierUpgrade.asset",
                AudioLayer.EarshotGated,
                1.00f,
                new Vector2(1.00f, 1.15f),
                false,
                "etfx_explosion_sparkle3.wav",
                "etfx_explosion_flash.wav"
            );

            AudioCueSO goldBankedCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Pot_GoldBanked.asset",
                AudioLayer.PlayerAction,
                0.95f,
                new Vector2(0.95f, 1.05f),
                false,
                "etfx_impact_metal03.wav",
                "etfx_pop_balloon.wav"
            );

            AudioCueSO goldPickupCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Gold_Pickup.asset",
                AudioLayer.PlayerAction,
                0.85f,
                new Vector2(1.05f, 1.25f),
                false,
                "etfx_pop_balloon.wav",
                "etfx_explosion_bubble.wav"
            );

            AudioCueSO chantCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Banking_Chant.asset",
                AudioLayer.PlayerAction,
                0.85f,
                new Vector2(0.95f, 1.05f),
                false,
                "etfx_explosion_chargeup.wav",
                "etfx_explosion_chargeup2.wav"
            );

            AudioCueSO completeCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Banking_Complete.asset",
                AudioLayer.GlobalPriority,
                1.00f,
                new Vector2(0.98f, 1.02f),
                true,
                "etfx_explosion_sparkle2.wav",
                "etfx_explosion_flash.wav"
            );

            AudioCueSO interruptedCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Banking_Interrupted.asset",
                AudioLayer.PlayerAction,
                0.90f,
                new Vector2(0.95f, 1.05f),
                false,
                "etfx_stun01.wav",
                "etfx_stun02.wav"
            );

            AudioCueSO unicornSpawnCue = CreateOrUpdateCue(
                $"{CuesDirectory}/Cue_Unicorn_Spawn.asset",
                AudioLayer.GlobalPriority,
                0.95f,
                new Vector2(0.95f, 1.05f),
                true,
                "etfx_spawn.wav",
                "etfx_explosion_mystic.wav"
            );

            AssetDatabase.SaveAssets();

            // 2. Assign to Prefabs
            AssignToPotPrefab(harvestStartCue, goldBankedCue, tierUpgradeCue);
            AssignToPlayerPrefab(fireCue, chantCue, completeCue, interruptedCue);
            AssignToBowImpactPrefab(impactCue);
            AssignToGoldPickupPrefab(goldPickupCue);
            AssignToUnicornPrefab(unicornSpawnCue);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[AudioCueSetup] All AudioCueSO assets successfully created and assigned to prefabs!</color>");
        }

        private static AudioCueSO CreateOrUpdateCue(
            string assetPath,
            AudioLayer layer,
            float volume,
            Vector2 pitchRange,
            bool bypassEarshot,
            params string[] clipFileNames)
        {
            AudioCueSO cue = AssetDatabase.LoadAssetAtPath<AudioCueSO>(assetPath);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<AudioCueSO>();
                AssetDatabase.CreateAsset(cue, assetPath);
            }

            List<AudioClip> loadedClips = new List<AudioClip>();
            foreach (string fileName in clipFileNames)
            {
                string path = $"{SoundDirectory}/{fileName}";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    loadedClips.Add(clip);
                }
                else
                {
                    Debug.LogWarning($"[AudioCueSetup] Could not load sound clip at {path}");
                }
            }

            // Set private fields via reflection
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(AudioCueSO).GetField("layer", flags)?.SetValue(cue, layer);
            typeof(AudioCueSO).GetField("clips", flags)?.SetValue(cue, loadedClips.ToArray());
            typeof(AudioCueSO).GetField("volume", flags)?.SetValue(cue, volume);
            typeof(AudioCueSO).GetField("pitchRange", flags)?.SetValue(cue, pitchRange);
            typeof(AudioCueSO).GetField("bypassEarshot", flags)?.SetValue(cue, bypassEarshot);

            // Also synchronize with SerializedObject
            SerializedObject so = new SerializedObject(cue);
            so.Update();
            SerializedProperty layerProp = so.FindProperty("layer");
            if (layerProp != null) layerProp.intValue = (int)layer;

            SerializedProperty volumeProp = so.FindProperty("volume");
            if (volumeProp != null) volumeProp.floatValue = volume;

            SerializedProperty pitchProp = so.FindProperty("pitchRange");
            if (pitchProp != null) pitchProp.vector2Value = pitchRange;

            SerializedProperty bypassProp = so.FindProperty("bypassEarshot");
            if (bypassProp != null) bypassProp.boolValue = bypassEarshot;

            SerializedProperty clipsProp = so.FindProperty("clips");
            if (clipsProp != null)
            {
                clipsProp.arraySize = loadedClips.Count;
                for (int i = 0; i < loadedClips.Count; i++)
                {
                    clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = loadedClips[i];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(cue);
            return cue;
        }

        private static void AssignToPotPrefab(AudioCueSO harvestStart, AudioCueSO goldBanked, AudioCueSO tierUpgrade)
        {
            const string potPath = "Assets/_Project/Prefabs/PF_Pot.prefab";
            if (!File.Exists(potPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(potPath);
            try
            {
                PotController pot = root.GetComponent<PotController>();
                if (pot != null)
                {
                    pot.ConfigureAudio(harvestStart, goldBanked, tierUpgrade);

                    SerializedObject so = new SerializedObject(pot);
                    SetProperty(so, "harvestStartCue", harvestStart);
                    SetProperty(so, "goldBankedCue", goldBanked);
                    SetProperty(so, "tierUpgradeCue", tierUpgrade);
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, potPath);
                    Debug.Log("[AudioCueSetup] Assigned AudioCueSO references to PF_Pot.prefab");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignToPlayerPrefab(AudioCueSO fireCue, AudioCueSO chantCue, AudioCueSO completeCue, AudioCueSO interruptedCue)
        {
            const string playerPath = "Assets/_Project/Prefabs/PF_Player.prefab";
            if (!File.Exists(playerPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                WeaponController weapon = root.GetComponentInChildren<WeaponController>(true);
                if (weapon != null)
                {
                    weapon.ConfigureFireCue(fireCue);

                    SerializedObject so = new SerializedObject(weapon);
                    SetProperty(so, "fireCue", fireCue);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PlayerPotBankingController banking = root.GetComponentInChildren<PlayerPotBankingController>(true);
                if (banking != null)
                {
                    banking.ConfigureAudioCues(chantCue, completeCue, interruptedCue);

                    SerializedObject so = new SerializedObject(banking);
                    SetProperty(so, "bankingChantCue", chantCue);
                    SetProperty(so, "bankingCompleteCue", completeCue);
                    SetProperty(so, "coughSputterCue", interruptedCue);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, playerPath);
                Debug.Log("[AudioCueSetup] Assigned AudioCueSO references to PF_Player.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignToBowImpactPrefab(AudioCueSO impactCue)
        {
            const string impactPath = "Assets/_Project/Prefabs/PF_BowImpact.prefab";
            if (!File.Exists(impactPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(impactPath);
            try
            {
                CombatImpactEffect impact = root.GetComponent<CombatImpactEffect>();
                if (impact != null)
                {
                    impact.ConfigureImpactCue(impactCue);

                    SerializedObject so = new SerializedObject(impact);
                    SetProperty(so, "impactCue", impactCue);
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, impactPath);
                    Debug.Log("[AudioCueSetup] Assigned AudioCueSO references to PF_BowImpact.prefab");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignToGoldPickupPrefab(AudioCueSO pickupCue)
        {
            const string pickupPath = "Assets/_Project/Prefabs/PF_GoldPickup.prefab";
            if (!File.Exists(pickupPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(pickupPath);
            try
            {
                GoldPickup pickup = root.GetComponent<GoldPickup>();
                if (pickup != null)
                {
                    pickup.ConfigurePickupCue(pickupCue);

                    SerializedObject so = new SerializedObject(pickup);
                    SetProperty(so, "pickupCue", pickupCue);
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, pickupPath);
                    Debug.Log("[AudioCueSetup] Assigned AudioCueSO references to PF_GoldPickup.prefab");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignToUnicornPrefab(AudioCueSO spawnCue)
        {
            const string unicornPath = "Assets/_Project/Prefabs/PF_Unicorn.prefab";
            if (!File.Exists(unicornPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(unicornPath);
            try
            {
                UnicornController unicorn = root.GetComponent<UnicornController>();
                if (unicorn != null)
                {
                    unicorn.ConfigureSpawnCue(spawnCue);

                    SerializedObject so = new SerializedObject(unicorn);
                    SetProperty(so, "spawnCue", spawnCue);
                    so.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, unicornPath);
                    Debug.Log("[AudioCueSetup] Assigned AudioCueSO references to PF_Unicorn.prefab");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetProperty(SerializedObject so, string propertyName, Object targetValue)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = targetValue;
            }
        }
    }
}
