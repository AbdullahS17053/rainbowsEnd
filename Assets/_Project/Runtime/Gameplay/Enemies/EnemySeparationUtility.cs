using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public static class EnemySeparationUtility
    {
        private static readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        private static readonly ContactFilter2D _contactFilter = new ContactFilter2D { useTriggers = true, useLayerMask = false };

        public static Vector2 CalculateSeparation(
            Vector2 position,
            Collider2D selfCollider,
            float separationRadius,
            float separationStrength)
        {
            if (selfCollider == null || separationRadius <= 0f || separationStrength <= 0f)
            {
                return Vector2.zero;
            }

            Vector2 separation = Vector2.zero;
            int hitCount = Physics2D.OverlapCircle(position, separationRadius, _contactFilter, _overlapBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _overlapBuffer[i];
                if (hit == null || hit == selfCollider)
                {
                    continue;
                }

                if (hit.GetComponent<TravellerController>() == null &&
                    hit.GetComponent<PoliceController>() == null &&
                    hit.GetComponent<MilitaryController>() == null)
                {
                    continue;
                }

                Vector2 away = position - (Vector2)hit.transform.position;
                float distance = away.sqrMagnitude;

                if (distance < 0.0001f)
                {
                    away = new Vector2(Random.value - 0.5f, Random.value - 0.5f);
                    distance = 0.01f;
                }

                float distanceFactor = 1f - Mathf.Sqrt(distance) / separationRadius;
                if (distanceFactor <= 0f)
                {
                    continue;
                }

                separation += away.normalized * (separationStrength * distanceFactor);
            }

            return separation;
        }
    }
}
