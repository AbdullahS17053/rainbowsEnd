using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public static class PoliceLoadoutSelectionUtility
    {
        public static bool ShouldUsePistol(
            float roll01,
            float pistolChance,
            bool hasPistol,
            bool hasShotgun)
        {
            if (!hasPistol)
            {
                return false;
            }

            if (!hasShotgun)
            {
                return true;
            }

            return Mathf.Clamp01(roll01) < Mathf.Clamp01(pistolChance);
        }
    }
}
