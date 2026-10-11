using NUnit.Framework;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.World
{
    public sealed class ArenaWallColliderBuilderTests
    {
        [Test]
        public void BuildWallColliders_CreatesSolidCollider2DAndObstacle_ForWallHierarchy()
        {
            GameObject wallRoot = new GameObject("Walls");
            GameObject wallPiece = new GameObject("SM_Prop_StoneWall_01");
            wallPiece.transform.SetParent(wallRoot.transform, false);

            BoxCollider box3D = wallPiece.AddComponent<BoxCollider>();
            box3D.size = new Vector3(5f, 1f, 0.5f);
            box3D.center = new Vector3(2.5f, 0.5f, 0f);

            try
            {
                ArenaWallColliderBuilder.BuildWallColliders();

                // Verify NavMeshObstacle with carving was added
                UnityEngine.AI.NavMeshObstacle obstacle = wallPiece.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                Assert.That(obstacle, Is.Not.Null);
                Assert.That(obstacle.carving, Is.True);

                // Verify 2D collider proxy was created under [ArenaWalls2DColliders]
                GameObject proxyRoot = GameObject.Find("[ArenaWalls2DColliders]");
                Assert.That(proxyRoot, Is.Not.Null);

                PolygonCollider2D poly = proxyRoot.GetComponentInChildren<PolygonCollider2D>();
                Assert.That(poly, Is.Not.Null);
                Assert.That(poly.isTrigger, Is.False);
                Assert.That(poly.points.Length, Is.GreaterThanOrEqualTo(3));

                CoverBlocker blocker = proxyRoot.GetComponentInChildren<CoverBlocker>();
                Assert.That(blocker, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(wallRoot);
                GameObject proxyRoot = GameObject.Find("[ArenaWalls2DColliders]");
                if (proxyRoot != null)
                {
                    Object.DestroyImmediate(proxyRoot);
                }
            }
        }
    }
}
