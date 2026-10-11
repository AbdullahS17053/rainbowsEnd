using NUnit.Framework;
using RainbowsEnd.Core.Common;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Combat
{
    public sealed class HealthStageFeedbackPresenterTests
    {
        [Test]
        public void ResolveHealthFlashColor_ReturnsExpectedColorsForHealthTiers()
        {
            GameObject go = new GameObject("HealthPresenterTest");
            try
            {
                Health health = go.AddComponent<Health>();
                health.Initialize(100f);

                HealthStageFeedbackPresenter presenter = go.AddComponent<HealthStageFeedbackPresenter>();
                presenter.Configure(health, (SpriteRenderer)null);

                // Full health (100%) -> Green
                Color healthyColor = presenter.ResolveHealthFlashColor();
                Assert.That(healthyColor.g, Is.GreaterThan(0.8f));
                Assert.That(healthyColor.r, Is.LessThan(0.5f));

                // 50% health -> Yellow (wounded)
                health.ApplyDamage(new DamageInfo(50f, FactionId.Neutral, Vector2.zero, null));
                Color woundedColor = presenter.ResolveHealthFlashColor();
                Assert.That(woundedColor.r, Is.GreaterThan(0.8f));
                Assert.That(woundedColor.g, Is.GreaterThan(0.8f));

                // 20% health -> Red (critical)
                health.ApplyDamage(new DamageInfo(30f, FactionId.Neutral, Vector2.zero, null));
                Color criticalColor = presenter.ResolveHealthFlashColor();
                Assert.That(criticalColor.r, Is.GreaterThan(0.8f));
                Assert.That(criticalColor.g, Is.LessThan(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DamageEvent_TriggersFlashState()
        {
            GameObject go = new GameObject("HealthPresenterDamageTest");
            try
            {
                Health health = go.AddComponent<Health>();
                health.Initialize(100f);

                GameObject child = new GameObject("MeshChild");
                child.transform.SetParent(go.transform);
                child.AddComponent<MeshRenderer>();

                HealthStageFeedbackPresenter presenter = go.AddComponent<HealthStageFeedbackPresenter>();
                presenter.Configure(health, (SpriteRenderer)null);

                Assert.That(presenter.IsFlashing, Is.False);

                health.ApplyDamage(new DamageInfo(20f, FactionId.Player, Vector2.zero, null));
                Assert.That(presenter.IsFlashing, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
