using NUnit.Framework;
using RainbowsEnd.Gameplay.Combat;
using UnityEngine;
using System.Collections.Generic;

namespace RainbowsEnd.Tests.EditMode.Combat
{
    public sealed class WeaponAutoFireTargetingUtilityTests
    {
        [Test]
        public void IsWithinCone_ReturnsTrueForTargetInsideConeAndRange()
        {
            bool isWithinCone = WeaponAutoFireTargetingUtility.IsWithinCone(
                Vector2.zero,
                Vector2.up,
                new Vector2(0f, 4f),
                6f,
                45f);

            Assert.That(isWithinCone, Is.True);
        }

        [Test]
        public void IsWithinCone_ReturnsFalseForTargetOutsideCone()
        {
            bool isWithinCone = WeaponAutoFireTargetingUtility.IsWithinCone(
                Vector2.zero,
                Vector2.up,
                new Vector2(4f, 1f),
                6f,
                45f);

            Assert.That(isWithinCone, Is.False);
        }

        [Test]
        public void IsWithinCone_ReturnsFalseForTargetOutsideRange()
        {
            bool isWithinCone = WeaponAutoFireTargetingUtility.IsWithinCone(
                Vector2.zero,
                Vector2.up,
                new Vector2(0f, 8f),
                6f,
                45f);

            Assert.That(isWithinCone, Is.False);
        }

        [Test]
        public void SelectNearestTargetIndex_ReturnsNearestTargetWithinCone()
        {
            List<Vector2> targetPositions = new List<Vector2>
            {
                new Vector2(0.5f, 3f),
                new Vector2(0.1f, 1.5f),
                new Vector2(3f, 0.5f)
            };

            int selectedIndex = WeaponAutoFireTargetingUtility.SelectNearestTargetIndex(
                Vector2.zero,
                Vector2.up,
                targetPositions,
                6f,
                45f);

            Assert.That(selectedIndex, Is.EqualTo(1));
        }

        [Test]
        public void SelectNearestTargetIndex_ReturnsMinusOneWhenNoTargetFitsCone()
        {
            List<Vector2> targetPositions = new List<Vector2>
            {
                new Vector2(3f, 0.5f),
                new Vector2(-3f, 0.5f)
            };

            int selectedIndex = WeaponAutoFireTargetingUtility.SelectNearestTargetIndex(
                Vector2.zero,
                Vector2.up,
                targetPositions,
                6f,
                30f);

            Assert.That(selectedIndex, Is.EqualTo(-1));
        }
    }
}