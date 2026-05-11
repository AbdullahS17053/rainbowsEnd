using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public static class WeaponVolleyUtility
    {
        public static float CalculateProjectileOffsetDegrees(int projectileIndex, int projectileCount, float volleySpreadDegrees)
        {
            int clampedProjectileCount = Mathf.Max(1, projectileCount);
            if (clampedProjectileCount == 1 || volleySpreadDegrees <= 0f)
            {
                return 0f;
            }

            float t = clampedProjectileCount > 1
                ? Mathf.Clamp01((float)projectileIndex / (clampedProjectileCount - 1))
                : 0.5f;

            float halfSpread = volleySpreadDegrees * 0.5f;
            return Mathf.Lerp(-halfSpread, halfSpread, t);
        }

        public static Vector2 CalculateProjectileDirection(Vector2 baseDirection, int projectileIndex, int projectileCount, float volleySpreadDegrees)
        {
            Vector2 normalizedBaseDirection = baseDirection.sqrMagnitude > 0f ? baseDirection.normalized : Vector2.up;
            float offsetDegrees = CalculateProjectileOffsetDegrees(projectileIndex, projectileCount, volleySpreadDegrees);
            return WeaponSpreadUtility.ApplySpread(normalizedBaseDirection, offsetDegrees);
        }
    }
}