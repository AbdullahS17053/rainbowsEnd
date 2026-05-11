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

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];

        private PrefabPool _owningPool;
        private WeaponConfig _weaponConfig;
        private Collider2D _ignoredCollider;
        private WeaponImpactFeedbackSpawner _impactFeedbackSpawner;
        private Vector2 _direction = Vector2.up;
        private FactionId _sourceFaction;
        private Object _instigator;
        private float _distanceTravelled;
        private bool _isLaunched;

        public void Launch(
            PrefabPool owningPool,
            Vector2 direction,
            WeaponConfig weaponConfig,
            FactionId sourceFaction,
            Collider2D ignoredCollider,
            Object instigator,
            WeaponImpactFeedbackSpawner impactFeedbackSpawner)
        {
            _owningPool = owningPool;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
            _weaponConfig = weaponConfig;
            _sourceFaction = sourceFaction;
            _ignoredCollider = ignoredCollider;
            _instigator = instigator;
            _impactFeedbackSpawner = impactFeedbackSpawner;
            _distanceTravelled = 0f;
            _isLaunched = true;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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

            float remainingDistance = Mathf.Max(0f, _weaponConfig.Range - _distanceTravelled);
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
                if (!WeaponHitUtility.TryResolveHit(hit.collider, _ignoredCollider, _sourceFaction, _weaponConfig.Damage, hit.point, hit.normal, _instigator, out WeaponHitResult hitResult))
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

            if (_distanceTravelled >= _weaponConfig.Range)
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
            _owningPool = null;
            _weaponConfig = null;
            _ignoredCollider = null;
            _impactFeedbackSpawner = null;
            _direction = Vector2.up;
            _sourceFaction = FactionId.Neutral;
            _instigator = null;
            _distanceTravelled = 0f;
            _isLaunched = false;
        }
    }
}