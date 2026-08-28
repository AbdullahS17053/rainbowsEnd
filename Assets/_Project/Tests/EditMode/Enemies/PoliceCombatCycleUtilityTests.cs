using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class PoliceCombatCycleUtilityTests
    {
        [Test]
        public void ApproachOnlyBeginsAimHoldAtPreferredRangeWithLineOfSight()
        {
            Assert.That(
                PoliceCombatCycleUtility.ShouldBeginAimHold(3f, 3f, true),
                Is.True);
            Assert.That(
                PoliceCombatCycleUtility.ShouldBeginAimHold(3.01f, 3f, true),
                Is.False);
            Assert.That(
                PoliceCombatCycleUtility.ShouldBeginAimHold(2f, 3f, false),
                Is.False);
        }

        [Test]
        public void AimHoldWaitsForTimerAndRetriesFailedFire()
        {
            Assert.That(
                PoliceCombatCycleUtility.ResolveAimHold(0.01f, true, false),
                Is.EqualTo(PoliceCombatPhase.AimHold));
            Assert.That(
                PoliceCombatCycleUtility.ResolveAimHold(0f, true, false),
                Is.EqualTo(PoliceCombatPhase.AimHold));
            Assert.That(
                PoliceCombatCycleUtility.ResolveAimHold(0f, true, true),
                Is.EqualTo(PoliceCombatPhase.Reposition));
        }

        [Test]
        public void RepositionEndsOnlyAfterItsTimerAndResetsOnInterruption()
        {
            Assert.That(
                PoliceCombatCycleUtility.ResolveReposition(0.01f, true),
                Is.EqualTo(PoliceCombatPhase.Reposition));
            Assert.That(
                PoliceCombatCycleUtility.ResolveReposition(0f, true),
                Is.EqualTo(PoliceCombatPhase.Approach));
            Assert.That(
                PoliceCombatCycleUtility.ResolveReposition(0.5f, false),
                Is.EqualTo(PoliceCombatPhase.Approach));
        }

        [Test]
        public void RepositionDirectionUsesOneBoundarySafeRoll()
        {
            Assert.That(PoliceCombatCycleUtility.ResolveRepositionSign(0.49f), Is.EqualTo(-1));
            Assert.That(PoliceCombatCycleUtility.ResolveRepositionSign(0.5f), Is.EqualTo(1));
        }
    }
}
