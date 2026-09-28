using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Player
{
    public sealed class PlayerRootTests
    {
        private GameObject _playerObject;
        private PlayerRoot _playerRoot;
        private Health _health;
        private PlayerConfig _config;

[SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerConfig>();
            _playerObject = new GameObject("Player");
            Rigidbody2D body = _playerObject.AddComponent<Rigidbody2D>();
            CircleCollider2D bodyCollider = _playerObject.AddComponent<CircleCollider2D>();
            _health = _playerObject.AddComponent<Health>();
            _playerRoot = _playerObject.AddComponent<PlayerRoot>();
            SetPrivateField("config", _config);
            SetPrivateField("body", body);
            SetPrivateField("bodyCollider", bodyCollider);
            SetPrivateField("health", _health);
            InvokePrivate("OnEnable");
            _health.Initialize(_config.MaxHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void Depletion_StunsThenRecoversAndRestoresDamage()
        {
            DamageInfo lethalDamage = new DamageInfo(20f, FactionId.Military, Vector2.zero);

            Assert.That(_health.ApplyDamage(lethalDamage).AppliedAmount, Is.EqualTo(_config.MaxHealth));
            Assert.That(_playerRoot.IsDowned, Is.True);
            Assert.That(_playerRoot.IsDisabled, Is.True);
            Assert.That(_playerRoot.CanFire, Is.False);
            Assert.That(_playerRoot.CanUsePortals, Is.False);
            Assert.That(_playerRoot.CanInteractWithPot, Is.False);
            Assert.That(_playerRoot.CanOpenUpgradeMenu, Is.False);

            SetLifeTimer(0f);
            InvokeUpdate();

            Assert.That(_playerRoot.IsDowned, Is.False);
            Assert.That(_playerRoot.IsRecovering, Is.True);
            Assert.That(_health.CurrentHealth, Is.EqualTo(_health.MaxHealth));
            Assert.That(_health.IsInvulnerable, Is.True);
            Assert.That(_playerRoot.IsDisabled, Is.False);
            Assert.That(_playerRoot.CanFire, Is.False);
            Assert.That(_playerRoot.CanUsePortals, Is.False);
            Assert.That(_playerRoot.CanInteractWithPot, Is.True);
            Assert.That(_playerRoot.CanOpenUpgradeMenu, Is.True);

            Assert.That(_health.ApplyDamage(new DamageInfo(1f, FactionId.Unicorn, Vector2.zero)).AppliedAmount, Is.EqualTo(0f));
            _playerRoot.ApplyKnockbackAndDaze(Vector2.up, 0f, 1f);
            Assert.That(_playerRoot.IsDazed, Is.False);

            SetLifeTimer(0f);
            InvokeUpdate();

            Assert.That(_playerRoot.IsRecovering, Is.False);
            Assert.That(_health.IsInvulnerable, Is.False);
            Assert.That(_playerRoot.CanFire, Is.True);
            Assert.That(_playerRoot.CanUsePortals, Is.True);
            Assert.That(_health.ApplyDamage(new DamageInfo(1f, FactionId.Military, Vector2.zero)).AppliedAmount, Is.EqualTo(1f));
        }

        [Test]
        public void Disable_ClearsRecoveryProtection()
        {
            _health.ApplyDamage(new DamageInfo(20f, FactionId.Military, Vector2.zero));
            SetLifeTimer(0f);
            InvokeUpdate();

            Assert.That(_playerRoot.IsRecovering, Is.True);
            Assert.That(_health.IsInvulnerable, Is.True);

            _playerObject.SetActive(false);
            InvokePrivate("OnDisable");

            Assert.That(_playerRoot.IsRecovering, Is.False);
            Assert.That(_health.IsInvulnerable, Is.False);
        }

private void InvokeUpdate()
        {
            InvokePrivate("Update");
        }

private void InvokePrivate(string methodName)
        {
            MethodInfo method = typeof(PlayerRoot).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_playerRoot, null);
        }


        private void SetLifeTimer(float value)
        {
            FieldInfo timer = typeof(PlayerRoot).GetField("_downedTimerSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(timer, Is.Not.Null);
            timer.SetValue(_playerRoot, value);
        }

private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(PlayerRoot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_playerRoot, value);
        }



        [Test]
        public void Daze_ShowsStarsUntilDazeRecovers()
        {
            GameObject stars = new GameObject("Stars");
            stars.transform.SetParent(_playerObject.transform);
            stars.SetActive(false);
            SetPrivateField("stunnedStars", stars);

            _playerRoot.ApplyKnockbackAndDaze(Vector2.up, 0f, 1f);

            Assert.That(_playerRoot.IsDazed, Is.True);
            Assert.That(stars.activeSelf, Is.True);

            InvokePrivate("RecoverFromDazedState");

            Assert.That(_playerRoot.IsDazed, Is.False);
            Assert.That(stars.activeSelf, Is.False);
        }

        [Test]
        public void Damage_TriggersCombatFeedbackScreenShake()
        {
            GameObject feedbackObject = new GameObject("CombatFeedback");
            Camera camera = feedbackObject.AddComponent<Camera>();
            CombatFeedbackController feedback = feedbackObject.AddComponent<CombatFeedbackController>();

            try
            {
                DamageInfo damage = new DamageInfo(2f, FactionId.Military, Vector2.zero);
                DamageResult result = _health.ApplyDamage(damage);

                Assert.That(result.AppliedAmount, Is.EqualTo(2f));
                Assert.That(CombatFeedbackController.Instance, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(feedbackObject);
            }
        }
    }
}