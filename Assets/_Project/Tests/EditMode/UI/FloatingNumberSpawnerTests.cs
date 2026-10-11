using NUnit.Framework;
using RainbowsEnd.UI;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.UI
{
    public sealed class FloatingNumberSpawnerTests
    {
        [Test]
        public void Instance_ResolvesOrCreatesInstance()
        {
            FloatingNumberSpawner instance = FloatingNumberSpawner.Instance;
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void SpawnGoldNumber_SpawnsFormattedGoldText()
        {
            GameObject spawnerObj = new GameObject("TestSpawner");
            try
            {
                FloatingNumberSpawner spawner = spawnerObj.AddComponent<FloatingNumberSpawner>();
                spawner.SpawnGoldNumber(Vector3.zero, 15, isGain: true, showSuffix: true);

                TMPro.TextMeshPro tmp = spawnerObj.GetComponentInChildren<TMPro.TextMeshPro>();
                Assert.That(tmp, Is.Not.Null);
                Assert.That(tmp.text, Is.EqualTo("+15g"));
                Assert.That(tmp.fontStyle, Is.EqualTo(TMPro.FontStyles.Bold));
            }
            finally
            {
                Object.DestroyImmediate(spawnerObj);
            }
        }
    }
}
