using NUnit.Framework;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.World
{
    public sealed class EnemyEscapeRoutingUtilityTests
    {
        [Test]
        public void CalculatePathLength_ReturnsAccurateDistance()
        {
            Vector2[] waypoints = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(3f, 4f), // distance = 5
                new Vector2(3f, 7f)  // distance = 3
            };

            float length = EnemyEscapeRoutingUtility.CalculatePathLength(waypoints);

            Assert.That(length, Is.EqualTo(8f).Within(0.001f));
        }

        [Test]
        public void CalculatePathLength_HandlesNullAndShortArrays()
        {
            Assert.That(EnemyEscapeRoutingUtility.CalculatePathLength(null), Is.EqualTo(0f));
            Assert.That(EnemyEscapeRoutingUtility.CalculatePathLength(new Vector2[0]), Is.EqualTo(0f));
            Assert.That(EnemyEscapeRoutingUtility.CalculatePathLength(new Vector2[] { Vector2.one }), Is.EqualTo(0f));
        }

        [Test]
        public void TryFindBestEscapePath_FallsBackToArenaBoundsEdge_WhenNoNavMesh()
        {
            GameObject boundsObj = new GameObject("ArenaBounds");
            ArenaBoundsAuthoring bounds = boundsObj.AddComponent<ArenaBoundsAuthoring>();
            bounds.Configure(Vector2.zero, new Vector2(20f, 20f));

            try
            {
                bool hasPath = EnemyEscapeRoutingUtility.TryFindBestEscapePath(
                    new Vector2(8f, 2f),
                    bounds,
                    out Vector2 bestExit,
                    out Vector2[] path);

                // In edit mode without baked NavMesh, TryGetPath returns false, so fallback is edge
                Assert.That(bestExit.x, Is.EqualTo(10f).Within(0.01f));
                Assert.That(bestExit.y, Is.EqualTo(2f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(boundsObj);
            }
        }
    }
}
