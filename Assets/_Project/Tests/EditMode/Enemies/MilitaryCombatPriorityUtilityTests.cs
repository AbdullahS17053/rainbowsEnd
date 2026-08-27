using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class MilitaryCombatPriorityUtilityTests
    {
        [Test]
        public void ShouldPrioritizeCombat_OnlyForClearTargetWithinWeaponRange()
        {
            Assert.That(MilitaryCombatPriorityUtility.ShouldPrioritizeCombat(8f, 8f, true, true), Is.True);
            Assert.That(MilitaryCombatPriorityUtility.ShouldPrioritizeCombat(8.01f, 8f, true, true), Is.False);
            Assert.That(MilitaryCombatPriorityUtility.ShouldPrioritizeCombat(4f, 8f, true, false), Is.False);
        }
    }
}