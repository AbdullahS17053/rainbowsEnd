using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Spells;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Spells
{
    public sealed class PortalRegistryTests
    {
        private GameObject _portalPrefab;
        private GameObject _poolObject;
        private GameObject _registryObject;
        private PrefabPool _portalPool;
        private PortalRegistry _portalRegistry;
        private PortalConfig _portalConfig;

        [SetUp]
        public void SetUp()
        {
            _portalPrefab = new GameObject("PortalPrefab");
            _portalPrefab.AddComponent<CircleCollider2D>().isTrigger = true;
            _portalPrefab.AddComponent<PortalInstance>();

            _poolObject = new GameObject("PortalPool");
            _portalPool = _poolObject.AddComponent<PrefabPool>();
            _portalPool.Configure(_portalPrefab, 0, _poolObject.transform);

            _registryObject = new GameObject("PortalRegistry");
            _portalRegistry = _registryObject.AddComponent<PortalRegistry>();
            _portalConfig = ScriptableObject.CreateInstance<PortalConfig>();
            _portalRegistry.Configure(_portalConfig, _portalPool);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registryObject);
            Object.DestroyImmediate(_poolObject);
            Object.DestroyImmediate(_portalPrefab);
            Object.DestroyImmediate(_portalConfig);
        }

        [Test]
        public void TryPlacePortal_PairsTwoPortalsAndReplacesOldestOnThirdPlacement()
        {
            Assert.That(_portalRegistry.TryPlacePortal(new Vector2(0f, 0f), Vector2.up, null), Is.True);
            Assert.That(_portalRegistry.ActivePortals.Count, Is.EqualTo(1));

            PortalInstance firstPortal = _portalRegistry.ActivePortals[0];
            Assert.That(firstPortal.PairedPortal, Is.Null);

            Assert.That(_portalRegistry.TryPlacePortal(new Vector2(2f, 0f), Vector2.right, null), Is.True);
            Assert.That(_portalRegistry.ActivePortals.Count, Is.EqualTo(2));

            PortalInstance secondPortal = _portalRegistry.ActivePortals[1];
            Assert.That(firstPortal.PairedPortal, Is.SameAs(secondPortal));
            Assert.That(secondPortal.PairedPortal, Is.SameAs(firstPortal));

            Assert.That(_portalRegistry.TryPlacePortal(new Vector2(4f, 0f), Vector2.down, null), Is.True);
            Assert.That(_portalRegistry.ActivePortals.Count, Is.EqualTo(2));
            Assert.That(_portalRegistry.ActivePortals[0], Is.Not.SameAs(firstPortal));
            Assert.That(_portalRegistry.ActivePortals[1], Is.Not.SameAs(firstPortal));

            PortalInstance remainingFirstPortal = _portalRegistry.ActivePortals[0];
            PortalInstance remainingSecondPortal = _portalRegistry.ActivePortals[1];
            Assert.That(remainingFirstPortal.PairedPortal, Is.SameAs(remainingSecondPortal));
            Assert.That(remainingSecondPortal.PairedPortal, Is.SameAs(remainingFirstPortal));
        }
    }
}