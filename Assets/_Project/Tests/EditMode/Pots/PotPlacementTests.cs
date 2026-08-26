using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotPlacementTests
    {
        private GameObject _potObject;
        private PotController _potController;
        private Rigidbody2D _body;
        private CircleCollider2D _bodyCollider;
        private PotTrailCaptureSensor _captureSensor;
        private Health _health;
        private PotConfig _potConfig;
        private GameObject _fixtureObject;

        [SetUp]
        public void SetUp()
        {
            _potConfig = ScriptableObject.CreateInstance<PotConfig>();

            _potObject = new GameObject("Pot");
            _body = _potObject.AddComponent<Rigidbody2D>();
            _bodyCollider = _potObject.AddComponent<CircleCollider2D>();
            _bodyCollider.radius = 0.5f;
            _health = _potObject.AddComponent<Health>();
            _potController = _potObject.AddComponent<PotController>();

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(_potObject.transform, false);
            SpriteRenderer visualRenderer = visualRoot.AddComponent<SpriteRenderer>();

            GameObject captureSensorRoot = new GameObject("TrailCaptureSensor");
            captureSensorRoot.transform.SetParent(_potObject.transform, false);
            captureSensorRoot.AddComponent<CircleCollider2D>();
            _captureSensor = captureSensorRoot.AddComponent<PotTrailCaptureSensor>();

            _potController.Configure(_potConfig, _body, _bodyCollider, _health, _captureSensor, visualRenderer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potConfig);
            Object.DestroyImmediate(_potObject);
            Object.DestroyImmediate(_fixtureObject);
        }

        [Test]
        public void CanDropAt_RejectsMovementBlockingCover()
        {
            _fixtureObject = new GameObject("HardBarrier");
            BoxCollider2D collider = _fixtureObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            _fixtureObject.AddComponent<CoverBlocker>();

            Physics2D.SyncTransforms();

            Assert.That(_potController.CanDropAt(Vector2.zero), Is.False);
        }

        [Test]
        public void CanDropAt_AcceptsCandidateOutsideCover()
        {
            _fixtureObject = new GameObject("HardBarrier");
            _fixtureObject.transform.position = new Vector2(3f, 0f);
            BoxCollider2D collider = _fixtureObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            _fixtureObject.AddComponent<CoverBlocker>();

            Physics2D.SyncTransforms();

            Assert.That(_potController.CanDropAt(Vector2.zero), Is.True);
        }

        [Test]
        public void CanDropAt_IgnoresTriggerWithoutMovementBlockingCover()
        {
            _fixtureObject = new GameObject("CosmeticTrigger");
            BoxCollider2D collider = _fixtureObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;

            Physics2D.SyncTransforms();

            Assert.That(_potController.CanDropAt(Vector2.zero), Is.True);
        }
    }
}
