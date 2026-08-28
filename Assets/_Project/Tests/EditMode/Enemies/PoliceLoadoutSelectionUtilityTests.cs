using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class PoliceLoadoutSelectionUtilityTests
    {
        [Test]
        public void SelectsPistolBelowConfiguredHalfBoundary()
        {
            Assert.That(
                PoliceLoadoutSelectionUtility.ShouldUsePistol(0.49f, 0.5f, true, true),
                Is.True);
        }

        [Test]
        public void SelectsShotgunAtOrAboveConfiguredHalfBoundary()
        {
            Assert.That(
                PoliceLoadoutSelectionUtility.ShouldUsePistol(0.5f, 0.5f, true, true),
                Is.False);
        }

        [Test]
        public void UsesAvailableLoadoutWhenOtherWeaponIsMissing()
        {
            Assert.That(
                PoliceLoadoutSelectionUtility.ShouldUsePistol(0.99f, 0.5f, true, false),
                Is.True);
            Assert.That(
                PoliceLoadoutSelectionUtility.ShouldUsePistol(0.01f, 0.5f, false, true),
                Is.False);
        }
    }
}
