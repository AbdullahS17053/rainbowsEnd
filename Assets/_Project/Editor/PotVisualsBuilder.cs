using System.IO;
using RainbowsEnd.Gameplay.Pots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Editor
{
    public static class PotVisualsBuilder
    {
        private const string PotPrefabPath = "Assets/_Project/Prefabs/PF_Pot.prefab";
        private const string GoldMaterialPath = "Assets/_Project/Art/Materials/M_Pot_GoldGlint.mat";
        private const string FallbackGoldMatPath = "Assets/Synty/PolygonMilitary/Materials/Misc/PolygonMilitary_01_A_Gold.mat";
        private const string IngotPrefabPath = "Assets/PolygonFantasyKingdom/Prefabs/Items/SM_Item_Ingot_Gold_01.prefab";
        private const string OrePrefabPath = "Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Ore_Gold_01.prefab";
        private const string CoinFountainPrefabPath = "Assets/Epic Toon FX/Prefabs/Interactive/Money/Coins/GoldCoinFountain.prefab";
        private const string CoinShowerPrefabPath = "Assets/Epic Toon FX/Prefabs/Interactive/Money/Coins/GoldCoinShower.prefab";
        private const string SparkleSoftPrefabPath = "Assets/Epic Toon FX/Prefabs/Interactive/Loot/ItemSparkleSoft/ItemSparkleSoftYellow.prefab";
        private const string SparkleAreaPrefabPath = "Assets/Epic Toon FX/Prefabs/Interactive/Sparkle/SparkleArea/SparkleAreaYellow.prefab";
        private const string FireworkPrefabPath = "Assets/Epic Toon FX/Prefabs/Environment/Firework/FireworkYellowCluster.prefab";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("PotVisualsBuilt_v4", false))
                {
                    SessionState.SetBool("PotVisualsBuilt_v4", true);
                    BuildAndAssignPotVisuals();
                }
            };
        }

        [MenuItem("Rainbows End/Setup/Build Pot Visuals")]
        public static void BuildAndAssignPotVisuals()
        {
            if (!File.Exists(PotPrefabPath))
            {
                Debug.LogError($"[PotVisualsBuilder] Cannot find pot prefab at {PotPrefabPath}");
                return;
            }

            Material goldMaterial = EnsureGoldMaterial();
            GameObject ingotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IngotPrefabPath);
            GameObject orePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OrePrefabPath);
            GameObject coinFountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinFountainPrefabPath);
            GameObject coinShowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinShowerPrefabPath);
            GameObject sparkleSoftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SparkleSoftPrefabPath);
            GameObject sparkleAreaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SparkleAreaPrefabPath);
            GameObject fireworkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FireworkPrefabPath);

            // Fetch primitive meshes for dome and surface
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempSphere);

            GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh cylinderMesh = tempCylinder.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCylinder);

            GameObject root = PrefabUtility.LoadPrefabContents(PotPrefabPath);
            try
            {
                Transform visualRootTransform = root.transform.Find("VisualRoot");
                if (visualRootTransform == null)
                {
                    GameObject vr = new GameObject("VisualRoot");
                    vr.transform.SetParent(root.transform, false);
                    visualRootTransform = vr.transform;
                }

                // Clean up previous generated visual containers if any exist
                CleanUpExisting(visualRootTransform, "PartiallyFullVisual");
                CleanUpExisting(visualRootTransform, "FullVisual");
                CleanUpExisting(visualRootTransform, "OverflowingVisual");
                CleanUpExisting(visualRootTransform, "GoldPileFloorVisual");
                CleanUpExisting(root.transform, "ShimmerVFX");
                CleanUpExisting(root.transform, "FireworksVFX");
                CleanUpExisting(root.transform, "RainbowFlareVFX");
                CleanUpExisting(root.transform, "HarvestFlair");

                // 1. TIER 1: Partially Full Visual (1-9 Gold)
                GameObject partiallyFullObj = new GameObject("PartiallyFullVisual");
                partiallyFullObj.transform.SetParent(visualRootTransform, false);
                partiallyFullObj.transform.localPosition = Vector3.zero;

                GameObject discObj = new GameObject("GoldSurfaceDisc");
                discObj.transform.SetParent(partiallyFullObj.transform, false);
                discObj.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                discObj.transform.localScale = new Vector3(0.66f, 0.05f, 0.66f);
                discObj.AddComponent<MeshFilter>().sharedMesh = cylinderMesh;
                discObj.AddComponent<MeshRenderer>().sharedMaterial = goldMaterial;

                CreateIngot(partiallyFullObj.transform, new Vector3(-0.07f, 0.14f, 0.03f), new Vector3(10f, 35f, -8f), new Vector3(0.55f, 0.55f, 0.55f), ingotPrefab, goldMaterial);
                CreateIngot(partiallyFullObj.transform, new Vector3(0.08f, 0.13f, -0.05f), new Vector3(-8f, 115f, 12f), new Vector3(0.5f, 0.5f, 0.5f), ingotPrefab, goldMaterial);
                CreateIngot(partiallyFullObj.transform, new Vector3(0.02f, 0.15f, 0.08f), new Vector3(5f, 210f, 18f), new Vector3(0.52f, 0.52f, 0.52f), ingotPrefab, goldMaterial);

                if (sparkleSoftPrefab != null)
                {
                    GameObject sparkle = Object.Instantiate(sparkleSoftPrefab, partiallyFullObj.transform);
                    sparkle.name = "SparkleGlint";
                    sparkle.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                    sparkle.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                }
                partiallyFullObj.SetActive(false);

                // 2. TIER 2: Full Visual (10-24 Gold)
                GameObject fullObj = new GameObject("FullVisual");
                fullObj.transform.SetParent(visualRootTransform, false);
                fullObj.transform.localPosition = Vector3.zero;

                GameObject fullDome = new GameObject("GoldMoundDome");
                fullDome.transform.SetParent(fullObj.transform, false);
                fullDome.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                fullDome.transform.localScale = new Vector3(0.72f, 0.22f, 0.72f);
                fullDome.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
                fullDome.AddComponent<MeshRenderer>().sharedMaterial = goldMaterial;

                CreateIngot(fullObj.transform, new Vector3(-0.10f, 0.23f, 0.06f), new Vector3(12f, 40f, -15f), new Vector3(0.6f, 0.6f, 0.6f), ingotPrefab, goldMaterial);
                CreateIngot(fullObj.transform, new Vector3(0.12f, 0.24f, -0.04f), new Vector3(-15f, 130f, 10f), new Vector3(0.58f, 0.58f, 0.58f), ingotPrefab, goldMaterial);
                CreateIngot(fullObj.transform, new Vector3(0.01f, 0.27f, 0.02f), new Vector3(8f, -20f, 5f), new Vector3(0.62f, 0.62f, 0.62f), ingotPrefab, goldMaterial);
                CreateIngot(fullObj.transform, new Vector3(-0.04f, 0.22f, -0.10f), new Vector3(-5f, 85f, -20f), new Vector3(0.55f, 0.55f, 0.55f), ingotPrefab, goldMaterial);
                CreateIngot(fullObj.transform, new Vector3(0.08f, 0.22f, 0.11f), new Vector3(18f, 220f, 15f), new Vector3(0.55f, 0.55f, 0.55f), ingotPrefab, goldMaterial);

                if (orePrefab != null)
                {
                    CreateOre(fullObj.transform, new Vector3(-0.02f, 0.26f, -0.02f), new Vector3(15f, 60f, 0f), new Vector3(0.45f, 0.45f, 0.45f), orePrefab, goldMaterial);
                }

                if (sparkleSoftPrefab != null)
                {
                    GameObject sparkle = Object.Instantiate(sparkleSoftPrefab, fullObj.transform);
                    sparkle.name = "SparkleGlint";
                    sparkle.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                    sparkle.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                }
                fullObj.SetActive(false);

                // 3. TIER 3: Overflowing Visual (25+ Gold)
                GameObject overflowingObj = new GameObject("OverflowingVisual");
                overflowingObj.transform.SetParent(visualRootTransform, false);
                overflowingObj.transform.localPosition = Vector3.zero;

                GameObject overflowDome = new GameObject("GoldOverflowDome");
                overflowDome.transform.SetParent(overflowingObj.transform, false);
                overflowDome.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                overflowDome.transform.localScale = new Vector3(0.82f, 0.30f, 0.82f);
                overflowDome.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
                overflowDome.AddComponent<MeshRenderer>().sharedMaterial = goldMaterial;

                // Spillover ingots cascading over rim edges
                CreateIngot(overflowingObj.transform, new Vector3(-0.30f, 0.17f, 0.02f), new Vector3(5f, 15f, 50f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);
                CreateIngot(overflowingObj.transform, new Vector3(0.30f, 0.17f, 0.04f), new Vector3(0f, -25f, -48f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);
                CreateIngot(overflowingObj.transform, new Vector3(0.04f, 0.16f, -0.30f), new Vector3(-45f, 0f, 10f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);
                CreateIngot(overflowingObj.transform, new Vector3(-0.02f, 0.18f, 0.30f), new Vector3(45f, 180f, 5f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);

                // Heaped center crown
                CreateIngot(overflowingObj.transform, new Vector3(-0.06f, 0.30f, 0.03f), new Vector3(10f, 55f, -12f), new Vector3(0.7f, 0.7f, 0.7f), ingotPrefab, goldMaterial);
                CreateIngot(overflowingObj.transform, new Vector3(0.08f, 0.31f, -0.02f), new Vector3(-12f, 140f, 15f), new Vector3(0.68f, 0.68f, 0.68f), ingotPrefab, goldMaterial);
                CreateIngot(overflowingObj.transform, new Vector3(0f, 0.34f, 0f), new Vector3(5f, -15f, 5f), new Vector3(0.72f, 0.72f, 0.72f), ingotPrefab, goldMaterial);

                if (orePrefab != null)
                {
                    CreateOre(overflowingObj.transform, new Vector3(-0.08f, 0.28f, -0.06f), new Vector3(20f, 45f, -10f), new Vector3(0.5f, 0.5f, 0.5f), orePrefab, goldMaterial);
                    CreateOre(overflowingObj.transform, new Vector3(0.10f, 0.28f, 0.08f), new Vector3(-15f, 160f, 20f), new Vector3(0.48f, 0.48f, 0.48f), orePrefab, goldMaterial);
                }

                // Active Coin Fountain particle shower
                if (coinFountainPrefab != null)
                {
                    GameObject fountain = Object.Instantiate(coinFountainPrefab, overflowingObj.transform);
                    fountain.name = "GoldCoinFountain";
                    fountain.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                    fountain.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                }
                overflowingObj.SetActive(false);

                // 4. FLOOR PILE: Spilled gold on the ground (25+ Gold)
                GameObject floorPileObj = new GameObject("GoldPileFloorVisual");
                floorPileObj.transform.SetParent(visualRootTransform, false);
                floorPileObj.transform.localPosition = new Vector3(0f, -0.36f, 0f);

                // Ring of gold spilled on the ground
                CreateIngot(floorPileObj.transform, new Vector3(-0.42f, 0.02f, 0.15f), new Vector3(0f, 25f, 0f), new Vector3(0.7f, 0.7f, 0.7f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(-0.35f, 0.02f, -0.25f), new Vector3(0f, -60f, 0f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(0.38f, 0.02f, 0.18f), new Vector3(0f, 80f, 0f), new Vector3(0.7f, 0.7f, 0.7f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(0.40f, 0.02f, -0.15f), new Vector3(0f, -30f, 0f), new Vector3(0.6f, 0.6f, 0.6f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(0.05f, 0.02f, -0.42f), new Vector3(0f, 15f, 0f), new Vector3(0.75f, 0.75f, 0.75f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(-0.12f, 0.02f, -0.38f), new Vector3(0f, 110f, 0f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(0.15f, 0.02f, 0.40f), new Vector3(0f, -75f, 0f), new Vector3(0.6f, 0.6f, 0.6f), ingotPrefab, goldMaterial);
                CreateIngot(floorPileObj.transform, new Vector3(-0.28f, 0.02f, 0.35f), new Vector3(0f, 45f, 0f), new Vector3(0.65f, 0.65f, 0.65f), ingotPrefab, goldMaterial);

                if (sparkleSoftPrefab != null)
                {
                    GameObject floorSparkle = Object.Instantiate(sparkleSoftPrefab, floorPileObj.transform);
                    floorSparkle.name = "FloorGlint";
                    floorSparkle.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                    floorSparkle.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                floorPileObj.SetActive(false);

                // 5. AMBIENT & CELEBRATION VFX (Under Root)
                GameObject shimmerObj = new GameObject("ShimmerVFX");
                shimmerObj.transform.SetParent(root.transform, false);
                shimmerObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                if (sparkleAreaPrefab != null)
                {
                    GameObject s = Object.Instantiate(sparkleAreaPrefab, shimmerObj.transform);
                    s.name = "SparkleArea";
                    s.transform.localPosition = Vector3.zero;
                    s.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                }
                shimmerObj.SetActive(false);

                GameObject fireworksObj = new GameObject("FireworksVFX");
                fireworksObj.transform.SetParent(root.transform, false);
                fireworksObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                if (fireworkPrefab != null)
                {
                    GameObject fw = Object.Instantiate(fireworkPrefab, fireworksObj.transform);
                    fw.name = "FireworkCluster";
                    fw.transform.localPosition = Vector3.zero;
                    fw.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                }
                fireworksObj.SetActive(false);

                GameObject flareObj = BuildGoldShowerHarvestFlair(root.transform, coinShowerPrefab, sparkleSoftPrefab);

                // 6. Assign everything to PotController
                PotController potController = root.GetComponent<PotController>();
                if (potController != null)
                {
                    potController.ConfigureVisuals(
                        partiallyFullObj,
                        fullObj,
                        overflowingObj,
                        floorPileObj,
                        fireworksObj,
                        flareObj,
                        shimmerObj,
                        flareObj);

                    SerializedObject so = new SerializedObject(potController);
                    SetSerializedProperty(so, "partiallyFullVisual", partiallyFullObj);
                    SetSerializedProperty(so, "fullVisual", fullObj);
                    SetSerializedProperty(so, "overflowingVisual", overflowingObj);
                    SetSerializedProperty(so, "goldPileFloorVisual", floorPileObj);
                    SetSerializedProperty(so, "shimmerVFX", shimmerObj);
                    SetSerializedProperty(so, "fireworksVFX", fireworksObj);
                    SetSerializedProperty(so, "rainbowFlareVFX", flareObj);
                    SetSerializedProperty(so, "harvestFlair", flareObj);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PotPrefabPath);
                Debug.Log("<color=green>[PotVisualsBuilder] Successfully created and assigned 4-tier pot visuals and celebration VFX to PF_Pot.prefab!</color>");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Refresh open scene if applicable
            RefreshOpenScenePots();
        }

        private static void CreateIngot(Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale, GameObject prefab, Material fallbackMat)
        {
            GameObject ingot = new GameObject("GoldIngot");
            ingot.transform.SetParent(parent, false);
            ingot.transform.localPosition = localPos;
            ingot.transform.localEulerAngles = localRot;
            ingot.transform.localScale = localScale;

            MeshFilter mf = ingot.AddComponent<MeshFilter>();
            MeshRenderer mr = ingot.AddComponent<MeshRenderer>();

            if (prefab != null)
            {
                MeshFilter sourceMf = prefab.GetComponentInChildren<MeshFilter>();
                MeshRenderer sourceMr = prefab.GetComponentInChildren<MeshRenderer>();
                if (sourceMf != null) mf.sharedMesh = sourceMf.sharedMesh;
                if (sourceMr != null) mr.sharedMaterials = sourceMr.sharedMaterials;
            }

            if (mf.sharedMesh == null)
            {
                // Fallback cube scaled as an ingot
                GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mf.sharedMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tempCube);
                mr.sharedMaterial = fallbackMat;
                ingot.transform.localScale = Vector3.Scale(localScale, new Vector3(0.4f, 0.15f, 0.2f));
            }
        }

        private static void CreateOre(Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale, GameObject prefab, Material fallbackMat)
        {
            GameObject ore = new GameObject("GoldOre");
            ore.transform.SetParent(parent, false);
            ore.transform.localPosition = localPos;
            ore.transform.localEulerAngles = localRot;
            ore.transform.localScale = localScale;

            MeshFilter mf = ore.AddComponent<MeshFilter>();
            MeshRenderer mr = ore.AddComponent<MeshRenderer>();

            if (prefab != null)
            {
                MeshFilter sourceMf = prefab.GetComponentInChildren<MeshFilter>();
                MeshRenderer sourceMr = prefab.GetComponentInChildren<MeshRenderer>();
                if (sourceMf != null) mf.sharedMesh = sourceMf.sharedMesh;
                if (sourceMr != null) mr.sharedMaterials = sourceMr.sharedMaterials;
            }

            if (mf.sharedMesh == null)
            {
                mr.sharedMaterial = fallbackMat;
            }
        }

        private static void CleanUpExisting(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void SetSerializedProperty(SerializedObject so, string propertyName, Object targetValue)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = targetValue;
            }
        }

        private static Material EnsureGoldMaterial()
        {
            string dir = Path.GetDirectoryName(GoldMaterialPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GoldMaterialPath);
            if (mat == null)
            {
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader == null)
                {
                    urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
                if (urpLitShader == null)
                {
                    urpLitShader = Shader.Find("Standard");
                }

                mat = new Material(urpLitShader);
                mat.name = "M_Pot_GoldGlint";
                mat.color = new Color(1f, 0.85f, 0.22f, 1f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.92f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.9f, 0.65f, 0.1f) * 0.45f);
                }

                AssetDatabase.CreateAsset(mat, GoldMaterialPath);
                AssetDatabase.SaveAssets();
            }

            return mat;
        }

        private static void RefreshOpenScenePots()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                PotController[] pots = Object.FindObjectsByType<PotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (pots != null && pots.Length > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private static GameObject BuildGoldShowerHarvestFlair(
            Transform parent,
            GameObject coinShowerPrefab,
            GameObject sparkleSoftPrefab)
        {
            GameObject flareRoot = new GameObject("RainbowFlareVFX");
            flareRoot.transform.SetParent(parent, false);
            flareRoot.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            // 1. Tall and thin falling gold coin shower (from Epic Toon FX)
            if (coinShowerPrefab != null)
            {
                GameObject showerObj = Object.Instantiate(coinShowerPrefab, flareRoot.transform);
                showerObj.name = "GoldCoinShower";
                // Start tall above the pot mouth (2.8m above mouth, so 3.15m above ground)
                showerObj.transform.localPosition = new Vector3(0f, 2.8f, 0f);
                showerObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                showerObj.transform.localScale = Vector3.one;

                ParticleSystem showerPs = showerObj.GetComponent<ParticleSystem>();
                if (showerPs != null)
                {
                    var main = showerPs.main;
                    main.loop = true;
                    main.playOnAwake = true;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    // Fall from 2.8m down to pot mouth:
                    main.startSpeed = new ParticleSystem.MinMaxCurve(-4.8f, -5.6f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.52f, 0.64f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.20f);
                    main.gravityModifier = 0.5f;

                    var emission = showerPs.emission;
                    emission.enabled = true;
                    emission.rateOverTime = 38f;

                    // Thin column matching pot mouth radius (~0.26m)
                    var shape = showerPs.shape;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.26f;
                    shape.scale = new Vector3(0.52f, 0.52f, 0.1f);
                }

                // Ensure all particle systems loop and play on awake for duration of harvest
                foreach (ParticleSystem ps in showerObj.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var m = ps.main;
                    m.loop = true;
                    m.playOnAwake = true;
                }
            }

            // 2. Cauldron Mouth Golden Gleam / Impact Sparkles (where the gold falls into the pot)
            if (sparkleSoftPrefab != null)
            {
                GameObject mouthSparkles = Object.Instantiate(sparkleSoftPrefab, flareRoot.transform);
                mouthSparkles.name = "PotMouthGoldSparkle";
                mouthSparkles.transform.localPosition = Vector3.zero;
                mouthSparkles.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

                foreach (ParticleSystem ps in mouthSparkles.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var m = ps.main;
                    m.loop = true;
                    m.playOnAwake = true;
                }
            }

            flareRoot.SetActive(false);
            return flareRoot;
        }
    }
}
