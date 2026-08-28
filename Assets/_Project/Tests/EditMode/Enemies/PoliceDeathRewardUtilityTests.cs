using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class PoliceDeathRewardUtilityTests
    {
        [Test]
        public void SuccessfulRollUsesWeaponBelowBranchBoundary()
        {
            Assert.That(
                PoliceDeathRewardUtility.ResolveRewardKind(
                    0.329f,
                    0.49f,
                    0.33f,
                    0.5f,
                    true,
                    true),
                Is.EqualTo(PoliceDeathRewardKind.Weapon));
        }

        [Test]
        public void DropBoundaryReturnsNoReward()
        {
            Assert.That(
                PoliceDeathRewardUtility.ResolveRewardKind(
                    0.33f,
                    0.49f,
                    0.33f,
                    0.5f,
                    true,
                    true),
                Is.EqualTo(PoliceDeathRewardKind.None));
        }

        [Test]
        public void SuccessfulRollUsesGoldAtOrAboveWeaponBoundary()
        {
            Assert.That(
                PoliceDeathRewardUtility.ResolveRewardKind(
                    0.1f,
                    0.5f,
                    0.33f,
                    0.5f,
                    true,
                    true),
                Is.EqualTo(PoliceDeathRewardKind.Gold));
        }

        [Test]
        public void WeaponIsFallbackWhenGoldIsUnavailable()
        {
            Assert.That(
                PoliceDeathRewardUtility.ResolveRewardKind(
                    0.1f,
                    0.1f,
                    0.33f,
                    0.5f,
                    true,
                    false),
                Is.EqualTo(PoliceDeathRewardKind.Weapon));
        }

        [Test]
        public void GoldIsFallbackWhenWeaponIsUnavailable()
        {
            Assert.That(
                PoliceDeathRewardUtility.ResolveRewardKind(
                    0.1f,
                    0.1f,
                    0.33f,
                    0.5f,
                    false,
                    true),
                Is.EqualTo(PoliceDeathRewardKind.Gold));
        }

        [Test]
        public void MissingPoolsReturnNoReward()
        {
            Assert.That(
                PoliceDeathRewardUtility.ResolveRewardKind(
                    0.1f,
                    0.1f,
                    0.33f,
                    0.5f,
                    false,
                    false),
                Is.EqualTo(PoliceDeathRewardKind.None));
        }
    }
}
