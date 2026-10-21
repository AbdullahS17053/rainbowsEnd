using System.IO;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    [InitializeOnLoad]
    public static class LeprechaunPlayerSetup
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string LeprechaunModelPath = "Assets/Leprechaun Rigged.fbx";
        private const string LeprechaunTexturePath = "Assets/Leprechaun Rigged.fbm/Colors.jpeg";
        private const string OutputFolder = "Assets/_Project/Art/Characters";
        private const string MaterialPath = OutputFolder + "/M_Leprechaun.mat";
        private const string ControllerPath = "Assets/_Project/Art/Animators/PlayerFeedback.controller";

        static LeprechaunPlayerSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("LeprechaunPlayerSetup_Ran", false))
                {
                    SessionState.SetBool("LeprechaunPlayerSetup_Ran", true);
                    Apply();
                }
            };
        }

        [MenuItem("Rainbows End/Setup/Apply Leprechaun Player Visual")]
        public static void Apply()
        {
            GameObject leprechaunFbx = AssetDatabase.LoadAssetAtPath<GameObject>(LeprechaunModelPath);
            if (leprechaunFbx == null)
            {
                Debug.LogError($"[LeprechaunPlayerSetup] Could not find model at {LeprechaunModelPath}");
                return;
            }

            Material material = EnsureMaterial();
            RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            // 1. Update PF_Player prefab
            UpdatePlayerPrefab(leprechaunFbx, material, animController);

            // 2. Update player in active scenes if open
            UpdateScenePlayerInstances(leprechaunFbx, material, animController);

            AssetDatabase.SaveAssets();
            Debug.Log("[LeprechaunPlayerSetup] Successfully configured Leprechaun Rigged for Player!");
        }

        private static Material EnsureMaterial()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Art");
            }
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Characters");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LeprechaunTexturePath);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "M_Leprechaun" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTexture("_MainTex", texture);
            }
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void UpdatePlayerPrefab(GameObject leprechaunFbx, Material material, RuntimeAnimatorController animController)
        {
            if (!File.Exists(PlayerPrefabPath))
            {
                Debug.LogWarning($"[LeprechaunPlayerSetup] Prefab missing at {PlayerPrefabPath}");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visualRoot = prefabRoot.transform.Find("VisualRoot");
                if (visualRoot == null)
                {
                    visualRoot = new GameObject("VisualRoot").transform;
                    visualRoot.SetParent(prefabRoot.transform, false);
                }

                // Disable 2D sprite renderer if still present on visualRoot
                SpriteRenderer sr = visualRoot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = false;
                }

                // Check for existing 3D visual or previous goblin/fairy
                Transform oldVisual = null;
                for (int i = 0; i < visualRoot.childCount; i++)
                {
                    Transform c = visualRoot.GetChild(i);
                    if (c.name.Contains("Visual") || c.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    {
                        oldVisual = c;
                        break;
                    }
                }

                // Find old weapon visuals if attached under oldVisual hand
                Transform oldHand = oldVisual != null ? FindRightHand(oldVisual) : null;
                Transform faeBoltMuzzle = oldHand?.Find("FaeBoltMuzzle");
                Transform pistolVisual = oldHand?.Find("PlayerPistolVisual");
                Transform shotgunVisual = oldHand?.Find("PlayerShotgunVisual");
                Transform assaultRifleVisual = oldHand?.Find("PlayerAssaultRifleVisual");

                // Unparent weapon visuals before destroying old visual
                if (faeBoltMuzzle != null) faeBoltMuzzle.SetParent(visualRoot, false);
                if (pistolVisual != null) pistolVisual.SetParent(visualRoot, false);
                if (shotgunVisual != null) shotgunVisual.SetParent(visualRoot, false);
                if (assaultRifleVisual != null) assaultRifleVisual.SetParent(visualRoot, false);

                if (oldVisual != null)
                {
                    Object.DestroyImmediate(oldVisual.gameObject);
                }

                // Instantiate Leprechaun
                GameObject leprechaunVisual = Object.Instantiate(leprechaunFbx, visualRoot);
                leprechaunVisual.name = "Leprechaun Visual";
                leprechaunVisual.transform.localPosition = Vector3.zero;
                // Characters stand up in XY plane facing forward (-90 on X)
                leprechaunVisual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                leprechaunVisual.transform.localScale = Vector3.one;

                // Assign material to all renderers
                SkinnedMeshRenderer[] smrs = leprechaunVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    if (material != null)
                    {
                        smr.sharedMaterial = material;
                    }
                }
                MeshRenderer[] mrs = leprechaunVisual.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in mrs)
                {
                    if (material != null)
                    {
                        mr.sharedMaterial = material;
                    }
                }

                // Configure Animator
                Animator animator = leprechaunVisual.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = leprechaunVisual.AddComponent<Animator>();
                }
                if (animController != null)
                {
                    animator.runtimeAnimatorController = animController;
                }
                animator.applyRootMotion = false;

                // Find Leprechaun right hand and reparent weapon visuals
                Transform newHand = FindRightHand(leprechaunVisual.transform);
                if (newHand != null)
                {
                    if (faeBoltMuzzle != null) faeBoltMuzzle.SetParent(newHand, false);
                    if (pistolVisual != null) pistolVisual.SetParent(newHand, false);
                    if (shotgunVisual != null) shotgunVisual.SetParent(newHand, false);
                    if (assaultRifleVisual != null) assaultRifleVisual.SetParent(newHand, false);
                }

                // Update PlayerRoot references
                PlayerRoot playerRoot = prefabRoot.GetComponent<PlayerRoot>();
                if (playerRoot != null)
                {
                    playerRoot.RefreshVisualState();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void UpdateScenePlayerInstances(GameObject leprechaunFbx, Material material, RuntimeAnimatorController animController)
        {
            PlayerRoot[] players = Object.FindObjectsByType<PlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int p = 0; p < players.Length; p++)
            {
                PlayerRoot player = players[p];
                if (player == null) continue;

                Transform visualRoot = player.transform.Find("VisualRoot");
                if (visualRoot == null) continue;

                Transform lepVisual = visualRoot.Find("Leprechaun Visual");
                if (lepVisual == null)
                {
                    for (int i = visualRoot.childCount - 1; i >= 0; i--)
                    {
                        Transform c = visualRoot.GetChild(i);
                        if (c.name.Contains("Visual") || c.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                        {
                            Object.DestroyImmediate(c.gameObject);
                        }
                    }

                    GameObject newLep = Object.Instantiate(leprechaunFbx, visualRoot);
                    newLep.name = "Leprechaun Visual";
                    newLep.transform.localPosition = Vector3.zero;
                    newLep.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    newLep.transform.localScale = Vector3.one;
                    lepVisual = newLep.transform;
                }

                if (lepVisual != null)
                {
                    SkinnedMeshRenderer[] smrs = lepVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var smr in smrs)
                    {
                        if (material != null) smr.sharedMaterial = material;
                    }
                    Animator anim = lepVisual.GetComponent<Animator>();
                    if (anim != null && animController != null)
                    {
                        anim.runtimeAnimatorController = animController;
                        anim.applyRootMotion = false;
                    }
                }

                player.RefreshVisualState();
                EditorUtility.SetDirty(player.gameObject);
            }
        }

        private static Transform FindRightHand(Transform root)
        {
            if (root == null) return null;

            Animator anim = root.GetComponent<Animator>();
            if (anim != null && anim.isHuman)
            {
                Transform bone = anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (bone != null) return bone;
            }

            return FindRecursive(root, "CC_Base_R_Hand", "Hand_R", "RightHand", "hand_r");
        }

        private static Transform FindRecursive(Transform current, params string[] names)
        {
            if (current == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                if (current.name.Equals(names[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }
            }
            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindRecursive(current.GetChild(i), names);
                if (found != null) return found;
            }
            return null;
        }
    }
}
