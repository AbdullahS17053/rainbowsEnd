using System.Collections.Generic;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public sealed class RainbowTrailSegment : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Transform floatingVisual;
        [SerializeField] private ParticleSystem trailParticleSystem;
        [SerializeField, Min(0f)] private float floatAmplitude = 0.12f;
        [SerializeField, Min(0f)] private float floatFrequency = 3.2f;

        public ParticleSystem TrailParticleSystem => trailParticleSystem;
        private static readonly List<RainbowTrailSegment> ActiveSegments = new List<RainbowTrailSegment>();
        private static readonly Dictionary<Vector2Int, RainbowTrailSegment> ActiveSegmentsByCell = new Dictionary<Vector2Int, RainbowTrailSegment>();


        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private Vector3 _floatingVisualBaseLocalPosition;
        private bool _hasFloatingVisualBasePosition;
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
            if (trailParticleSystem != null)
            {
                trailParticleSystem.Clear();
                trailParticleSystem.Play();
            }
        }

        public void OnDespawned()
        {
            CacheReferences();
            RemoveCellRegistration();
            ResetState();
            UpdateVisualAlpha(1f);
            if (trailParticleSystem != null)
            {
                trailParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            ActiveSegments.Remove(this);
        }

        private void Update()
        {
            if (floatingVisual != null && _hasFloatingVisualBasePosition)
            {
                floatingVisual.localPosition = _floatingVisualBaseLocalPosition;
            }

            if (trailParticleSystem != null)
            {
                trailParticleSystem.transform.localPosition = Vector3.zero;
            }
        }

        private void Reset()
        {
            CacheReferences(refreshBaseColor: true, refreshFloatBasePosition: true);
        }

        private void OnValidate()
        {
            CacheReferences(refreshBaseColor: true, refreshFloatBasePosition: true);
        }

        private void Awake()
        {
            CacheReferences(refreshBaseColor: true, refreshFloatBasePosition: true);
        }

        public void ConfigureParticles(ParticleSystem ps)
        {
            trailParticleSystem = ps;
        }

        private void EnsureTrailParticleSystem()
        {
            if (trailParticleSystem != null)
            {
                return;
            }

            trailParticleSystem = GetComponentInChildren<ParticleSystem>();
            if (trailParticleSystem == null)
            {
                GameObject psObj = new GameObject("RainbowTrailParticles");
                psObj.transform.SetParent(transform, false);
                trailParticleSystem = psObj.AddComponent<ParticleSystem>();
                ConfigureDefaultTrailParticles(trailParticleSystem);
            }
        }

        private void ConfigureDefaultTrailParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.52f);
            main.maxParticles = 500;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.35f, 0.45f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.25f),
                    new GradientColorKey(new Color(0.2f, 1f, 0.4f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.85f, 1f), 0.75f),
                    new GradientColorKey(new Color(0.85f, 0.45f, 1f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.85f, 0.15f),
                    new GradientAlphaKey(0.85f, 0.75f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.6f);
            curve.AddKey(0.3f, 1.1f);
            curve.AddKey(1f, 0.35f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);

            if (ps.TryGetComponent(out ParticleSystemRenderer renderer))
            {
                renderer.sortingOrder = 15;
                renderer.sortingLayerName = "Default";
#if UNITY_EDITOR
                Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_UnicornTailTrail.mat");
                if (mat != null)
                {
                    renderer.material = mat;
                    renderer.trailMaterial = mat;
                }
#endif
            }
        }

        private void CacheReferences(bool refreshBaseColor = false, bool refreshFloatBasePosition = false)
        {
            visualRenderer ??= GetComponent<SpriteRenderer>();
            floatingVisual ??= transform.Find("FX_RainbowGlow_02");

            // Deactivate FX_RainbowGlow_02 (the glowing glob) in favor of the rainbow particle trail
            if (floatingVisual != null && floatingVisual.gameObject.activeSelf)
            {
                floatingVisual.gameObject.SetActive(false);
            }

            EnsureTrailParticleSystem();

            if (visualRenderer != null && (refreshBaseColor || !_hasBaseColor))
            {
                _baseColor = visualRenderer.color;
                _hasBaseColor = true;
            }

            if (floatingVisual != null && (refreshFloatBasePosition || !_hasFloatingVisualBasePosition))
            {
                _floatingVisualBaseLocalPosition = floatingVisual.localPosition;
                _hasFloatingVisualBasePosition = true;
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
            if (floatingVisual != null && _hasFloatingVisualBasePosition)
            {
                floatingVisual.localPosition = _floatingVisualBaseLocalPosition;
            }

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
