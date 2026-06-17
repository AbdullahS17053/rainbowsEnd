using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public readonly struct TravellerTargetCandidate
    {
        public TravellerTargetCandidate(Vector2 position, bool isHarvesting, bool hasGold)
        {
            Position = position;
            IsHarvesting = isHarvesting;
            HasGold = hasGold;
        }

        public Vector2 Position { get; }

        public bool IsHarvesting { get; }

        public bool HasGold { get; }
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
            int bestPriority = -1;

            for (int index = 0; index < candidates.Count; index++)
            {
                TravellerTargetCandidate candidate = candidates[index];
                float distanceSquared = (candidate.Position - seekerPosition).sqrMagnitude;
                if (distanceSquared > maxDistanceSquared)
                {
                    continue;
                }

                int priority = candidate switch
                {
                    { IsHarvesting: true, HasGold: true } => 3,
                    { HasGold: true } => 2,
                    { IsHarvesting: true } => 1,
                    _ => 0
                };

                if (priority == 0)
                {
                    continue;
                }

                if (priority > bestPriority || (priority == bestPriority && distanceSquared < bestDistanceSquared))
                {
                    bestPriority = priority;
                    bestIndex = index;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return bestIndex;
        }
    }
}
