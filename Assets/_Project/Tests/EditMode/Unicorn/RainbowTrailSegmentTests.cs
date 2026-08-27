using NUnit.Framework;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Unicorn
{
    public sealed class RainbowTrailSegmentTests
    {
        [SetUp]
        public void SetUp()
        {
            RainbowTrailSegment.ReturnAllToPool();
        }

        [TearDown]
        public void TearDown()
        {
            RainbowTrailSegment.ReturnAllToPool();
        }

        [Test]
        public void GetCell_UsesStableWorldBoundaries()
        {
            Assert.That(
                RainbowTrailCellUtility.GetCell(new Vector2(0.31f, -0.01f), 0.32f),
                Is.EqualTo(new Vector2Int(0, -1)));
            Assert.That(
                RainbowTrailCellUtility.GetCell(new Vector2(0.32f, 0.64f), 0.32f),
                Is.EqualTo(new Vector2Int(1, 2)));
        }

        [Test]
        public void IsCellOccupied_ClearsAfterTrailReturn()
        {
            GameObject segmentObject = new GameObject("RainbowTrailSegment");
            RainbowTrailSegment segment = segmentObject.AddComponent<RainbowTrailSegment>();
            Vector2Int cell = new Vector2Int(4, -2);

            try
            {
                segment.Activate(null, 1f, cell);

                Assert.That(RainbowTrailSegment.IsCellOccupied(cell), Is.True);

                RainbowTrailSegment.ReturnAllToPool();

                Assert.That(RainbowTrailSegment.IsCellOccupied(cell), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(segmentObject);
            }
        }
    }
}