namespace RainbowsEnd.Gameplay.Enemies
{
    public enum PoliceCombatPhase
    {
        Approach,
        AimHold,
        Reposition
    }

    public static class PoliceCombatCycleUtility
    {
        public static bool ShouldBeginAimHold(float distanceToTarget, float preferredRange, bool canShoot)
        {
            return canShoot && distanceToTarget <= preferredRange;
        }

        public static PoliceCombatPhase ResolveAimHold(
            float remainingSeconds,
            bool targetValid,
            bool fireSucceeded)
        {
            if (!targetValid)
            {
                return PoliceCombatPhase.Approach;
            }

            if (remainingSeconds > 0f)
            {
                return PoliceCombatPhase.AimHold;
            }

            return fireSucceeded ? PoliceCombatPhase.Reposition : PoliceCombatPhase.AimHold;
        }

        public static PoliceCombatPhase ResolveReposition(
            float remainingSeconds,
            bool targetValid)
        {
            if (!targetValid)
            {
                return PoliceCombatPhase.Approach;
            }

            return remainingSeconds > 0f
                ? PoliceCombatPhase.Reposition
                : PoliceCombatPhase.Approach;
        }

        public static int ResolveRepositionSign(float roll01)
        {
            return roll01 < 0.5f ? -1 : 1;
        }
    }
}
