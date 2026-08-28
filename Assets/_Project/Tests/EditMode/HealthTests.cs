using NUnit.Framework;
using RainbowsEnd.Core.Common;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class HealthTests
    {
        private GameObject _healthObject;

        [TearDown]
        public void TearDown()
        {
            if (_healthObject != null)
            {
                Object.DestroyImmediate(_healthObject);
            }
        }

        [Test]
        public void Invulnerability_BlocksDamageUntilCleared()
        {
            _healthObject = new GameObject("Health");
            Health health = _healthObject.AddComponent<Health>();
            health.Initialize(10f);
            DamageInfo damage = new DamageInfo(4f, FactionId.Military, Vector2.zero);

            Assert.That(health.ApplyDamage(damage).AppliedAmount, Is.EqualTo(4f));

            health.SetInvulnerable(true);

            DamageResult blocked = health.ApplyDamage(damage);
            Assert.That(blocked.AppliedAmount, Is.EqualTo(0f));
            Assert.That(health.CurrentHealth, Is.EqualTo(6f));

            health.SetInvulnerable(false);

            Assert.That(health.ApplyDamage(damage).AppliedAmount, Is.EqualTo(4f));
            Assert.That(health.CurrentHealth, Is.EqualTo(2f));
        }
    }
}