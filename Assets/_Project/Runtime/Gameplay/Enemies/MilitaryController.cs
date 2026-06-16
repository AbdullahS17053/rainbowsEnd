using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public enum MilitaryState
    {
        Combat,
        Stealing,
        Escaping
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(ProjectileWeaponLauncher))]
    [RequireComponent(typeof(WeaponImpactFeedbackSpawner))]
    [RequireComponent(typeof(RangedAttackController))]
    public sealed class MilitaryController : MonoBehaviour, ITeamAffiliation, IPoolable
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

        private PrefabPool _owningPool;
        private PrefabPool _weaponPickupPool;
        private PrefabPool _goldPickupPool;
        private MilitarySpawner _owningSpawner;
        private WeaponConfig _dropWeaponConfig;
        private int _dropAmmoCount;
        private int _dropGoldAmount;
        private PlayerRoot _targetPlayer;
        private bool _deathHandled;
        private MilitaryState _currentState;
        private PotController _carriedPot;
        private bool _isCarrier;
        private ArenaBoundsAuthoring _arenaBounds;
        private PotRegistry _potRegistry;
        private Vector2 _fleeTarget;

        public FactionId FactionId => FactionId.Military;

        public SpriteRenderer VisualRenderer => visualRenderer;

        public Health Health => health;

        public bool IsCarrier => _isCarrier;

        public void Configure(
            EnemyConfig enemyConfig,
            WeaponConfig militaryWeaponConfig,
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
            weaponConfig = militaryWeaponConfig;
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
            MilitarySpawner owningSpawner,
            PrefabPool owningPool,
            PrefabPool weaponPickupPool,
            PrefabPool goldPickupPool,
            EnemyConfig enemyConfig,
            WeaponConfig militaryWeaponConfig,
            WeaponConfig dropWeaponConfig,
            int dropAmmoCount,
            int dropGoldAmount)
        {
            _owningSpawner = owningSpawner;
            _owningPool = owningPool;
            _weaponPickupPool = weaponPickupPool;
            _goldPickupPool = goldPickupPool;
            _dropWeaponConfig = dropWeaponConfig;
            _dropAmmoCount = Mathf.Max(0, dropAmmoCount);
            _dropGoldAmount = Mathf.Max(0, dropGoldAmount);
            _deathHandled = false;
            _currentState = MilitaryState.Combat;
            _carriedPot = null;
            _isCarrier = false;
            _arenaBounds = null;
            _potRegistry = null;

            if (enemyConfig != null)
            {
                config = enemyConfig;
            }

            if (militaryWeaponConfig != null)
            {
                weaponConfig = militaryWeaponConfig;
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
            _goldPickupPool = null;
            _owningSpawner = null;
            _dropWeaponConfig = null;
            _dropAmmoCount = 0;
            _dropGoldAmount = 0;
            _targetPlayer = null;
            _deathHandled = false;
            _currentState = MilitaryState.Combat;
            _carriedPot = null;
            _isCarrier = false;
            _arenaBounds = null;
            _potRegistry = null;
            body.linearVelocity = Vector2.zero;
        }

        public void OnDespawned()
        {
            if (_carriedPot != null)
            {
                _carriedPot.SetMilitaryCarryActive(false);
                _carriedPot = null;
            }

            _owningSpawner = null;
            _targetPlayer = null;
            _deathHandled = false;
            _isCarrier = false;
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

            switch (_currentState)
            {
                case MilitaryState.Combat:
                    TickCombat();
                    break;
                case MilitaryState.Stealing:
                    TickStealing();
                    break;
                case MilitaryState.Escaping:
                    TickEscaping();
                    break;
            }
        }

        private void TickCombat()
        {
            if (ShouldSwitchToStealing())
            {
                _currentState = MilitaryState.Stealing;
                body.linearVelocity = Vector2.zero;
                return;
            }

            TickPoliceStyleCombat();
        }

        private void TickStealing()
        {
            if (body == null)
            {
                return;
            }

            if (_carriedPot != null && _carriedPot.IsDestroyed)
            {
                _carriedPot = null;
                _isCarrier = false;
            }

            if (_isCarrier)
            {
                _currentState = MilitaryState.Escaping;
                StartEscaping();
                return;
            }

            MilitaryController existingCarrier = FindCarrier();
            if (existingCarrier != null && existingCarrier != this)
            {
                _currentState = MilitaryState.Escaping;
                StartEscaping();
                return;
            }

            PotController targetPot = ResolveNearestStealablePot();
            if (targetPot == null)
            {
                _currentState = MilitaryState.Combat;
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toTarget = (Vector2)targetPot.transform.position - body.position;
            float stopDistance = (bodyCollider as CircleCollider2D)?.radius * 2f + 0.1f ?? 0.5f;
            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                _carriedPot = targetPot;
                _isCarrier = true;
                targetPot.SetMilitaryCarryActive(true);
                _currentState = MilitaryState.Escaping;
                StartEscaping();
                return;
            }

            float moveSpeed = config != null ? Mathf.Max(0f, config.MoveSpeed) : 3f;
            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position, bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            Vector2 combinedVelocity = toTarget.normalized * moveSpeed + separation;
            if (combinedVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * moveSpeed;
            }

            body.linearVelocity = combinedVelocity;

            if (visualRoot != null)
            {
                visualRoot.up = toTarget.normalized;
            }
        }

        private void TickEscaping()
        {
            if (body == null)
            {
                return;
            }

            if (_isCarrier)
            {
                if (_carriedPot == null || _carriedPot.IsDestroyed)
                {
                    _isCarrier = false;
                    _currentState = MilitaryState.Combat;
                    body.linearVelocity = Vector2.zero;
                    return;
                }

                TickCarrierEscaping();
            }
            else
            {
                MilitaryController carrier = FindCarrier();
                if (carrier == null)
                {
                    _currentState = MilitaryState.Combat;
                    body.linearVelocity = Vector2.zero;
                    return;
                }

                TickEscortEscaping(carrier);
            }
        }

        private void TickCarrierEscaping()
        {
            ResolveArenaBounds();
            Vector2 edgeTarget = _arenaBounds != null
                ? _arenaBounds.GetNearestEdgePoint(body.position)
                : body.position + (Vector2)transform.up * 20f;

            Vector2 toEdge = edgeTarget - body.position;
            float fleeSpeed = config != null ? config.MoveSpeed * config.FleeSpeedMultiplier : 5f;

            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position, bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            Vector2 combinedVelocity = toEdge.normalized * fleeSpeed + separation;
            if (combinedVelocity.sqrMagnitude > fleeSpeed * fleeSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * fleeSpeed;
            }

            body.linearVelocity = combinedVelocity;
            _carriedPot.SetCarriedMilitaryPosition(body.position);

            if (visualRoot != null)
            {
                visualRoot.up = toEdge.normalized;
            }

            float edgeArrivalDistance = (bodyCollider as CircleCollider2D)?.radius * 2f + 0.1f ?? 0.5f;
            if (toEdge.sqrMagnitude <= edgeArrivalDistance * edgeArrivalDistance)
            {
                PotController potToDestroy = _carriedPot;
                _carriedPot = null;
                _isCarrier = false;
                potToDestroy.DestroyByMilitary(this);

                _currentState = MilitaryState.Combat;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void TickEscortEscaping(MilitaryController carrier)
        {
            Vector2 toCarrier = carrier.transform.position - transform.position;
            float escortDistance = 1f;
            float moveSpeed = config != null ? config.MoveSpeed * config.FleeSpeedMultiplier : 5f;

            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position, bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            if (toCarrier.sqrMagnitude > escortDistance * escortDistance)
            {
                Vector2 combinedVelocity = toCarrier.normalized * moveSpeed + separation;
                if (combinedVelocity.sqrMagnitude > moveSpeed * moveSpeed)
                {
                    combinedVelocity = combinedVelocity.normalized * moveSpeed;
                }

                body.linearVelocity = combinedVelocity;
            }
            else
            {
                body.linearVelocity = separation;
            }

            if (visualRoot != null)
            {
                visualRoot.up = toCarrier.normalized;
            }
        }

        private void StartEscaping()
        {
            ResolveArenaBounds();
        }

        private void TickPoliceStyleCombat()
        {
            ResolveTargetPlayer();

            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                transform.position, bodyCollider,
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

            Vector2 targetVelocity = distanceToTarget > preferredRange ? direction * config.MoveSpeed : Vector2.zero;
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

            if (distanceToTarget <= weaponConfig.Range)
            {
                rangedAttackController.TryFire(transform.position, direction, 0f, this);
            }
        }

        private bool ShouldSwitchToStealing()
        {
            return _owningSpawner != null && _owningSpawner.ActiveMilitaryCount >= 2;
        }

        private PotController ResolveNearestStealablePot()
        {
            ResolvePotRegistry();

            PotController[] pots = _potRegistry != null
                ? FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                : FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            PotController bestPot = null;
            float bestDistanceSquared = float.MaxValue;
            Vector2 position = transform.position;

            for (int i = 0; i < pots.Length; i++)
            {
                PotController pot = pots[i];
                if (pot == null || pot.IsDestroyed || pot.IsBeingStolenByMilitary)
                {
                    continue;
                }

                float distSq = ((Vector2)pot.transform.position - position).sqrMagnitude;
                if (distSq < bestDistanceSquared)
                {
                    bestPot = pot;
                    bestDistanceSquared = distSq;
                }
            }

            return bestPot;
        }

        private MilitaryController FindCarrier()
        {
            MilitaryController[] allMilitary = FindObjectsByType<MilitaryController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < allMilitary.Length; i++)
            {
                if (allMilitary[i] != this && allMilitary[i].IsCarrier && !allMilitary[i].Health.IsDepleted)
                {
                    return allMilitary[i];
                }
            }

            return null;
        }

        private void HandleDeath()
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;

            if (_carriedPot != null)
            {
                _carriedPot.SetMilitaryCarryActive(false);
                _carriedPot = null;
                _isCarrier = false;
            }

            SpawnWeaponPickup();
            SpawnGoldPickup();
            _owningSpawner?.NotifyMilitaryDespawned(this);

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

        private void SpawnWeaponPickup()
        {
            if (_weaponPickupPool == null || _dropWeaponConfig == null || _dropAmmoCount <= 0)
            {
                return;
            }

            GameObject pickupInstance = _weaponPickupPool.Rent(transform.position, Quaternion.identity);
            if (pickupInstance == null)
            {
                return;
            }

            WeaponPickup pickup = pickupInstance.GetComponent<WeaponPickup>();
            if (pickup == null)
            {
                _weaponPickupPool.Return(pickupInstance);
                return;
            }

            pickup.Activate(_weaponPickupPool, _dropWeaponConfig, _dropAmmoCount);
        }

        private void SpawnGoldPickup()
        {
            if (_goldPickupPool == null || _dropGoldAmount <= 0)
            {
                return;
            }

            GoldPickup goldPickup = _goldPickupPool.Rent<GoldPickup>(transform.position, Quaternion.identity);
            if (goldPickup == null)
            {
                return;
            }

            goldPickup.Activate(_goldPickupPool, _dropGoldAmount);
        }

        private void ResolveTargetPlayer()
        {
            if (_targetPlayer != null && !_targetPlayer.Health.IsDepleted)
            {
                return;
            }

            _targetPlayer = FindFirstObjectByType<PlayerRoot>();
        }

        private void ResolveArenaBounds()
        {
            if (_arenaBounds != null)
            {
                return;
            }

            if (Core.Bootstrap.ProjectContext.Instance?.Services.TryGet(out Core.SceneManagement.ArenaSceneContext arenaCtx) == true)
            {
                _arenaBounds = arenaCtx.ArenaBounds;
            }

            if (_arenaBounds == null)
            {
                _arenaBounds = FindFirstObjectByType<ArenaBoundsAuthoring>(FindObjectsInactive.Exclude);
            }
        }

        private void ResolvePotRegistry()
        {
            if (_potRegistry != null)
            {
                return;
            }

            if (Core.Bootstrap.ProjectContext.Instance?.Services.TryGet(out PotRegistry potRegistry) == true)
            {
                _potRegistry = potRegistry;
                return;
            }

            _potRegistry = FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            projectileLauncher ??= GetComponent<ProjectileWeaponLauncher>();
            impactFeedbackSpawner ??= GetComponent<WeaponImpactFeedbackSpawner>();
            rangedAttackController ??= GetComponent<RangedAttackController>();

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
