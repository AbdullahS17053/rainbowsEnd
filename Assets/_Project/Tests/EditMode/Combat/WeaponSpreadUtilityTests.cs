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

        [Test]
        public void ShouldRenderTracer_PreservesSingleFirearmAndSelectsTwoShotgunRepresentatives()
        {
            Assert.That(WeaponVolleyUtility.ShouldRenderTracer(0, 1, true), Is.True);

            int shotgunTracerCount = 0;
            for (int index = 0; index < 6; index++)
            {
                if (WeaponVolleyUtility.ShouldRenderTracer(index, 6, true))
                {
                    shotgunTracerCount++;
                }
            }

            Assert.That(shotgunTracerCount, Is.EqualTo(2));
            Assert.That(WeaponVolleyUtility.ShouldRenderTracer(1, 6, true), Is.True);
            Assert.That(WeaponVolleyUtility.ShouldRenderTracer(4, 6, true), Is.True);
        }



        [Test]
        public void PlayerWeaponPosture_UsesStationaryAtThresholdAndHipFireAboveThreshold()
        {
            Assert.That(
                PlayerWeaponPostureUtility.ResolveRange(10f, 0f, 0.1f, 0.7f),
                Is.EqualTo(10f).Within(0.0001f));
            Assert.That(
                PlayerWeaponPostureUtility.ResolveConeDegrees(20f, 0f, 0.1f, 1.75f),
                Is.EqualTo(20f).Within(0.0001f));

            Assert.That(
                PlayerWeaponPostureUtility.ResolveRange(10f, 0.1f, 0.1f, 0.7f),
                Is.EqualTo(10f).Within(0.0001f));
            Assert.That(
                PlayerWeaponPostureUtility.ResolveConeDegrees(20f, 0.1f, 0.1f, 1.75f),
                Is.EqualTo(20f).Within(0.0001f));

            Assert.That(
                PlayerWeaponPostureUtility.ResolveRange(10f, 0.1001f, 0.1f, 0.7f),
                Is.EqualTo(7f).Within(0.0001f));
            Assert.That(
                PlayerWeaponPostureUtility.ResolveConeDegrees(20f, 0.1001f, 0.1f, 1.75f),
                Is.EqualTo(35f).Within(0.0001f));
        }
}
}