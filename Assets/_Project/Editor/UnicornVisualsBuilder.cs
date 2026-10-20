using System.Collections.Generic;
using System.IO;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Editor
{
    /// <summary>
    /// Turns the Palomino horse inside PF_Unicorn into a unicorn: pearl-white coat,
    /// rainbow mane and tail, and a twisted horn on the head bone. Visual only.
    /// </summary>
    public static class UnicornVisualsBuilder
    {
        private const string UnicornPrefabPath = "Assets/_Project/Prefabs/PF_Unicorn.prefab";
        private const string SourceAtlasPath = "Assets/SimpleFarmAnimals/Textures/SimpleAnimalsFarm.png";
        private const string SourceMaterialPath = "Assets/SimpleFarmAnimals/Materials/SimpleAnimalsFarm.mat";
        private const string HorseRendererName = "SA_Animal_Horse_Palomino";
        private const string HeadBoneName = "Horse_Rig_Head_TopSHJnt";

        private const string OutputFolder = "Assets/_Project/Art/Unicorn";
        private const string TexturePath = OutputFolder + "/T_Unicorn.png";
        private const string MaterialPath = OutputFolder + "/M_Unicorn.mat";
        private const string BodyMeshPath = OutputFolder + "/SM_Unicorn_Body.asset";
        private const string HornMeshPath = OutputFolder + "/SM_Unicorn_Horn.asset";

        private const int TexSize = 256;
        private const int CellSize = 16;
        private const int RainbowRowY = 64;   // 3 shade rows of 16px
        private const int HornIvoryRowY = 128;
        private const int HornGoldRowY = 144;

        private static readonly Color[] RainbowBands =
        {
            new Color(0.94f, 0.22f, 0.27f),
            new Color(1.00f, 0.56f, 0.16f),
            new Color(1.00f, 0.88f, 0.26f),
            new Color(0.32f, 0.80f, 0.38f),
            new Color(0.22f, 0.56f, 0.96f),
            new Color(0.58f, 0.32f, 0.88f)
        };

        private static readonly float[] RainbowShades = { 0.8f, 0.9f, 1f };

        private static readonly Color PearlShadow = new Color(0.88f, 0.88f, 0.95f);
        private static readonly Color PearlLight = new Color(1.00f, 1.00f, 1.00f);
        private static readonly Color MuzzleColor = new Color(0.55f, 0.53f, 0.60f);

        // Mesh-space landmarks of SA_Animal_Horse (bind pose, y up, +z = nose).
        private const float HeadStartZ = 16.5f;  // neck meets head; forelock is in front of this
        private const float WithersZ = 5.5f;
        private const float TailRootY = 25f;
        private const float TailTipY = 4.5f;
        private static readonly Vector3 HornBase = new Vector3(0f, 33.0f, 20.6f);
        private const float HornTiltDegrees = 40f;

        private enum Part { Body, Muzzle, Hoof, Eye, Mane, Tail }

        [MenuItem("Rainbows End/Setup/Build Unicorn Visuals")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Unicorn");
            }

            var atlas = new Texture2D(2, 2);
            atlas.LoadImage(File.ReadAllBytes(SourceAtlasPath));

            GameObject root = PrefabUtility.LoadPrefabContents(UnicornPrefabPath);
            try
            {
                SkinnedMeshRenderer horse = FindRenderer(root);
                if (horse == null)
                {
                    Debug.LogError($"[UnicornVisualsBuilder] {HorseRendererName} not found in {UnicornPrefabPath}");
                    return;
                }

                // Always rebuild from the original FBX mesh so re-running doesn't stack edits.
                Mesh sourceMesh = LoadFbxMesh(HorseRendererName) ?? horse.sharedMesh;

                var flatColors = new List<Color>();
                Mesh bodyMesh = BuildBodyMesh(sourceMesh, atlas, flatColors);
                Texture2D texture = BuildTexture(flatColors);
                Material material = EnsureMaterial(texture);
                bodyMesh = SaveMesh(bodyMesh, BodyMeshPath);
                Mesh hornMesh = SaveMesh(BuildHornMesh(), HornMeshPath);

                horse.sharedMesh = bodyMesh;
                horse.sharedMaterial = material;

                Renderer horn = BuildHorn(root, horse, hornMesh, material);
                UnicornController controller = root.GetComponent<UnicornController>();
                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    so.FindProperty("hornRenderer").objectReferenceValue = horn;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, UnicornPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UnicornVisualsBuilder] PF_Unicorn now uses unicorn visuals ({bodyMesh.vertexCount} body verts).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                Object.DestroyImmediate(atlas);
            }
        }

        private static SkinnedMeshRenderer FindRenderer(GameObject root)
        {
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.name == HorseRendererName)
                {
                    return smr;
                }
            }
            return null;
        }

        private static Mesh LoadFbxMesh(string meshName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/SimpleFarmAnimals/Models/SA_Animal_Horse.fbx"))
            {
                if (asset is Mesh mesh && mesh.name == meshName)
                {
                    return mesh;
                }
            }
            return null;
        }

        // ---------------------------------------------------------------- body

        private static Mesh BuildBodyMesh(Mesh src, Texture2D atlas, List<Color> flatColors)
        {
            Vector3[] verts = src.vertices;
            Vector3[] normals = src.normals;
            Vector4[] tangents = src.tangents;
            Color[] colors = src.colors;
            Vector2[] uvs = src.uv;
            BoneWeight[] weights = src.boneWeights;
            int[] tris = src.triangles;

            // Classify every triangle by the atlas colour it sampled.
            int triCount = tris.Length / 3;
            var triColor = new Color[triCount];
            float bodyMin = float.MaxValue, bodyMax = float.MinValue;
            var greyLums = new SortedSet<float>();
            var triPart = new Part[triCount];
            for (int t = 0; t < triCount; t++)
            {
                Vector2 c = (uvs[tris[t * 3]] + uvs[tris[t * 3 + 1]] + uvs[tris[t * 3 + 2]]) / 3f;
                Color col = atlas.GetPixelBilinear(c.x, c.y);
                triColor[t] = col;
                triPart[t] = Classify(col, verts[tris[t * 3]]);
                float lum = col.grayscale;
                if (triPart[t] == Part.Body)
                {
                    bodyMin = Mathf.Min(bodyMin, lum);
                    bodyMax = Mathf.Max(bodyMax, lum);
                }
                else if (triPart[t] == Part.Mane || triPart[t] == Part.Tail)
                {
                    greyLums.Add(Mathf.Round(lum * 100f) / 100f);
                }
            }
            var greyLevels = new List<float>(greyLums);

            var flatCellIndex = new Dictionary<Color, int>();
            var remap = new Dictionary<(int, int), int>();
            var outVerts = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outTangents = new List<Vector4>();
            var outColors = new List<Color>();
            var outUvs = new List<Vector2>();
            var outWeights = new List<BoneWeight>();
            var outTris = new int[tris.Length];

            for (int t = 0; t < triCount; t++)
            {
                Part part = triPart[t];
                Color original = triColor[t];
                int cellKey;
                Color flat = Color.clear;
                int shade = 0;

                switch (part)
                {
                    case Part.Body:
                        float f = Mathf.InverseLerp(bodyMin, bodyMax, original.grayscale);
                        flat = Color.Lerp(PearlShadow, PearlLight, Mathf.SmoothStep(0f, 1f, f));
                        break;
                    case Part.Muzzle:
                        flat = MuzzleColor;
                        break;
                    default:
                        flat = original;
                        break;
                }

                if (part == Part.Mane || part == Part.Tail)
                {
                    float lum = Mathf.Round(original.grayscale * 100f) / 100f;
                    int level = greyLevels.IndexOf(lum);
                    shade = greyLevels.Count <= 1 ? 2 : Mathf.RoundToInt(level * 2f / (greyLevels.Count - 1));
                    cellKey = -1 - shade - (part == Part.Tail ? 10 : 0);
                }
                else
                {
                    flat.a = 1f;
                    if (!flatCellIndex.TryGetValue(flat, out cellKey))
                    {
                        cellKey = flatColors.Count;
                        flatCellIndex[flat] = cellKey;
                        flatColors.Add(flat);
                    }
                }

                for (int k = 0; k < 3; k++)
                {
                    int vi = tris[t * 3 + k];
                    if (!remap.TryGetValue((vi, cellKey), out int ni))
                    {
                        ni = outVerts.Count;
                        remap[(vi, cellKey)] = ni;
                        outVerts.Add(verts[vi]);
                        outNormals.Add(normals[vi]);
                        if (tangents.Length > 0) outTangents.Add(tangents[vi]);
                        if (colors.Length > 0) outColors.Add(colors[vi]);
                        outWeights.Add(weights[vi]);
                        outUvs.Add(cellKey >= 0
                            ? CellUv(cellKey)
                            : RainbowUv(RainbowParam(part, verts[vi]), shade));
                    }
                    outTris[t * 3 + k] = ni;
                }
            }

            var lockBuilder = new LockBuilder(verts, weights, triPart, tris);
            lockBuilder.AddMane();
            lockBuilder.AddTail();

            var allTris = new List<int>(outTris);
            foreach (LockVertex lv in lockBuilder.Vertices)
            {
                allTris.Add(outVerts.Count);
                outVerts.Add(lv.Position);
                outNormals.Add(lv.Normal);
                if (outTangents.Count > 0) outTangents.Add(new Vector4(1f, 0f, 0f, 1f));
                if (outColors.Count > 0) outColors.Add(Color.white);
                outWeights.Add(lv.Weight);
                outUvs.Add(RainbowUv(lv.Rainbow, 2));
            }

            var mesh = new Mesh { name = "SM_Unicorn_Body" };
            mesh.SetVertices(outVerts);
            mesh.SetNormals(outNormals);
            if (outTangents.Count > 0) mesh.SetTangents(outTangents);
            if (outColors.Count > 0) mesh.SetColors(outColors);
            mesh.SetUVs(0, outUvs);
            mesh.boneWeights = outWeights.ToArray();
            mesh.bindposes = src.bindposes;
            mesh.SetTriangles(allTris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Part Classify(Color col, Vector3 anyVertex)
        {
            Color.RGBToHSV(col, out _, out float s, out float v);
            if (v < 0.05f) return Part.Eye;
            if (s < 0.12f)
            {
                if (v < 0.4f) return Part.Hoof;
                return anyVertex.z < 0f ? Part.Tail : Part.Mane;
            }
            return v < 0.58f && s > 0.7f ? Part.Muzzle : Part.Body;
        }

        private static float RainbowParam(Part part, Vector3 p)
        {
            if (part == Part.Tail)
            {
                return Mathf.InverseLerp(TailRootY, TailTipY, p.y);
            }
            return Mathf.InverseLerp(HeadStartZ, WithersZ, p.z);
        }

        private static Vector2 CellUv(int cell)
        {
            int perRow = TexSize / CellSize;
            float x = (cell % perRow) * CellSize + CellSize * 0.5f;
            float y = (cell / perRow) * CellSize + CellSize * 0.5f;
            return new Vector2(x / TexSize, y / TexSize);
        }

        private static Vector2 RainbowUv(float t, int shade)
        {
            float u = (2f + Mathf.Clamp01(t) * (TexSize - 4f)) / TexSize;
            float v = (RainbowRowY + shade * CellSize + CellSize * 0.5f) / TexSize;
            return new Vector2(u, v);
        }

        // ---------------------------------------------------------- mane/tail

        private struct LockVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public BoneWeight Weight;
            public float Rainbow;
        }

        /// <summary>
        /// Adds flowing low-poly hair locks (tapered triangular prisms) on top of the
        /// original mesh. Each vertex copies the skin weights of the nearest original
        /// vertex so the locks follow the neck/tail animation.
        /// </summary>
        private sealed class LockBuilder
        {
            private const float ManeDrop = 7f;
            private const int ManeColumns = 16;
            private const float TailStartY = 19f;

            private readonly Vector3[] _verts;
            private readonly BoneWeight[] _weights;
            private readonly List<int> _maneVerts = new List<int>();
            private readonly List<int> _tailVerts = new List<int>();
            private readonly List<int> _bodyVerts = new List<int>();

            public readonly List<LockVertex> Vertices = new List<LockVertex>();

            public LockBuilder(Vector3[] verts, BoneWeight[] weights, Part[] triPart, int[] tris)
            {
                _verts = verts;
                _weights = weights;
                var seen = new HashSet<int>();
                for (int t = 0; t < triPart.Length; t++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int vi = tris[t * 3 + k];
                        if (!seen.Add(vi)) continue;
                        switch (triPart[t])
                        {
                            case Part.Mane: _maneVerts.Add(vi); break;
                            case Part.Tail: _tailVerts.Add(vi); break;
                            default: _bodyVerts.Add(vi); break;
                        }
                    }
                }
            }

            /// <summary>
            /// A sheet of hair on each side of the neck: crest row, a bulging middle row
            /// and a zig-zag bottom edge so it reads as separate locks.
            /// </summary>
            public void AddMane()
            {
                var skinSource = new List<int>(_maneVerts);
                skinSource.AddRange(_bodyVerts);

                List<Vector2> crestLine = BuildCrestLine();
                if (crestLine.Count < 2) return;

                var crests = new List<Vector3>();
                var ts = new List<float>();
                for (int i = 0; i <= ManeColumns; i++)
                {
                    float t = (float)i / ManeColumns;
                    float z = Mathf.Lerp(HeadStartZ, WithersZ, t);
                    crests.Add(new Vector3(0f, CrestY(crestLine, z) - 0.3f, z));
                    ts.Add(t);
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    var rows = new Vector3[3][];
                    for (int r = 0; r < 3; r++) rows[r] = new Vector3[crests.Count];
                    float lastX = 2f;
                    for (int i = 0; i < crests.Count; i++)
                    {
                        Vector3 c = crests[i];
                        float drop = ManeDrop * Mathf.Lerp(0.5f, 1f, Mathf.Sin(ts[i] * Mathf.PI)) * (i % 2 == 0 ? 1f : 0.6f);

                        float yMid = c.y - drop * 0.45f;
                        float zMid = c.z - drop * 0.2f;
                        lastX = SurfaceX(yMid, zMid, lastX);
                        float yBot = c.y - drop;
                        float zBot = c.z - drop * 0.4f;
                        float xBot = SurfaceX(yBot, zBot, lastX);

                        rows[0][i] = new Vector3(side * 0.4f, c.y + 0.2f, c.z);
                        rows[1][i] = new Vector3(side * (lastX + 0.5f), yMid, zMid);
                        rows[2][i] = new Vector3(side * (xBot + 0.2f), yBot, zBot);
                    }

                    for (int r = 0; r < 2; r++)
                    {
                        for (int i = 0; i < crests.Count - 1; i++)
                        {
                            Vector3 a = rows[r][i], b = rows[r][i + 1], c = rows[r + 1][i], d = rows[r + 1][i + 1];
                            float ra = ts[i] + r * 0.04f, rb = ts[i + 1] + r * 0.04f;
                            float rc = ts[i] + (r + 1) * 0.04f, rd = ts[i + 1] + (r + 1) * 0.04f;
                            Vector3 centroid = (a + b + c + d) * 0.25f;
                            Vector3 inside = new Vector3(0f, centroid.y, centroid.z);
                            AddTri(a, b, c, ra, rb, rc, inside, skinSource);
                            AddTri(b, d, c, rb, rd, rc, inside, skinSource);
                        }
                    }
                }
            }

            public void AddTail()
            {
                float[] xs = { -0.9f, 0.9f, 0f, -0.45f, 0.45f };
                float[] zOffsets = { 0f, 0f, -0.9f, 0.7f, 0.7f };
                Vector3 start = TailCenter(TailStartY);
                const int segments = 5;
                for (int i = 0; i < xs.Length; i++)
                {
                    var path = new List<Vector3>();
                    var outs = new List<Vector3>();
                    var widths = new List<float>();
                    var rainbow = new List<float>();
                    float tipY = TailTipY + (i % 2) * 0.8f;
                    for (int k = 0; k <= segments; k++)
                    {
                        float f = (float)k / segments;
                        float y = Mathf.Lerp(start.y, tipY, f);
                        float z = start.z + zOffsets[i] - Mathf.Sin(f * Mathf.PI) * 1.2f - f * 0.6f;
                        float x = xs[i] * (1f + 0.35f * f);
                        path.Add(new Vector3(x, y, z));
                        outs.Add(Vector3.back);
                        widths.Add(k == segments ? 0f : Mathf.Lerp(2.2f, 1.1f, f));
                        rainbow.Add(RainbowParam(Part.Tail, new Vector3(x, y, z)));
                    }
                    Sweep(path, Vector3.right, outs, widths, 1.0f, rainbow, _tailVerts);
                }
            }

            /// <summary>Top envelope of the original mane strip as (z, y) points sorted by z.</summary>
            private List<Vector2> BuildCrestLine()
            {
                var top = new SortedDictionary<float, float>();
                foreach (int vi in _maneVerts)
                {
                    Vector3 p = _verts[vi];
                    if (p.z < WithersZ - 1f || p.z > HeadStartZ + 1f) continue;
                    float key = Mathf.Round(p.z * 2f) / 2f;
                    top[key] = top.TryGetValue(key, out float y) ? Mathf.Max(y, p.y) : p.y;
                }
                var line = new List<Vector2>();
                float runningMax = float.MinValue;
                foreach (var kv in top)
                {
                    // The crest only rises toward the head; drop side vertices that dip below it.
                    runningMax = Mathf.Max(runningMax, kv.Value);
                    line.Add(new Vector2(kv.Key, runningMax));
                }
                return line;
            }

            private static float CrestY(List<Vector2> line, float z)
            {
                if (z <= line[0].x) return line[0].y;
                for (int i = 1; i < line.Count; i++)
                {
                    if (z <= line[i].x)
                    {
                        return Mathf.Lerp(line[i - 1].y, line[i].y, Mathf.InverseLerp(line[i - 1].x, line[i].x, z));
                    }
                }
                return line[line.Count - 1].y;
            }

            private float SurfaceX(float y, float z, float fallback)
            {
                float best = -1f;
                foreach (int vi in _bodyVerts)
                {
                    Vector3 p = _verts[vi];
                    if (p.z > HeadStartZ) continue; // the jaw is wider than the neck
                    if (Mathf.Abs(p.y - y) < 1.2f && Mathf.Abs(p.z - z) < 1.2f)
                    {
                        best = Mathf.Max(best, Mathf.Abs(p.x));
                    }
                }
                return best < 0f ? fallback : best;
            }

            private Vector3 TailCenter(float y)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                float bestY = float.MinValue;
                Vector3 highest = Vector3.zero;
                foreach (int vi in _tailVerts)
                {
                    Vector3 p = _verts[vi];
                    if (p.y > bestY) { bestY = p.y; highest = p; }
                    if (Mathf.Abs(p.y - y) < 1.5f) { sum += p; count++; }
                }
                return count > 0 ? sum / count : highest;
            }

            private void Sweep(List<Vector3> path, Vector3 widthDir, List<Vector3> outs, List<float> widths,
                float thickness, List<float> rainbow, List<int> skinSource)
            {
                int n = path.Count;
                var ring = new Vector3[n, 3];
                for (int k = 0; k < n; k++)
                {
                    float w = widths[k];
                    ring[k, 0] = path[k] + widthDir * (w * 0.5f);
                    ring[k, 1] = path[k] - widthDir * (w * 0.5f);
                    ring[k, 2] = path[k] + outs[k] * (thickness * w / widths[0]);
                }

                for (int k = 0; k < n - 1; k++)
                {
                    Vector3 center = (ring[k, 0] + ring[k, 1] + ring[k, 2] + ring[k + 1, 0] + ring[k + 1, 1] + ring[k + 1, 2]) / 6f;
                    for (int e = 0; e < 3; e++)
                    {
                        int e2 = (e + 1) % 3;
                        AddTri(ring[k, e], ring[k, e2], ring[k + 1, e], rainbow[k], rainbow[k], rainbow[k + 1], center, skinSource);
                        AddTri(ring[k, e2], ring[k + 1, e2], ring[k + 1, e], rainbow[k], rainbow[k + 1], rainbow[k + 1], center, skinSource);
                    }
                }

                AddTri(ring[0, 0], ring[0, 1], ring[0, 2], rainbow[0], rainbow[0], rainbow[0], path[1], skinSource);
            }

            private void AddTri(Vector3 a, Vector3 b, Vector3 c, float ra, float rb, float rc, Vector3 inside, List<int> skinSource)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-8f) return;
                if (Vector3.Dot(normal, (a + b + c) / 3f - inside) < 0f)
                {
                    (b, c) = (c, b);
                    (rb, rc) = (rc, rb);
                    normal = -normal;
                }
                normal.Normalize();
                Vertices.Add(new LockVertex { Position = a, Normal = normal, Rainbow = ra, Weight = NearestWeight(a, skinSource) });
                Vertices.Add(new LockVertex { Position = b, Normal = normal, Rainbow = rb, Weight = NearestWeight(b, skinSource) });
                Vertices.Add(new LockVertex { Position = c, Normal = normal, Rainbow = rc, Weight = NearestWeight(c, skinSource) });
            }

            private BoneWeight NearestWeight(Vector3 p, List<int> candidates)
            {
                int best = candidates[0];
                float bestDist = float.MaxValue;
                foreach (int vi in candidates)
                {
                    float d = (_verts[vi] - p).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = vi;
                    }
                }
                return _weights[best];
            }
        }

        // ------------------------------------------------------------- texture

        private static Texture2D BuildTexture(List<Color> flatColors)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            var pixels = new Color[TexSize * TexSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

            int perRow = TexSize / CellSize;
            for (int c = 0; c < flatColors.Count; c++)
            {
                FillRect(pixels, (c % perRow) * CellSize, (c / perRow) * CellSize, CellSize, CellSize, flatColors[c]);
            }

            for (int x = 0; x < TexSize; x++)
            {
                float t = Mathf.Clamp01((x - 2f) / (TexSize - 4f));
                Color band = RainbowBands[Mathf.Min(RainbowBands.Length - 1, (int)(t * RainbowBands.Length))];
                for (int shade = 0; shade < RainbowShades.Length; shade++)
                {
                    Color shaded = band * RainbowShades[shade];
                    shaded.a = 1f;
                    FillRect(pixels, x, RainbowRowY + shade * CellSize, 1, CellSize, shaded);
                }

                Color ivory = Color.Lerp(new Color(0.94f, 0.90f, 0.82f), new Color(1f, 0.99f, 0.96f), t);
                Color gold = Color.Lerp(new Color(0.97f, 0.84f, 0.55f), new Color(1f, 0.95f, 0.84f), t);
                FillRect(pixels, x, HornIvoryRowY, 1, CellSize, ivory);
                FillRect(pixels, x, HornGoldRowY, 1, CellSize, gold);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static void FillRect(Color[] pixels, int x0, int y0, int w, int h, Color color)
        {
            for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                pixels[y * TexSize + x] = color;
            }
        }

        private static Material EnsureMaterial(Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath)) { name = "M_Unicorn" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            // CopySerialized doesn't reliably replace mesh data, so copy channels explicitly.
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.normals = mesh.normals;
            existing.tangents = mesh.tangents;
            existing.colors = mesh.colors;
            existing.uv = mesh.uv;
            existing.boneWeights = mesh.boneWeights;
            existing.bindposes = mesh.bindposes;
            existing.triangles = mesh.triangles;
            existing.RecalculateBounds();
            existing.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        // ---------------------------------------------------------------- horn

        private static Mesh BuildHornMesh()
        {
            const int sides = 6;
            const int segments = 9;
            const float length = 9f;
            const float baseRadius = 1.15f;
            const float twistTurns = 0.85f;

            Vector3 Ring(int seg, int side)
            {
                float t = (float)seg / segments;
                float r = baseRadius * Mathf.Pow(1f - t, 0.85f);
                float a = (side / (float)sides + t * twistTurns) * Mathf.PI * 2f;
                return new Vector3(Mathf.Cos(a) * r, t * length, Mathf.Sin(a) * r);
            }

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void AddTri(Vector3 a, Vector3 b, Vector3 c, float ta, float tb, float tc, float row)
            {
                Vector3 centroid = (a + b + c) / 3f;
                Vector3 outward = new Vector3(centroid.x, 0f, centroid.z);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
                {
                    (b, c) = (c, b);
                    (tb, tc) = (tc, tb);
                }
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                uvs.Add(HornUv(ta, row)); uvs.Add(HornUv(tb, row)); uvs.Add(HornUv(tc, row));
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }

            for (int s = 0; s < segments; s++)
            {
                float t0 = (float)s / segments;
                float t1 = (float)(s + 1) / segments;
                for (int j = 0; j < sides; j++)
                {
                    float row = j % 2 == 0 ? HornIvoryRowY : HornGoldRowY;
                    Vector3 p00 = Ring(s, j), p01 = Ring(s, j + 1), p10 = Ring(s + 1, j), p11 = Ring(s + 1, j + 1);
                    AddTri(p00, p10, p01, t0, t1, t0, row);
                    if (s < segments - 1)
                    {
                        AddTri(p01, p10, p11, t0, t1, t1, row);
                    }
                }
            }

            var mesh = new Mesh { name = "SM_Unicorn_Horn" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2 HornUv(float t, float rowY)
        {
            return new Vector2((2f + t * (TexSize - 4f)) / TexSize, (rowY + CellSize * 0.5f) / TexSize);
        }

        private static Renderer BuildHorn(GameObject root, SkinnedMeshRenderer horse, Mesh hornMesh, Material material)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == "Horn")
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }

            Transform headBone = null;
            foreach (Transform bone in horse.bones)
            {
                if (bone != null && bone.name == HeadBoneName)
                {
                    headBone = bone;
                }
            }

            var horn = new GameObject("Horn");
            horn.transform.SetParent(horse.transform, false);
            horn.transform.localPosition = HornBase;
            horn.transform.localRotation = Quaternion.Euler(HornTiltDegrees, 0f, 0f);
            horn.transform.SetParent(headBone != null ? headBone : horse.transform, true);

            horn.AddComponent<MeshFilter>().sharedMesh = hornMesh;
            var renderer = horn.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }
    }
}
