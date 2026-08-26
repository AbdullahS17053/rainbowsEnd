using RainbowsEnd.Core.Animation;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Orchestration;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(ProjectileWeaponLauncher))]
    [RequireComponent(typeof(WeaponImpactFeedbackSpawner))]
    [RequireComponent(typeof(RangedAttackController))]
    public sealed class PoliceController : MonoBehaviour, ITeamAffiliation, IPoolable
    {
        [SerializeField] private EnemyConfig config;
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Health health;
        [SerializeField] private ProjectileWeaponLauncher projectileLauncher;
        [SerializeField] private WeaponImpactFeedbackSpawner impactFeedbackSpawner;
        [SerializeField] private RangedAttackController rangedAttackController;
        [SerializeField] private CharacterAnimationBridge animationBridge;

        private PrefabPool _owningPool;
        private PrefabPool _weaponPickupPool;
        private PoliceSpawner _owningSpawner;
        private WeaponConfig _dropWeaponConfig;
        private int _dropAmmoCount;
        private PlayerRoot _targetPlayer;
        private bool _deathHandled;
        private Vector2[] _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;

        public FactionId FactionId => FactionId.Police;

        public SpriteRenderer VisualRenderer => visualRenderer;

        public Health Health => health;

        public void Configure(
            EnemyConfig enemyConfig,
            WeaponConfig policeWeaponConfig,
            Transform actorVisualRoot,
            SpriteRenderer actorVisualRenderer,
            Rigidbody2D actorBody,
            Collider2D actorCollider,
            Health actorHealth,
            ProjectileWeaponLauncher weaponLauncher,
            WeaponImpactFeedbackSpawner feedbackSpawner,
            RangedAttackController attackController)
        {
            config = enemyConfig;
            weaponConfig = policeWeaponConfig;
            visualRoot = actorVisualRoot;
            visualRenderer = actorVisualRenderer;
            body = actorBody;
            bodyCollider = actorCollider;
            health = actorHealth;
            projectileLauncher = weaponLauncher;
            impactFeedbackSpawner = feedbackSpawner;
            rangedAttackController = attackController;
        }

        public void Activate(
            PoliceSpawner owningSpawner,
            PrefabPool owningPool,
            PrefabPool weaponPickupPool,
            EnemyConfig enemyConfig,
            WeaponConfig policeWeaponConfig,
            WeaponConfig dropWeaponConfig,
            int dropAmmoCount)
        {
            _owningSpawner = owningSpawner;
            _owningPool = owningPool;
            _weaponPickupPool = weaponPickupPool;
            _dropWeaponConfig = dropWeaponConfig;
            _dropAmmoCount = Mathf.Max(0, dropAmmoCount);
            _deathHandled = false;

            if (enemyConfig != null)
            {
                config = enemyConfig;
            }

            if (policeWeaponConfig != null)
            {
                weaponConfig = policeWeaponConfig;
            }

            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }

            body.linearVelocity = Vector2.zero;
            rangedAttackController.Configure(projectileLauncher, impactFeedbackSpawner, weaponConfig, FactionId, bodyCollider);
        }

        public void OnSpawned()
        {
            CacheReferences();
            _owningPool = null;
            _weaponPickupPool = null;
            _owningSpawner = null;
            _dropWeaponConfig = null;
            _dropAmmoCount = 0;
            _targetPlayer = null;
            _deathHandled = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            body.linearVelocity = Vector2.zero;
        }

        public void OnDespawned()
        {
            _owningSpawner = null;
            _targetPlayer = null;
            _deathHandled = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            body.linearVelocity = Vector2.zero;
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();

            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }
        }

        private void Update()
        {
            rangedAttackController.Tick(Time.deltaTime);

            if (health == null || health.IsDepleted)
            {
                HandleDeath();
                return;
            }

            ResolveTargetPlayer();

            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                transform.position,
                bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            if (_targetPlayer == null || config == null || weaponConfig == null)
            {
                body.linearVelocity = separation;
                return;
            }

            Vector2 toTarget = _targetPlayer.transform.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            float engagementRange = Mathf.Max(0.5f, config.EngagementRange);
            if (distanceToTarget > engagementRange)
            {
                body.linearVelocity = separation;
                return;
            }

            float preferredRange = Mathf.Clamp(weaponConfig.Range * 0.7f, 2f, engagementRange);
            Vector2 direction = distanceToTarget > 0.001f ? toTarget / distanceToTarget : Vector2.zero;

            Vector2 targetVelocity;
            if (distanceToTarget > preferredRange)
            {
                _pathRefreshTimer -= Time.deltaTime;
                if (_currentPath == null || _pathRefreshTimer <= 0f)
                {
                    if (NavMeshPathfinder.TryGetPath(transform.position, _targetPlayer.transform.position, out Vector2[] newPath))
                    {
                        _currentPath = newPath;
                        _currentWaypointIndex = 0;
                    }

                    _pathRefreshTimer = 0.5f;
                }

                Vector2 moveDir = Vector2.zero;
                if (_currentPath != null)
                {
                    moveDir = NavMeshPathfinder.GetSteeringDirection(transform.position, _currentPath, ref _currentWaypointIndex);
                }

                if (moveDir == Vector2.zero)
                {
                    moveDir = direction;
                }

                targetVelocity = moveDir * config.MoveSpeed;
            }
            else
            {
                targetVelocity = Vector2.zero;
            }

            Vector2 combinedVelocity = targetVelocity + separation;
            if (combinedVelocity.sqrMagnitude > config.MoveSpeed * config.MoveSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * config.MoveSpeed;
            }

            body.linearVelocity = combinedVelocity;

            if (visualRoot != null && direction.sqrMagnitude > 0f)
            {
                visualRoot.up = direction;
            }

            if (_currentPath != null)
            {
                for (int i = 0; i < _currentPath.Length - 1; i++)
                {
                    Debug.DrawLine(_currentPath[i], _currentPath[i + 1], Color.cyan);
                }
            }

            if (distanceToTarget <= weaponConfig.Range
                && CoverBlocker.GetNearestBlockDistance(transform.position, direction, Mathf.Min(distanceToTarget, weaponConfig.Range)) >= distanceToTarget
                && !_targetPlayer.IsDisabled)
            {
                if (rangedAttackController.TryFire(transform.position, direction, 0f, this))
                {
                    animationBridge?.TriggerFire();
                }
            }
        }

        private void HandleDeath()
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;
            RecordKill();
            SpawnWeaponPickup();
            _owningSpawner?.NotifyPoliceDespawned(this);

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            if (owningPool != null)
            {
                owningPool.Return(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void RecordKill()
        {
            RunDirector runDirector = null;
            if (ProjectContext.Instance?.Services.TryGet(out RunDirector registeredDirector) == true)
            {
                runDirector = registeredDirector;
            }

            runDirector ??= FindFirstObjectByType<RunDirector>(FindObjectsInactive.Include);
            runDirector?.RecordKill();
        }

        private void SpawnWeaponPickup()
        {
            if (_weaponPickupPool == null || _dropWeaponConfig == null || _dropAmmoCount <= 0)
            {
                return;
            }

            GameObject pickupInstance = _weaponPickupPool.Rent(transform.position, Quaternion.identity);
            WeaponPickup pickup = pickupInstance.GetComponent<WeaponPickup>();
            if (pickup == null)
            {
                _weaponPickupPool.Return(pickupInstance);
                return;
            }

            pickup.Activate(_weaponPickupPool, _dropWeaponConfig, _dropAmmoCount);
        }

        private void ResolveTargetPlayer()
        {
            if (_targetPlayer != null && !_targetPlayer.Health.IsDepleted)
            {
                return;
            }

            _targetPlayer = FindFirstObjectByType<PlayerRoot>();
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            projectileLauncher ??= GetComponent<ProjectileWeaponLauncher>();
            impactFeedbackSpawner ??= GetComponent<WeaponImpactFeedbackSpawner>();
            rangedAttackController ??= GetComponent<RangedAttackController>();
            animationBridge ??= GetComponent<CharacterAnimationBridge>();

            if (visualRoot == null)
            {
                Transform child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }

            if (visualRenderer == null && visualRoot != null)
            {
                visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
            }
        }
    }
}