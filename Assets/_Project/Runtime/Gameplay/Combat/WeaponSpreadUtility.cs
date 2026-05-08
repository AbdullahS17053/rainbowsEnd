using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public static class WeaponSpreadUtility
    {
        public static float CalculateSpreadOffsetDegrees(float movementNormalized, float minSpreadDegrees, float maxSpreadDegrees, float randomValue)
        {
            float clampedMovement = Mathf.Clamp01(movementNormalized);
            float maxSpreadOffset = Mathf.Lerp(minSpreadDegrees, maxSpreadDegrees, clampedMovement);
            float centeredRandom = Mathf.Clamp01(randomValue) * 2f - 1f;
            return centeredRandom * maxSpreadOffset;
        }

        public static Vector2 ApplySpread(Vector2 baseDirection, float spreadOffsetDegrees)
        {
            Vector2 normalizedDirection = baseDirection.sqrMagnitude > 0f ? baseDirection.normalized : Vector2.up;
            return Quaternion.Euler(0f, 0f, spreadOffsetDegrees) * normalizedDirection;
        }
    }
}