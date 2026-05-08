using NUnit.Framework;
using RainbowsEnd.Gameplay.Combat;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Combat
{
    public sealed class WeaponSpreadUtilityTests
    {
        [Test]
        public void CalculateSpreadOffsetDegrees_UsesMinimumSpreadWhenStandingStill()
        {
            float spread = WeaponSpreadUtility.CalculateSpreadOffsetDegrees(0f, 2f, 18f, 1f);

            Assert.That(spread, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void CalculateSpreadOffsetDegrees_UsesMaximumSpreadWhenMoving()
        {
            float spread = WeaponSpreadUtility.CalculateSpreadOffsetDegrees(1f, 2f, 18f, 0f);

            Assert.That(spread, Is.EqualTo(-18f).Within(0.0001f));
        }

        [Test]
        public void ApplySpread_RotatesDirectionByOffsetDegrees()
        {
            Vector2 direction = WeaponSpreadUtility.ApplySpread(Vector2.up, 90f);

            Assert.That(direction.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}