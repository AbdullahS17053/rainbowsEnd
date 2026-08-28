using System.Reflection;
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

        [Test]
        public void OnDespawned_RestoresAuthoredFloatingVisualPosition()
        {
            GameObject segmentObject = new GameObject("RainbowTrailSegment");
            GameObject visualObject = new GameObject("FX_RainbowGlow_02");
            visualObject.transform.SetParent(segmentObject.transform, false);
            Vector3 authoredPosition = new Vector3(0.1f, -0.2f, 0.3f);
            visualObject.transform.localPosition = authoredPosition;
            RainbowTrailSegment segment = segmentObject.AddComponent<RainbowTrailSegment>();

            try
            {
                segment.Activate(null, Time.time);
                visualObject.transform.localPosition = authoredPosition + Vector3.forward;

                segment.OnDespawned();

                Assert.That(Vector3.Distance(visualObject.transform.localPosition, authoredPosition), Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(visualObject);
                Object.DestroyImmediate(segmentObject);
            }
        }

        [Test]
        public void NotifyUnicornTeleported_PreservesTrailAgeAndExistingCells()
        {
            GameObject emitterObject = new GameObject("RainbowTrailEmitter");
            RainbowTrailEmitter emitter = emitterObject.AddComponent<RainbowTrailEmitter>();
            GameObject segmentObject = new GameObject("RainbowTrailSegment");
            RainbowTrailSegment segment = segmentObject.AddComponent<RainbowTrailSegment>();
            Vector2Int cell = new Vector2Int(8, 3);

            try
            {
                segment.Activate(null, Time.time - 5f, cell);
                emitter.Configure(null);
                FieldInfo startTimeField = typeof(RainbowTrailEmitter).GetField("_trailStartTime", BindingFlags.Instance | BindingFlags.NonPublic);
                startTimeField.SetValue(emitter, Time.time - 5f);

                emitter.NotifyUnicornTeleported();

                Assert.That(emitter.TrailAgeSeconds, Is.GreaterThan(4f));
                Assert.That(RainbowTrailSegment.IsCellOccupied(cell), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(emitterObject);
                Object.DestroyImmediate(segmentObject);
            }
        }

    }
}