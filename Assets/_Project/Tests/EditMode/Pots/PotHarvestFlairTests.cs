using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Editor;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotHarvestFlairTests
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
        private GameObject _flairObject;

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

            _flairObject = new GameObject("RainbowFlareVFX");
            _flairObject.transform.SetParent(_potObject.transform, false);
            _flairObject.SetActive(false);

            _potController.Configure(_potConfig, _body, _bodyCollider, _health, _captureSensor, visualRenderer);
            _potController.ConfigureVisuals(null, null, null, null, null, _flairObject, null, _flairObject);

            _playerObject = new GameObject("Player");
            _playerRoot = _playerObject.AddComponent<PlayerRoot>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_potConfig != null) Object.DestroyImmediate(_potConfig);
            if (_potObject != null) Object.DestroyImmediate(_potObject);
            if (_playerObject != null) Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void Flair_IsInactiveByDefault()
        {
            Assert.That(_potController.HarvestFlair, Is.Not.Null);
            Assert.That(_potController.HarvestFlair.activeSelf, Is.False);
            Assert.That(_potController.RainbowFlareVFX.activeSelf, Is.False);
        }

        [Test]
        public void Flair_BecomesActiveAndRemainsActiveForDurationOfHarvest()
        {
            // Trigger harvest via PotHarvestState
            var harvestField = typeof(PotController).GetField("_harvestState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var harvestState = (PotHarvestState)harvestField.GetValue(_potController);

            harvestState.CaptureTrail(Time.time - 1f, 5f, 3f);
            harvestState.UpdateState(0.01f, isMoving: false, harvestCancelDelaySeconds: 0f);

            // Invoke ApplyVisualState
            var applyVisual = typeof(PotController).GetMethod("ApplyVisualState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            applyVisual.Invoke(_potController, null);

            // Flair should now be active!
            Assert.That(_potController.HarvestStatus, Is.EqualTo(PotHarvestStatus.Harvesting));
            Assert.That(_potController.HarvestFlair.activeSelf, Is.True, "Flair must shoot out and be active when harvest starts!");

            // Simulate progress during harvest: remain active
            harvestState.UpdateState(1.0f, isMoving: false, harvestCancelDelaySeconds: 0f);
            applyVisual.Invoke(_potController, null);
            Assert.That(_potController.HarvestFlair.activeSelf, Is.True, "Flair must remain active during harvest!");

            harvestState.UpdateState(1.0f, isMoving: false, harvestCancelDelaySeconds: 0f);
            applyVisual.Invoke(_potController, null);
            Assert.That(_potController.HarvestFlair.activeSelf, Is.True, "Flair must remain active while harvest continues!");

            // When harvest duration expires:
            harvestState.UpdateState(2.0f, isMoving: false, harvestCancelDelaySeconds: 0f);
            applyVisual.Invoke(_potController, null);
            Assert.That(_potController.HarvestStatus, Is.EqualTo(PotHarvestStatus.Idle));
            Assert.That(_potController.HarvestFlair.activeSelf, Is.False, "Flair must deactivate when harvest finishes!");
        }

        [Test]
        public void Flair_DeactivatesWhenPotIsPickedUpDuringHarvest()
        {
            var harvestField = typeof(PotController).GetField("_harvestState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var harvestState = (PotHarvestState)harvestField.GetValue(_potController);
            harvestState.CaptureTrail(Time.time - 1f, 5f, 5f);
            harvestState.UpdateState(0.01f, isMoving: false, harvestCancelDelaySeconds: 0f);

            var applyVisual = typeof(PotController).GetMethod("ApplyVisualState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            applyVisual.Invoke(_potController, null);
            Assert.That(_potController.HarvestFlair.activeSelf, Is.True);

            // Picking up the pot forces harvest to stop and disables flair
            _potController.TryPickUp(_playerRoot);
            Assert.That(_potController.HarvestFlair.activeSelf, Is.False);
            Assert.That(_potController.HarvestStatus, Is.EqualTo(PotHarvestStatus.Idle));
        }

        [Test]
        public void PotVisualsBuilder_BuildsGoldFallingHarvestFlairOnPrefab()
        {
            // Execute PotVisualsBuilder
            PotVisualsBuilder.BuildAndAssignPotVisuals();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PF_Pot.prefab");
            Assert.That(prefab, Is.Not.Null);

            PotController controller = prefab.GetComponent<PotController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.RainbowFlareVFX, Is.Not.Null, "RainbowFlareVFX should be assigned on prefab");
            Assert.That(controller.HarvestFlair, Is.Not.Null, "HarvestFlair should be assigned on prefab");

            // Check children of RainbowFlareVFX: should have falling gold coins from Epic Toon FX
            Transform flairTransform = controller.RainbowFlareVFX.transform;
            Transform coinShower = flairTransform.Find("GoldCoinShower");
            Assert.That(coinShower, Is.Not.Null, "Should contain GoldCoinShower from Epic Toon FX");

            ParticleSystem ps = coinShower.GetComponent<ParticleSystem>();
            Assert.That(ps, Is.Not.Null);
            Assert.That(ps.main.loop, Is.True, "Gold coin shower must loop to remain for duration of harvest");

            // Verify tall and thin configuration
            Assert.That(coinShower.localPosition.y, Is.GreaterThan(2.0f), "Shower emitter must be tall above pot mouth");
            Assert.That(ps.shape.radius, Is.LessThanOrEqualTo(0.35f), "Shower shape must be thin to fall into pot mouth");
        }
    }
}
