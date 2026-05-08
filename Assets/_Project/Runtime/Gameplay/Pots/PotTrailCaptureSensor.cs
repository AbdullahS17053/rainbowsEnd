using System.Collections.Generic;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Pots
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PotTrailCaptureSensor : MonoBehaviour
    {
        [SerializeField] private PotController potController;
        [SerializeField] private CircleCollider2D triggerCollider;

        private readonly List<RainbowTrailSegment> _overlappingSegments = new List<RainbowTrailSegment>();

        public void Configure(PotController controller, float radius)
        {
            potController = controller;
            EnsureCollider();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = Mathf.Max(0.01f, radius);
        }

        public bool TryGetOldestOverlappingTrailTimestamp(out float trailTimestampSeconds)
        {
            trailTimestampSeconds = 0f;

            RefreshOverlappingSegments();

            RainbowTrailSegment oldestSegment = null;
            float oldestTimestampSeconds = float.MaxValue;
            for (int index = _overlappingSegments.Count - 1; index >= 0; index--)
            {
                RainbowTrailSegment segment = _overlappingSegments[index];
                if (segment == null || !segment.isActiveAndEnabled)
                {
                    _overlappingSegments.RemoveAt(index);
                    continue;
                }

                if (segment.SpawnTimestampSeconds <= 0f || segment.SpawnTimestampSeconds >= oldestTimestampSeconds)
                {
                    continue;
                }

                oldestTimestampSeconds = segment.SpawnTimestampSeconds;
                oldestSegment = segment;
            }

            if (oldestSegment == null)
            {
                return false;
            }

            trailTimestampSeconds = oldestTimestampSeconds;
            return true;
        }

        private void OnEnable()
        {
            _overlappingSegments.Clear();
        }

        private void OnDisable()
        {
            _overlappingSegments.Clear();
        }

        private void Reset()
        {
            potController ??= GetComponentInParent<PotController>();
            EnsureCollider();
        }

        private void OnValidate()
        {
            potController ??= GetComponentInParent<PotController>();
            EnsureCollider();
        }

        private void OnDrawGizmosSelected()
        {
            EnsureCollider();
            if (triggerCollider == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.84f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(triggerCollider.bounds.center, ResolveWorldRadius());
        }

        private void EnsureCollider()
        {
            triggerCollider ??= GetComponent<CircleCollider2D>();
        }

        private void RefreshOverlappingSegments()
        {
            _overlappingSegments.Clear();
            EnsureCollider();
            if (triggerCollider == null || !triggerCollider.enabled)
            {
                return;
            }

            Collider2D[] overlapResults = Physics2D.OverlapCircleAll(triggerCollider.bounds.center, ResolveWorldRadius());
            for (int index = 0; index < overlapResults.Length; index++)
            {
                Collider2D overlap = overlapResults[index];
                if (overlap == null || overlap == triggerCollider || !overlap.TryGetComponent(out RainbowTrailSegment segment))
                {
                    continue;
                }

                if (!segment.isActiveAndEnabled || _overlappingSegments.Contains(segment))
                {
                    continue;
                }

                _overlappingSegments.Add(segment);
            }
        }

        private float ResolveWorldRadius()
        {
            return Mathf.Max(triggerCollider.bounds.extents.x, triggerCollider.bounds.extents.y, 0.01f);
        }
    }
}