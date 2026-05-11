using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public readonly struct TravellerTargetCandidate
    {
        public TravellerTargetCandidate(Vector2 position, bool isHarvesting)
        {
            Position = position;
            IsHarvesting = isHarvesting;
        }

        public Vector2 Position { get; }

        public bool IsHarvesting { get; }
    }

    public static class TravellerTargetSelectionUtility
    {
        public static int SelectBestCandidateIndex(
            Vector2 seekerPosition,
            IReadOnlyList<TravellerTargetCandidate> candidates,
            float maxRange)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return -1;
            }

            float maxDistanceSquared = maxRange > 0f ? maxRange * maxRange : float.PositiveInfinity;
            int bestIndex = -1;
            float bestDistanceSquared = float.MaxValue;
            bool foundHarvestingTarget = false;

            for (int index = 0; index < candidates.Count; index++)
            {
                TravellerTargetCandidate candidate = candidates[index];
                float distanceSquared = (candidate.Position - seekerPosition).sqrMagnitude;
                if (distanceSquared > maxDistanceSquared)
                {
                    continue;
                }

                if (candidate.IsHarvesting)
                {
                    if (!foundHarvestingTarget || distanceSquared < bestDistanceSquared)
                    {
                        foundHarvestingTarget = true;
                        bestIndex = index;
                        bestDistanceSquared = distanceSquared;
                    }

                    continue;
                }

                if (foundHarvestingTarget)
                {
                    continue;
                }

                if (bestIndex < 0 || distanceSquared < bestDistanceSquared)
                {
                    bestIndex = index;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return bestIndex;
        }
    }
}