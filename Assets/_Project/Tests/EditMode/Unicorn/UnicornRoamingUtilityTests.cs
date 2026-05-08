using NUnit.Framework;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Unicorn
{
    public sealed class UnicornRoamingUtilityTests
    {
        [Test]
        public void SelectNextBoundarySide_DoesNotRepeatSameSide()
        {
            ArenaBoundarySide nextSide = UnicornRoamingUtility.SelectNextBoundarySide(ArenaBoundarySide.Top, 0.1f);

            Assert.That(nextSide, Is.EqualTo(ArenaBoundarySide.Bottom));
        }

        [Test]
        public void SelectBoundaryTarget_ReturnsPointOnRequestedSide()
        {
            Rect bounds = new Rect(-9f, -5f, 18f, 10f);
            Vector2 target = UnicornRoamingUtility.SelectBoundaryTarget(bounds, ArenaBoundarySide.Left, 0.5f, 1f);

            Assert.That(target.x, Is.EqualTo(bounds.xMin).Within(0.0001f));
            Assert.That(target.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MoveTowards_AdvancesBySpeedAndDeltaTime()
        {
            Vector2 next = UnicornRoamingUtility.MoveTowards(Vector2.zero, new Vector2(10f, 0f), 4f, 0.5f);

            Assert.That(next.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(next.y, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}