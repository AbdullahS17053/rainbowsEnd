using System.Collections.Generic;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public sealed class RainbowTrailSegment : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer visualRenderer;

        private static readonly List<RainbowTrailSegment> ActiveSegments = new List<RainbowTrailSegment>();

        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private PrefabPool _owningPool;

        public float SpawnTimestampSeconds { get; private set; }

        public float AgeSeconds => SpawnTimestampSeconds <= 0f ? 0f : Mathf.Max(0f, Time.time - SpawnTimestampSeconds);

        public static void ReturnAllToPool()
        {
            for (int index = ActiveSegments.Count - 1; index >= 0; index--)
            {
                RainbowTrailSegment segment = ActiveSegments[index];
                if (segment != null && segment._owningPool != null)
                {
                    segment._owningPool.Return(segment.gameObject);
                }
            }

            ActiveSegments.Clear();
        }

        public void Activate(PrefabPool owningPool, float spawnTimestampSeconds)
        {
            CacheReferences();

            _owningPool = owningPool;
            SpawnTimestampSeconds = spawnTimestampSeconds;
            ActiveSegments.Add(this);
            UpdateVisualAlpha(1f);
        }

        public void OnSpawned()
        {
            CacheReferences();
            ResetState();
            UpdateVisualAlpha(1f);
        }

        public void OnDespawned()
        {
            CacheReferences();
            ResetState();
            UpdateVisualAlpha(1f);
            ActiveSegments.Remove(this);
        }

        private void Reset()
        {
            CacheReferences(refreshBaseColor: true);
        }

        private void OnValidate()
        {
            CacheReferences(refreshBaseColor: true);
        }

        private void Awake()
        {
            CacheReferences(refreshBaseColor: true);
        }

        private void CacheReferences(bool refreshBaseColor = false)
        {
            visualRenderer ??= GetComponent<SpriteRenderer>();
            if (visualRenderer != null && (refreshBaseColor || !_hasBaseColor))
            {
                _baseColor = visualRenderer.color;
                _hasBaseColor = true;
            }
        }

        private void UpdateVisualAlpha(float normalizedAlpha)
        {
            if (visualRenderer == null)
            {
                return;
            }

            Color color = _baseColor;
            color.a = _baseColor.a * Mathf.Clamp01(normalizedAlpha);
            visualRenderer.color = color;
        }

        private void ResetState()
        {
            _owningPool = null;
            SpawnTimestampSeconds = 0f;
        }
    }
}