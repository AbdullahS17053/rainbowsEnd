using NUnit.Framework;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotTrailCaptureSensorTests
    {
        [Test]
        public void TryGetOldestOverlappingTrailTimestamp_ReturnsFalse_WhenTrailIsOutsideRadius()
        {
            GameObject sensorObject = CreateSensorObject(0.65f, Vector3.zero);
            GameObject outsideSegmentObject = CreateTrailSegmentObject(new Vector3(2f, 0f, 0f), 1f);

            try
            {
                Physics2D.SyncTransforms();

                PotTrailCaptureSensor sensor = sensorObject.GetComponent<PotTrailCaptureSensor>();
                bool hasOverlap = sensor.TryGetOldestOverlappingTrailTimestamp(out float timestampSeconds);

                Assert.That(hasOverlap, Is.False);
                Assert.That(timestampSeconds, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(sensorObject);
                Object.DestroyImmediate(outsideSegmentObject);
            }
        }

        [Test]
        public void TryGetOldestOverlappingTrailTimestamp_ReturnsOldestTimestampInsideRadius()
        {
            GameObject sensorObject = CreateSensorObject(0.65f, Vector3.zero);
            GameObject newerSegmentObject = CreateTrailSegmentObject(new Vector3(0.15f, 0f, 0f), 4f);
            GameObject olderSegmentObject = CreateTrailSegmentObject(new Vector3(-0.15f, 0f, 0f), 2f);

            try
            {
                Physics2D.SyncTransforms();

                PotTrailCaptureSensor sensor = sensorObject.GetComponent<PotTrailCaptureSensor>();
                bool hasOverlap = sensor.TryGetOldestOverlappingTrailTimestamp(out float timestampSeconds);

                Assert.That(hasOverlap, Is.True);
                Assert.That(timestampSeconds, Is.EqualTo(2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(sensorObject);
                Object.DestroyImmediate(newerSegmentObject);
                Object.DestroyImmediate(olderSegmentObject);
            }
        }

        private static GameObject CreateSensorObject(float radius, Vector3 position)
        {
            GameObject sensorObject = new GameObject("PotTrailCaptureSensor");
            sensorObject.transform.position = position;
            sensorObject.AddComponent<CircleCollider2D>().isTrigger = true;

            PotTrailCaptureSensor sensor = sensorObject.AddComponent<PotTrailCaptureSensor>();
            sensor.Configure(null, radius);
            return sensorObject;
        }

        private static GameObject CreateTrailSegmentObject(Vector3 position, float spawnTimestampSeconds)
        {
            GameObject segmentObject = new GameObject("RainbowTrailSegment");
            segmentObject.transform.position = position;
            segmentObject.AddComponent<SpriteRenderer>();

            CircleCollider2D collider = segmentObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.16f;

            RainbowTrailSegment segment = segmentObject.AddComponent<RainbowTrailSegment>();
            segment.Activate(null, spawnTimestampSeconds, 5f);
            return segmentObject;
        }
    }
}