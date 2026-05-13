using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public static class WeaponAutoFireTargetingUtility
    {
        public static int SelectNearestTargetIndex(
            Vector2 origin,
            Vector2 forward,
            IReadOnlyList<Vector2> targetPositions,
            float range,
            float coneDegrees)
        {
            if (targetPositions == null || targetPositions.Count == 0)
            {
                return -1;
            }

            int bestIndex = -1;
            float bestDistanceSquared = float.MaxValue;

            for (int index = 0; index < targetPositions.Count; index++)
            {
                Vector2 targetPosition = targetPositions[index];
                if (!IsWithinCone(origin, forward, targetPosition, range, coneDegrees))
                {
                    continue;
                }

                float distanceSquared = (targetPosition - origin).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestIndex = index;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return bestIndex;
        }

        public static bool IsWithinCone(
            Vector2 origin,
            Vector2 forward,
            Vector2 targetPosition,
            float range,
            float coneDegrees)
        {
            Vector2 toTarget = targetPosition - origin;
            float distanceSquared = toTarget.sqrMagnitude;
            if (range > 0f && distanceSquared > range * range)
            {
                return false;
            }

            if (distanceSquared <= 0.0001f)
            {
                return true;
            }

            Vector2 normalizedForward = forward.sqrMagnitude > 0f ? forward.normalized : Vector2.up;
            Vector2 normalizedToTarget = toTarget / Mathf.Sqrt(distanceSquared);
            float halfConeDegrees = Mathf.Max(0f, coneDegrees) * 0.5f;
            float minimumDot = Mathf.Cos(halfConeDegrees * Mathf.Deg2Rad);
            return Vector2.Dot(normalizedForward, normalizedToTarget) >= minimumDot;
        }
    }
}