using System.Collections.Generic;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public sealed class RainbowTrailSegment : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer visualRenderer;

        private static readonly List<RainbowTrailSegment> ActiveSegments = new List<RainbowTrailSegment>();
        private static readonly Dictionary<Vector2Int, RainbowTrailSegment> ActiveSegmentsByCell = new Dictionary<Vector2Int, RainbowTrailSegment>();


        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private PrefabPool _owningPool;
        private Vector2Int _cell;
        private bool _hasCell;


        public float SpawnTimestampSeconds { get; private set; }

        public float AgeSeconds => SpawnTimestampSeconds <= 0f ? 0f : Mathf.Max(0f, Time.time - SpawnTimestampSeconds);

        public static bool IsCellOccupied(Vector2Int cell)
        {
            if (!ActiveSegmentsByCell.TryGetValue(cell, out RainbowTrailSegment segment))
            {
                return false;
            }

            if (segment == null || !segment.isActiveAndEnabled || !segment._hasCell)
            {
                ActiveSegmentsByCell.Remove(cell);
                return false;
            }

            return true;
        }


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
            ActiveSegmentsByCell.Clear();
        }

        public void Activate(PrefabPool owningPool, float spawnTimestampSeconds)
        {
            Activate(owningPool, spawnTimestampSeconds, null);
        }

        public void Activate(PrefabPool owningPool, float spawnTimestampSeconds, Vector2Int? cell)
        {
            CacheReferences();

            _owningPool = owningPool;
            SpawnTimestampSeconds = spawnTimestampSeconds;
            if (cell.HasValue)
            {
                _cell = cell.Value;
                _hasCell = true;
                ActiveSegmentsByCell[_cell] = this;
            }

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
            RemoveCellRegistration();
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
            _cell = default;
            _hasCell = false;
        }


        private void RemoveCellRegistration()
        {
            if (!_hasCell
                || !ActiveSegmentsByCell.TryGetValue(_cell, out RainbowTrailSegment current)
                || !ReferenceEquals(current, this))
            {
                return;
            }

            ActiveSegmentsByCell.Remove(_cell);
        }
    }
}
