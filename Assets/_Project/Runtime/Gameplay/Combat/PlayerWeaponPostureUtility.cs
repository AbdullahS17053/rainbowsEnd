using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public static class PlayerWeaponPostureUtility
    {
        public static bool IsHipFiring(float movementNormalized, float movementThreshold)
        {
            return movementNormalized > Mathf.Max(0f, movementThreshold);
        }

        public static float ResolveRange(
            float normalRange,
            float movementNormalized,
            float movementThreshold,
            float hipFireRangeMultiplier)
        {
            float resolvedRange = Mathf.Max(0f, normalRange);
            return IsHipFiring(movementNormalized, movementThreshold)
                ? resolvedRange * Mathf.Max(0f, hipFireRangeMultiplier)
                : resolvedRange;
        }

        public static float ResolveConeDegrees(
            float normalConeDegrees,
            float movementNormalized,
            float movementThreshold,
            float hipFireConeMultiplier)
        {
            float resolvedConeDegrees = Mathf.Max(0f, normalConeDegrees);
            return IsHipFiring(movementNormalized, movementThreshold)
                ? resolvedConeDegrees * Mathf.Max(0f, hipFireConeMultiplier)
                : resolvedConeDegrees;
        }
    }
}
