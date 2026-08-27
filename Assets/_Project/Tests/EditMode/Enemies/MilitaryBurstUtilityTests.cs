using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class MilitaryBurstUtilityTests
    {
        [Test]
        public void ShouldStartTelegraph_OnlyOnceBeforeEachBurst()
        {
            Assert.That(MilitaryBurstUtility.ShouldStartTelegraph(0, false, 0.35f), Is.True);
            Assert.That(MilitaryBurstUtility.ShouldStartTelegraph(0, true, 0.35f), Is.False);
            Assert.That(MilitaryBurstUtility.ShouldStartTelegraph(1, false, 0.35f), Is.False);
            Assert.That(MilitaryBurstUtility.ShouldStartTelegraph(0, false, 0f), Is.False);
        }
    }
}