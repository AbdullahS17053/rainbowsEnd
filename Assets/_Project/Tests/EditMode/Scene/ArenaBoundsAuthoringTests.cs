using NUnit.Framework;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Scene
{
    public sealed class ArenaBoundsAuthoringTests
    {
        [Test]
        public void Clamp_KeepsPointInsideConfiguredBounds()
        {
            GameObject testObject = new GameObject("ArenaBounds");
            ArenaBoundsAuthoring bounds = testObject.AddComponent<ArenaBoundsAuthoring>();
            bounds.Configure(Vector2.zero, new Vector2(10f, 6f));

            Vector2 clamped = bounds.Clamp(new Vector2(8f, -5f));

            Assert.That(clamped, Is.EqualTo(new Vector2(5f, -3f)));

            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void Contains_ReflectsConfiguredWorldRect()
        {
            GameObject testObject = new GameObject("ArenaBounds");
            ArenaBoundsAuthoring bounds = testObject.AddComponent<ArenaBoundsAuthoring>();
            bounds.Configure(new Vector2(2f, 1f), new Vector2(4f, 4f));

            Assert.That(bounds.Contains(new Vector2(2f, 1f)), Is.True);
            Assert.That(bounds.Contains(new Vector2(5f, 1f)), Is.False);

            Object.DestroyImmediate(testObject);
        }
    }
}