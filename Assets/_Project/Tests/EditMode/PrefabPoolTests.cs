using NUnit.Framework;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class PrefabPoolTests
    {
        [Test]
        public void RentAndReturn_ReusesSameInstance_AndFiresPoolableCallbacks()
        {
            GameObject host = new GameObject("PoolHost");
            PrefabPool pool = host.AddComponent<PrefabPool>();

            GameObject pooledPrefab = new GameObject("PooledPrefab");
            pooledPrefab.AddComponent<TestPoolable>();

            pool.Configure(pooledPrefab, 0);

            GameObject firstInstance = pool.Rent(new Vector3(1f, 2f, 0f), Quaternion.identity);
            TestPoolable firstPoolable = firstInstance.GetComponent<TestPoolable>();

            Assert.That(firstInstance.activeSelf, Is.True);
            Assert.That(firstPoolable.SpawnedCount, Is.EqualTo(1));

            pool.Return(firstInstance);

            Assert.That(firstInstance.activeSelf, Is.False);
            Assert.That(firstPoolable.DespawnedCount, Is.EqualTo(1));

            GameObject secondInstance = pool.Rent(Vector3.zero, Quaternion.identity);

            Assert.That(secondInstance, Is.SameAs(firstInstance));
            Assert.That(firstPoolable.SpawnedCount, Is.EqualTo(2));

            Object.DestroyImmediate(secondInstance);
            Object.DestroyImmediate(pooledPrefab);
            Object.DestroyImmediate(host);
        }

        private sealed class TestPoolable : MonoBehaviour, IPoolable
        {
            public int SpawnedCount { get; private set; }

            public int DespawnedCount { get; private set; }

            public void OnSpawned()
            {
                SpawnedCount++;
            }

            public void OnDespawned()
            {
                DespawnedCount++;
            }
        }
    }
}