using RainbowsEnd.Core.Utilities;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public sealed class RainbowTrailSegment : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] [Min(0.01f)] private float defaultLifetimeSeconds = 5f;

        private readonly CountdownTimer _lifetimeTimer = new CountdownTimer();
        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private PrefabPool _owningPool;

        public float SpawnTimestampSeconds { get; private set; }

        public float LifetimeSeconds { get; private set; }

        public float AgeSeconds => SpawnTimestampSeconds <= 0f ? 0f : Mathf.Max(0f, Time.time - SpawnTimestampSeconds);

        public void Activate(PrefabPool owningPool, float spawnTimestampSeconds, float lifetimeSeconds)
        {
            CacheReferences();

            _owningPool = owningPool;
            SpawnTimestampSeconds = spawnTimestampSeconds;
            LifetimeSeconds = lifetimeSeconds > 0f ? lifetimeSeconds : defaultLifetimeSeconds;
            _lifetimeTimer.Start(LifetimeSeconds);
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

        private void Update()
        {
            if (_owningPool == null || !_lifetimeTimer.IsRunning)
            {
                return;
            }

            _lifetimeTimer.Tick(Time.deltaTime);
            UpdateVisualAlpha(1f - _lifetimeTimer.ElapsedNormalized);

            if (!_lifetimeTimer.IsComplete)
            {
                return;
            }

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            owningPool.Return(gameObject);
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
            LifetimeSeconds = 0f;
            _lifetimeTimer.Reset();
        }
    }
}