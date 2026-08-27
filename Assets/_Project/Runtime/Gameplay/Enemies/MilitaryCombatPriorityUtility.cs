using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public static class MilitaryCombatPriorityUtility
    {
        public static bool ShouldPrioritizeCombat(float distanceToPlayer, float weaponRange, bool playerCanBeTargeted, bool hasLineOfSight)
        {
            return playerCanBeTargeted
                && hasLineOfSight
                && distanceToPlayer <= Mathf.Max(0f, weaponRange);
        }
    }
}