using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class ArrowProjectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private LayerMask hitMask = Physics2D.DefaultRaycastLayers;
        [SerializeField] [Min(0.01f)] private float fallbackSpeed = 18f;
        [SerializeField] [Min(0f)] private float castRadius = 0.05f;

        private TrailRenderer _firearmTracer;
        private GameObject _magicVisual;
        private ParticleSystem[] _magicParticles;


        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];

        private PrefabPool _owningPool;
        private WeaponConfig _weaponConfig;
        private Collider2D _ignoredCollider;
        private WeaponImpactFeedbackSpawner _impactFeedbackSpawner;
        private Vector2 _direction = Vector2.up;
        private FactionId _sourceFaction;
        private Object _instigator;
        private float _damage;
        private float _range;
        private float _distanceTravelled;
        private bool _isLaunched;

        public void Launch(
            PrefabPool owningPool,
            Vector2 direction,
            WeaponConfig weaponConfig,
            FactionId sourceFaction,
            Collider2D ignoredCollider,
            Object instigator,
            WeaponImpactFeedbackSpawner impactFeedbackSpawner,
            float damageOverride = -1f,
            float rangeOverride = -1f,
            bool renderTracer = true)
        {
            _owningPool = owningPool;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
            _weaponConfig = weaponConfig;
            _sourceFaction = sourceFaction;
            _ignoredCollider = ignoredCollider;
            _instigator = instigator;
            _impactFeedbackSpawner = impactFeedbackSpawner;
            _damage = damageOverride >= 0f ? damageOverride : (weaponConfig != null ? weaponConfig.Damage : 0f);
            _range = rangeOverride >= 0f
                ? rangeOverride
                : (weaponConfig != null ? Mathf.Max(0f, weaponConfig.Range) : 0f);
            _distanceTravelled = 0f;
            _isLaunched = true;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            ApplyPresentation(renderTracer);
        }

        public void OnSpawned()
        {
            ResetState();
        }

        public void OnDespawned()
        {
            ResetState();
        }

        private void Update()
        {
            if (!_isLaunched || _owningPool == null || _weaponConfig == null)
            {
                return;
            }

            float remainingDistance = Mathf.Max(0f, _range - _distanceTravelled);
            if (remainingDistance <= 0f)
            {
                ReturnToPool();
                return;
            }

            float stepDistance = Mathf.Min(ResolveSpeed() * Time.deltaTime, remainingDistance);
            if (stepDistance <= 0f)
            {
                return;
            }

            Vector2 currentPosition = transform.position;
            ContactFilter2D contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = hitMask,
                useTriggers = true
            };

            int hitCount = Physics2D.CircleCast(currentPosition, castRadius, _direction, contactFilter, _hits, stepDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (!WeaponHitUtility.TryResolveHit(hit.collider, _ignoredCollider, _sourceFaction, _damage, hit.point, hit.normal, _instigator, out WeaponHitResult hitResult))
                {
                    continue;
                }

                transform.position = hit.point;
                _impactFeedbackSpawner?.Spawn(hitResult, _weaponConfig);
                ReturnToPool();
                return;
            }

            transform.position = currentPosition + _direction * stepDistance;
            _distanceTravelled += stepDistance;

            if (_distanceTravelled >= _range)
            {
                ReturnToPool();
            }
        }

        private float ResolveSpeed()
        {
            if (_weaponConfig != null && _weaponConfig.ProjectileSpeed > 0f)
            {
                return _weaponConfig.ProjectileSpeed;
            }

            return fallbackSpeed;
        }

        private void ReturnToPool()
        {
            if (_owningPool == null)
            {
                ResetState();
                return;
            }

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            _isLaunched = false;
            owningPool.Return(gameObject);
        }

        private void ResetState()
        {
            ResetPresentation();
            _owningPool = null;
            _weaponConfig = null;
            _ignoredCollider = null;
            _impactFeedbackSpawner = null;
            _direction = Vector2.up;
            _sourceFaction = FactionId.Neutral;
            _instigator = null;
            _damage = 0f;
            _range = 0f;
            _distanceTravelled = 0f;
            _isLaunched = false;
        }

        private void ApplyPresentation(bool renderTracer)
        {
            CachePresentation();

            bool isFirearm = _weaponConfig != null && _weaponConfig.IsFirearm;
            if (_firearmTracer != null)
            {
                _firearmTracer.Clear();
                _firearmTracer.enabled = isFirearm && renderTracer;
                if (_firearmTracer.enabled)
                {
                    bool isPlayerTracer = _sourceFaction == FactionId.Player;
                    _firearmTracer.startColor = isPlayerTracer
                        ? new Color(1f, 0.72f, 0.08f, 1f)
                        : new Color(1f, 0.12f, 0.02f, 1f);
                    _firearmTracer.endColor = new Color(
                        _firearmTracer.startColor.r,
                        _firearmTracer.startColor.g,
                        _firearmTracer.startColor.b,
                        0f);
                }
            }

            if (_magicVisual != null)
            {
                _magicVisual.SetActive(!isFirearm);
            }

            for (int index = 0; index < (_magicParticles != null ? _magicParticles.Length : 0); index++)
            {
                ParticleSystem particleSystem = _magicParticles[index];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (!isFirearm)
                {
                    ParticleSystem.MainModule main = particleSystem.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(0.08f, 1f, 0.38f, 1f),
                        new Color(1f, 0.82f, 0.12f, 1f));
                    particleSystem.Play(true);
                }
            }
        }

        private void CachePresentation()
        {
            _firearmTracer ??= GetComponent<TrailRenderer>();
            _magicVisual ??= transform.Find("SpikyFireTrail")?.gameObject;
            _magicParticles ??= GetComponentsInChildren<ParticleSystem>(true);
        }

        private void ResetPresentation()
        {
            CachePresentation();

            if (_firearmTracer != null)
            {
                _firearmTracer.Clear();
                _firearmTracer.enabled = false;
            }

            if (_magicVisual != null)
            {
                _magicVisual.SetActive(false);
            }

            for (int index = 0; index < (_magicParticles != null ? _magicParticles.Length : 0); index++)
            {
                _magicParticles[index]?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}