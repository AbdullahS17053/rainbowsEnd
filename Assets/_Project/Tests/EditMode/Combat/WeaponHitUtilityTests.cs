using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Gameplay.Combat;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Combat
{
    public sealed class WeaponHitUtilityTests
    {
        [Test]
        public void TryResolveHit_IgnoresFriendlyTargets()
        {
            GameObject target = new GameObject("FriendlyTarget");
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
            target.AddComponent<FriendlyTarget>();

            bool didResolve = WeaponHitUtility.TryResolveHit(collider, null, FactionId.Player, 2f, Vector2.zero, Vector2.up, null, out WeaponHitResult _);

            Assert.That(didResolve, Is.False);

            Object.DestroyImmediate(target);
        }

        [Test]
        public void TryResolveHit_AppliesDamageToOpposingDamageable()
        {
            GameObject target = new GameObject("PoliceTarget");
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
            TestDamageable damageable = target.AddComponent<TestDamageable>();

            bool didResolve = WeaponHitUtility.TryResolveHit(collider, null, FactionId.Player, 2f, new Vector2(1f, 2f), Vector2.left, null, out WeaponHitResult hitResult);

            Assert.That(didResolve, Is.True);
            Assert.That(hitResult.HitDamageable, Is.True);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(3f));
            Assert.That(hitResult.DamageResult.AppliedAmount, Is.EqualTo(2f));

            Object.DestroyImmediate(target);
        }

        [Test]
        public void TryResolveHit_IgnoresNonDamageableTriggerVolumes()
        {
            GameObject target = new GameObject("TriggerVolume");
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            bool didResolve = WeaponHitUtility.TryResolveHit(collider, null, FactionId.Player, 2f, Vector2.zero, Vector2.up, null, out WeaponHitResult _);

            Assert.That(didResolve, Is.False);

            Object.DestroyImmediate(target);
        }

        private sealed class FriendlyTarget : MonoBehaviour, ITeamAffiliation
        {
            public FactionId FactionId => FactionId.Player;
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable, ITeamAffiliation
        {
            public float CurrentHealth { get; private set; } = 5f;

            public float MaxHealth => 5f;

            public bool IsDepleted => CurrentHealth <= 0f;

            public FactionId FactionId => FactionId.Police;

            public DamageResult ApplyDamage(in DamageInfo damageInfo)
            {
                float appliedAmount = Mathf.Min(CurrentHealth, damageInfo.Amount);
                CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
                return new DamageResult(appliedAmount, CurrentHealth, CurrentHealth / MaxHealth, IsDepleted);
            }

            public float Restore(float amount)
            {
                float previousHealth = CurrentHealth;
                CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
                return CurrentHealth - previousHealth;
            }
        }
    }
}