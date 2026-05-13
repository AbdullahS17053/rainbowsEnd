using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotControllerCarryTests
    {
        private GameObject _potObject;
        private PotController _potController;
        private Rigidbody2D _body;
        private CircleCollider2D _bodyCollider;
        private PotTrailCaptureSensor _captureSensor;
        private Health _health;
        private PotConfig _potConfig;
        private GameObject _playerObject;
        private PlayerRoot _playerRoot;

        [SetUp]
        public void SetUp()
        {
            _potConfig = ScriptableObject.CreateInstance<PotConfig>();

            _potObject = new GameObject("Pot");
            _body = _potObject.AddComponent<Rigidbody2D>();
            _bodyCollider = _potObject.AddComponent<CircleCollider2D>();
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

            _playerObject = new GameObject("Player");
            _playerRoot = _playerObject.AddComponent<PlayerRoot>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_potConfig);
            Object.DestroyImmediate(_potObject);
            Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void TryPickUp_DisablesWorldInteractionAndMarksPotCarried()
        {
            bool pickedUp = _potController.TryPickUp(_playerRoot);

            Assert.That(pickedUp, Is.True);
            Assert.That(_potController.IsCarried, Is.True);
            Assert.That(_body.simulated, Is.False);
            Assert.That(_bodyCollider.enabled, Is.False);
            Assert.That(_captureSensor.enabled, Is.False);
        }

        [Test]
        public void Drop_ReenablesWorldInteractionAndMovesPot()
        {
            _potController.TryPickUp(_playerRoot);

            _potController.Drop(new Vector2(2f, 3f));

            Assert.That(_potController.IsCarried, Is.False);
            Assert.That(_body.simulated, Is.True);
            Assert.That(_bodyCollider.enabled, Is.True);
            Assert.That(_captureSensor.enabled, Is.True);
            Assert.That((Vector2)_potObject.transform.position, Is.EqualTo(new Vector2(2f, 3f)));
        }
    }
}