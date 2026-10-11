using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RainbowsEnd.Gameplay.World
{
    /// <summary>
    /// Ensures all arena walls, cover props, and obstacles in the scene
    /// have solid 2D colliders (PolygonCollider2D / BoxCollider2D) and CoverBlocker components
    /// so the player and other 2D rigidbodies cannot pass through them.
    /// Also attaches NavMeshObstacle with carving so mobs and the unicorn route through open ways.
    /// </summary>
    public sealed class ArenaWallColliderBuilder : MonoBehaviour
    {
        private const string ProxyRootName = "[ArenaWalls2DColliders]";
        private static readonly HashSet<int> ProcessedTransforms = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            BuildWallColliders();
        }

        private void Awake()
        {
            BuildWallColliders();
        }

        public static void BuildWallColliders()
        {
            ProcessedTransforms.Clear();

            // 1. Ensure solid collider on existing CoverBlocker objects
            CoverBlocker[] existingBlockers = UnityEngine.Object.FindObjectsByType<CoverBlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existingBlockers.Length; i++)
            {
                CoverBlocker blocker = existingBlockers[i];
                if (blocker == null) continue;
                blocker.gameObject.SetActive(true);
                Collider2D col = blocker.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = false;
                }
            }

            // 2. Locate or create root container for 2D proxies with identity rotation
            GameObject collidersRoot = GameObject.Find(ProxyRootName);
            if (collidersRoot == null)
            {
                collidersRoot = new GameObject(ProxyRootName);
                collidersRoot.transform.position = Vector3.zero;
                collidersRoot.transform.rotation = Quaternion.identity;
                collidersRoot.transform.localScale = Vector3.one;
            }
            else
            {
                for (int i = collidersRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = collidersRoot.transform.GetChild(i);
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            // 3. Process all potential wall objects in the scene
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.IsValid() ? activeScene.GetRootGameObjects() : Array.Empty<GameObject>();
            for (int r = 0; r < rootObjects.Length; r++)
            {
                Transform root = rootObjects[r].transform;
                if (root.name == ProxyRootName) continue;
                ProcessPotentialWallHierarchy(root, collidersRoot.transform);
            }
        }

        private static void ProcessPotentialWallHierarchy(Transform parent, Transform collidersRoot)
        {
            if (parent == null) return;

            string nameLower = parent.name.ToLowerInvariant();
            bool isWallGroupOrPiece = nameLower.Contains("wall")
                || nameLower.Contains("fence")
                || nameLower.Contains("coverblocker")
                || nameLower.Contains("pillar_arch")
                || nameLower.Contains("barrier");

            if (isWallGroupOrPiece)
            {
                EnsureColliderOnWallTransform(parent, collidersRoot);
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                ProcessPotentialWallHierarchy(child, collidersRoot);
            }
        }

        private static void EnsureColliderOnWallTransform(Transform wallTransform, Transform collidersRoot)
        {
            if (wallTransform == null) return;
            int instanceId = wallTransform.gameObject.GetInstanceID();
            if (ProcessedTransforms.Contains(instanceId)) return;

            // If it's a container with children and no mesh renderer of its own, process children
            Renderer rend = wallTransform.GetComponent<Renderer>();
            if (rend == null && wallTransform.childCount > 0)
            {
                for (int c = 0; c < wallTransform.childCount; c++)
                {
                    EnsureColliderOnWallTransform(wallTransform.GetChild(c), collidersRoot);
                }
                return;
            }

            if (rend == null)
            {
                rend = wallTransform.GetComponentInChildren<Renderer>();
            }

            BoxCollider box3D = wallTransform.GetComponent<BoxCollider>();
            if (rend == null && box3D == null) return;

            ProcessedTransforms.Add(instanceId);

            // Add NavMeshObstacle with carving so mobs and unicorn pathfind around walls
            NavMeshObstacle obstacle = wallTransform.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = wallTransform.gameObject.AddComponent<NavMeshObstacle>();
            }
            obstacle.shape = NavMeshObstacleShape.Box;
            if (box3D != null)
            {
                obstacle.center = box3D.center;
                obstacle.size = box3D.size;
            }
            else if (rend != null)
            {
                Vector3 localCenter = wallTransform.InverseTransformPoint(rend.bounds.center);
                Vector3 localSize = wallTransform.InverseTransformVector(rend.bounds.size);
                obstacle.center = localCenter;
                obstacle.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            }
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            // Create solid 2D proxy collider under collidersRoot
            GameObject proxy = new GameObject($"Collider2D_{wallTransform.name}");
            proxy.transform.SetParent(collidersRoot, false);
            proxy.transform.position = Vector3.zero;
            proxy.transform.rotation = Quaternion.identity;
            proxy.transform.localScale = Vector3.one;

            if (box3D != null)
            {
                Vector3 c = box3D.center;
                Vector3 s = box3D.size * 0.5f;
                Vector3[] localCorners = new Vector3[8]
                {
                    c + new Vector3(-s.x, -s.y, -s.z),
                    c + new Vector3(-s.x, -s.y,  s.z),
                    c + new Vector3(-s.x,  s.y, -s.z),
                    c + new Vector3(-s.x,  s.y,  s.z),
                    c + new Vector3( s.x, -s.y, -s.z),
                    c + new Vector3( s.x, -s.y,  s.z),
                    c + new Vector3( s.x,  s.y, -s.z),
                    c + new Vector3( s.x,  s.y,  s.z)
                };

                Vector2[] pts2D = new Vector2[8];
                for (int i = 0; i < 8; i++)
                {
                    Vector3 worldP = wallTransform.TransformPoint(localCorners[i]);
                    pts2D[i] = new Vector2(worldP.x, worldP.y);
                }

                Vector2[] hull = ComputeConvexHull(pts2D);
                if (hull != null && hull.Length >= 3)
                {
                    PolygonCollider2D poly = proxy.AddComponent<PolygonCollider2D>();
                    poly.points = hull;
                    poly.isTrigger = false;
                }
                else
                {
                    CreateFallbackBoxCollider2D(proxy, rend != null ? rend.bounds : new Bounds(wallTransform.position, Vector3.one));
                }
            }
            else if (rend != null)
            {
                CreateFallbackBoxCollider2D(proxy, rend.bounds);
            }

            proxy.AddComponent<CoverBlocker>();
        }

        private static void CreateFallbackBoxCollider2D(GameObject proxy, Bounds b)
        {
            BoxCollider2D box2D = proxy.AddComponent<BoxCollider2D>();
            box2D.offset = new Vector2(b.center.x, b.center.y);
            box2D.size = new Vector2(Mathf.Max(0.35f, b.size.x), Mathf.Max(0.35f, b.size.y));
            box2D.isTrigger = false;
        }

        private static Vector2[] ComputeConvexHull(Vector2[] points)
        {
            if (points == null || points.Length <= 3) return points;

            Vector2[] pts = (Vector2[])points.Clone();
            Array.Sort(pts, (a, b) =>
            {
                int cmp = a.x.CompareTo(b.x);
                return cmp != 0 ? cmp : a.y.CompareTo(b.y);
            });

            float Cross(Vector2 o, Vector2 a, Vector2 b)
            {
                return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            }

            int n = pts.Length;
            int k = 0;
            Vector2[] hull = new Vector2[2 * n];

            // Build lower hull
            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], pts[i]) <= 0.0001f)
                {
                    k--;
                }
                hull[k++] = pts[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(hull[k - 2], hull[k - 1], pts[i]) <= 0.0001f)
                {
                    k--;
                }
                hull[k++] = pts[i];
            }

            int count = k > 1 ? k - 1 : k;
            Vector2[] result = new Vector2[count];
            Array.Copy(hull, result, count);
            return result;
        }
    }
}
