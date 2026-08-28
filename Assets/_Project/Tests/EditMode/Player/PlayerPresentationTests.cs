using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Player
{
    public sealed class PlayerPresentationTests
    {
        private GameObject _playerObject;
        private GameObject _visualObject;
        private GameObject _zoneObject;
        private Material _visualMaterial;
        private PlayerRoot _playerRoot;
        private Health _health;
        private PlayerConfig _config;
        private SpriteRenderer _visualRenderer;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerConfig>();

            _playerObject = new GameObject("Player");
            Rigidbody2D body = _playerObject.AddComponent<Rigidbody2D>();
            _health = _playerObject.AddComponent<Health>();
            _playerRoot = _playerObject.AddComponent<PlayerRoot>();

            _visualObject = new GameObject("VisualRoot");
            _visualObject.transform.SetParent(_playerObject.transform);
            _visualRenderer = _visualObject.AddComponent<SpriteRenderer>();
            _visualMaterial = new Material(Shader.Find("Sprites/Default"));
            _visualRenderer.sharedMaterial = _visualMaterial;

            _zoneObject = new GameObject("ConcealmentZone");
            BoxCollider2D zoneCollider = _zoneObject.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = Vector2.one * 4f;
            ConcealmentZone zone = _zoneObject.AddComponent<ConcealmentZone>();
            SetPrivateField(zone, "zoneCollider", zoneCollider);

            SetPrivateField(_playerRoot, "config", _config);
            SetPrivateField(_playerRoot, "body", body);
            SetPrivateField(_playerRoot, "health", _health);
            SetPrivateField(_playerRoot, "visualRoot", _visualObject.transform);
            InvokePrivate(_playerRoot, "OnEnable");
            _health.Initialize(_config.MaxHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_visualMaterial);
            Object.DestroyImmediate(_zoneObject);
            Object.DestroyImmediate(_playerObject);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Presentation_UsesConcealmentRevealAndRecoveryPrecedence()
        {
            _playerRoot.RefreshVisualState();

            Assert.That(_playerRoot.IsConcealed, Is.True);
            Assert.That(_playerRoot.CurrentVisualOpacity, Is.EqualTo(_config.ConcealedOpacity).Within(0.001f));
            Assert.That(ReadRendererAlpha(), Is.EqualTo(_config.ConcealedOpacity).Within(0.001f));
            Assert.That(_visualMaterial.GetColor(Shader.PropertyToID("_Color")).a, Is.EqualTo(1f).Within(0.001f));

            _playerRoot.NotifyShotFired();
            Assert.That(_playerRoot.CurrentVisualOpacity, Is.EqualTo(1f).Within(0.001f));
            Assert.That(ReadRendererAlpha(), Is.EqualTo(1f).Within(0.001f));

            SetPrivateField(_playerRoot, "_concealmentRevealTimerSeconds", 0f);
            _health.ApplyDamage(new DamageInfo(20f, FactionId.Military, Vector2.zero));
            SetPrivateField(_playerRoot, "_downedTimerSeconds", 0f);
            InvokePrivate(_playerRoot, "Update");

            Assert.That(_playerRoot.IsRecovering, Is.True);
            Assert.That(_playerRoot.CurrentVisualOpacity, Is.EqualTo(_config.RecoveryVeilOpacity).Within(0.001f));
            Assert.That(ReadRendererAlpha(), Is.EqualTo(_config.RecoveryVeilOpacity).Within(0.001f));

            _playerRoot.NotifyShotFired();
            Assert.That(_playerRoot.CurrentVisualOpacity, Is.EqualTo(1f).Within(0.001f));
            Assert.That(ReadRendererAlpha(), Is.EqualTo(1f).Within(0.001f));
        }

        private float ReadRendererAlpha()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _visualRenderer.GetPropertyBlock(block);
            return block.GetColor(Shader.PropertyToID("_Color")).a;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
