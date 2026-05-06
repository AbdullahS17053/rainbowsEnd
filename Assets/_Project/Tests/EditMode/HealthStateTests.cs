using NUnit.Framework;
using RainbowsEnd.Core.Common;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class HealthStateTests
    {
        [Test]
        public void Initialize_SetsCurrentHealthToMax()
        {
            HealthState healthState = new HealthState();

            healthState.Initialize(10f);

            Assert.That(healthState.MaxHealth, Is.EqualTo(10f));
            Assert.That(healthState.CurrentHealth, Is.EqualTo(10f));
            Assert.That(healthState.IsDepleted, Is.False);
        }

        [Test]
        public void ApplyDamage_ClampsToZeroAndMarksDepleted()
        {
            HealthState healthState = new HealthState();
            healthState.Initialize(5f);

            DamageResult result = healthState.ApplyDamage(8f);

            Assert.That(result.AppliedAmount, Is.EqualTo(5f));
            Assert.That(result.CurrentHealth, Is.EqualTo(0f));
            Assert.That(result.IsDepleted, Is.True);
            Assert.That(healthState.IsDepleted, Is.True);
        }

        [Test]
        public void Restore_DoesNotExceedMaxHealth()
        {
            HealthState healthState = new HealthState();
            healthState.Initialize(10f);
            healthState.ApplyDamage(6f);

            float restored = healthState.Restore(20f);

            Assert.That(restored, Is.EqualTo(6f));
            Assert.That(healthState.CurrentHealth, Is.EqualTo(10f));
            Assert.That(healthState.NormalizedHealth, Is.EqualTo(1f));
        }
    }
}