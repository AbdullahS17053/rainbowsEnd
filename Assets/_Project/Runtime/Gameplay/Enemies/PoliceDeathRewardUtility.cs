namespace RainbowsEnd.Gameplay.Enemies
{
    public enum PoliceDeathRewardKind
    {
        None,
        Weapon,
        Gold
    }

    public static class PoliceDeathRewardUtility
    {
        public static PoliceDeathRewardKind ResolveRewardKind(
            float deathRoll01,
            float branchRoll01,
            float deathDropChance,
            float weaponRewardChance,
            bool weaponAvailable,
            bool goldAvailable)
        {
            if ((!weaponAvailable && !goldAvailable)
                || deathRoll01 >= Clamp01(deathDropChance))
            {
                return PoliceDeathRewardKind.None;
            }

            if (weaponAvailable
                && (!goldAvailable || branchRoll01 < Clamp01(weaponRewardChance)))
            {
                return PoliceDeathRewardKind.Weapon;
            }

            return goldAvailable
                ? PoliceDeathRewardKind.Gold
                : PoliceDeathRewardKind.None;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}
