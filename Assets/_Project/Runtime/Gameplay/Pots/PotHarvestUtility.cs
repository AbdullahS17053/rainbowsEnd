using UnityEngine;

namespace RainbowsEnd.Gameplay.Pots
{
    public static class PotHarvestUtility
    {
        public static float CalculateBaseHarvestGoldPerSecond(float capturedTrailAgeSeconds, float baseHarvestGoldPerSecond, float trailAgeGoldMultiplier)
        {
            float clampedAgeSeconds = Mathf.Max(0f, capturedTrailAgeSeconds);
            float clampedBaseHarvestRate = Mathf.Max(0f, baseHarvestGoldPerSecond);
            float clampedAgeMultiplier = Mathf.Max(0f, trailAgeGoldMultiplier);
            return clampedBaseHarvestRate * (1f + clampedAgeSeconds * clampedAgeMultiplier);
        }
    }
}