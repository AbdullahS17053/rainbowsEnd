using System.Collections.Generic;
using RainbowsEnd.Core.Animation;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Orchestration;
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
        private enum MilitaryCombatPhase
        {
            Firing,
            Telegraphing,
            Repositioning
        }
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
        [SerializeField] private LineRenderer aimTelegraph;


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
        private Vector2[] _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;
        private MilitaryMobConfig _militaryMobConfig;
        private PotController _stealTargetPot;
        private bool _targetPotWasConcealed;
        private int _shotsInBurst;
        private bool _hasTelegraphedCurrentBurst;

        private int _strafeSign;
        private float _combatPhaseTimer;
        private MilitaryCombatPhase _combatPhase;


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
            MilitaryMobConfig mobConfig,
            WeaponConfig dropWeaponConfig,
            int dropAmmoCount,
            int dropGoldAmount)
        {
            _owningSpawner = owningSpawner;
            _owningPool = owningPool;
            _weaponPickupPool = weaponPickupPool;
            _goldPickupPool = goldPickupPool;
            _militaryMobConfig = mobConfig;
            _dropWeaponConfig = dropWeaponConfig;
            _dropAmmoCount = Mathf.Max(0, dropAmmoCount);
            _dropGoldAmount = Mathf.Max(0, dropGoldAmount);
            _deathHandled = false;
            _currentState = MilitaryState.Combat;
            _carriedPot = null;
            _stealTargetPot = null;
            _targetPotWasConcealed = false;
            _isCarrier = false;
            _arenaBounds = null;
            _potRegistry = null;
            ResetBurstState();

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
            ConfigureAimTelegraph();
        }

public void OnSpawned()
        {
            CacheReferences();
            _owningPool = null;
            _weaponPickupPool = null;
            _goldPickupPool = null;
            _owningSpawner = null;
            _militaryMobConfig = null;
            _dropWeaponConfig = null;
            _dropAmmoCount = 0;
            _dropGoldAmount = 0;
            _targetPlayer = null;
            _deathHandled = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            _currentState = MilitaryState.Combat;
            _carriedPot = null;
            _stealTargetPot = null;
            _targetPotWasConcealed = false;
            _isCarrier = false;
            _arenaBounds = null;
            _potRegistry = null;
            ResetBurstState();
            body.linearVelocity = Vector2.zero;
        }

public void OnDespawned()
        {
            if (_carriedPot != null)
            {
                BreakMilitaryCarry();
            }

            _owningSpawner = null;
            _targetPlayer = null;
            _deathHandled = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            _isCarrier = false;
            _stealTargetPot = null;
            ResetBurstState();
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
            if (ShouldPrioritizePlayerCombat())
            {
                TickPoliceStyleCombat();
                return;
            }

            if (ShouldSwitchToStealing())
            {
                _currentState = MilitaryState.Stealing;
                _currentPath = null;
                _currentWaypointIndex = 0;
                _pathRefreshTimer = 0f;
                body.linearVelocity = Vector2.zero;
                return;
            }

            TickPoliceStyleCombat();
        }

private bool ShouldPrioritizePlayerCombat()
        {
            ResolveTargetPlayer();
            if (_targetPlayer == null || weaponConfig == null || _targetPlayer.IsDisabled)
            {
                return false;
            }

            Vector2 toTarget = _targetPlayer.transform.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            Vector2 direction = distanceToTarget > 0.001f ? toTarget / distanceToTarget : Vector2.up;
            bool hasLineOfSight = distanceToTarget <= 0.001f
                || CoverBlocker.GetNearestBlockDistance(transform.position, direction, distanceToTarget) >= distanceToTarget;

            return MilitaryCombatPriorityUtility.ShouldPrioritizeCombat(
                distanceToTarget,
                weaponConfig.Range,
                true,
                hasLineOfSight);
        }


private void TickStealing()
        {
            if (body == null)
            {
                return;
            }

            if (_carriedPot != null && _carriedPot.IsDestroyed)
            {
                BreakMilitaryCarry();
                return;
            }

            if (_isCarrier)
            {
                _currentState = MilitaryState.Escaping;
                StartEscaping();
                return;
            }

            if (_stealTargetPot == null || _stealTargetPot.IsDestroyed || _stealTargetPot.IsBeingStolenByMilitary)
            {
                _stealTargetPot = ResolveNearestStealablePot();
                if (_stealTargetPot == null)
                {
                    _currentState = MilitaryState.Combat;
                    body.linearVelocity = Vector2.zero;
                    return;
                }

                _targetPotWasConcealed = IsPotInConcealment(_stealTargetPot);
                _currentPath = null;
                _currentWaypointIndex = 0;
                _pathRefreshTimer = 0f;
            }

            PotController targetPot = _stealTargetPot;
            Vector2 toTarget = (Vector2)targetPot.transform.position - body.position;
            float stopDistance = (bodyCollider as CircleCollider2D)?.radius * 2f + 0.1f ?? 0.5f;
            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                if (_targetPotWasConcealed)
                {
                    BeginMilitaryCarry(targetPot, null);
                }
                else
                {
                    MilitaryController partner = FindCarryPartner(targetPot, true);
                    if (partner != null)
                    {
                        BeginMilitaryCarry(targetPot, partner);
                    }
                    else
                    {
                        body.linearVelocity = Vector2.zero;
                    }
                }

                return;
            }

            _pathRefreshTimer -= Time.deltaTime;
            if (_currentPath == null || _pathRefreshTimer <= 0f)
            {
                if (NavMeshPathfinder.TryGetPath(body.position, targetPot.transform.position, out Vector2[] newPath))
                {
                    _currentPath = newPath;
                    _currentWaypointIndex = 0;
                }
                else
                {
                    _currentPath = null;
                    _currentWaypointIndex = 0;
                }

                _pathRefreshTimer = 0.5f;
            }

            Vector2 moveDirection = _currentPath != null
                ? NavMeshPathfinder.GetSteeringDirection(body.position, _currentPath, ref _currentWaypointIndex)
                : Vector2.zero;
            if (moveDirection == Vector2.zero
                && CoverBlocker.GetNearestBlockDistance(body.position, toTarget.normalized, toTarget.magnitude) >= toTarget.magnitude)
            {
                moveDirection = toTarget.normalized;
            }

            float moveSpeed = config != null ? Mathf.Max(0f, config.MoveSpeed) : 3f;
            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position, bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            Vector2 combinedVelocity = moveDirection * moveSpeed + separation;
            if (combinedVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * moveSpeed;
            }

            body.linearVelocity = combinedVelocity;

            if (visualRoot != null && moveDirection != Vector2.zero)
            {
                visualRoot.up = moveDirection;
            }
        }

private void TickEscaping()
        {
            if (body == null)
            {
                return;
            }

            if (_carriedPot == null || _carriedPot.IsDestroyed)
            {
                BreakMilitaryCarry();
                return;
            }

            if (_isCarrier)
            {
                TickCarrierEscaping();
                return;
            }

            MilitaryController carrier = FindCarrier(_carriedPot);
            if (carrier == null)
            {
                BreakMilitaryCarry();
                return;
            }

            TickEscortEscaping(carrier);
        }

private void TickCarrierEscaping()
        {
            ResolveArenaBounds();
            Vector2 edgeTarget = _arenaBounds != null
                ? _arenaBounds.GetNearestEdgePoint(body.position)
                : body.position + (Vector2)transform.up * 20f;

            if (!_targetPotWasConcealed)
            {
                MilitaryController partner = FindCarryPartner(_carriedPot);
                float followDistance = _militaryMobConfig != null ? _militaryMobConfig.CarryFollowDistance : 2.5f;
                if (partner == null || ((Vector2)partner.transform.position - body.position).sqrMagnitude > followDistance * followDistance)
                {
                    BreakMilitaryCarry();
                    return;
                }
            }

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
                MilitaryController partner = FindCarryPartner(potToDestroy);
                partner?.ClearCarryState();
                ClearCarryState();
                potToDestroy.DestroyByMilitary(this);
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
                ResetBurstState();
                body.linearVelocity = separation;
                return;
            }

            Vector2 toTarget = _targetPlayer.transform.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            float engagementRange = Mathf.Max(0.5f, config.EngagementRange);
            if (distanceToTarget > engagementRange)
            {
                ResetBurstState();
                body.linearVelocity = separation;
                return;
            }

            float preferredRange = Mathf.Clamp(weaponConfig.Range * 0.7f, 2f, engagementRange);
            Vector2 direction = distanceToTarget > 0.001f ? toTarget / distanceToTarget : (Vector2)transform.up;
            bool canShoot = distanceToTarget <= weaponConfig.Range
                && CoverBlocker.GetNearestBlockDistance(transform.position, direction, Mathf.Min(distanceToTarget, weaponConfig.Range)) >= distanceToTarget
                && !_targetPlayer.IsDisabled;

            if (_combatPhase == MilitaryCombatPhase.Repositioning)
            {
                Vector2 strafeDirection = new Vector2(-direction.y, direction.x) * _strafeSign;
                Vector2 repositionVelocity = strafeDirection * config.MoveSpeed + separation;
                if (repositionVelocity.sqrMagnitude > config.MoveSpeed * config.MoveSpeed)
                {
                    repositionVelocity = repositionVelocity.normalized * config.MoveSpeed;
                }

                body.linearVelocity = repositionVelocity;
                SetAimTelegraphVisible(false);
                if (visualRoot != null)
                {
                    visualRoot.up = direction;
                }

                _combatPhaseTimer -= Time.deltaTime;
                if (_combatPhaseTimer <= 0f)
                {
                    _shotsInBurst = 0;
                    _hasTelegraphedCurrentBurst = false;
                    _combatPhase = MilitaryCombatPhase.Firing;
                }

                return;
            }

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

            if (!canShoot)
            {
                ResetBurstState();
                return;
            }

            if (_combatPhase == MilitaryCombatPhase.Telegraphing)
            {
                UpdateAimTelegraph(direction);
                _combatPhaseTimer -= Time.deltaTime;
                if (_combatPhaseTimer <= 0f)
                {
                    SetAimTelegraphVisible(false);
                    _combatPhase = MilitaryCombatPhase.Firing;
                }

                return;
            }

            if (_shotsInBurst >= ResolveBurstShotCount())
            {
                BeginReposition();
                return;
            }

            if (_militaryMobConfig != null
                && MilitaryBurstUtility.ShouldStartTelegraph(
                    _shotsInBurst,
                    _hasTelegraphedCurrentBurst,
                    _militaryMobConfig.AimTelegraphSeconds))
            {
                BeginAimTelegraph(direction);
                return;
            }

            if (rangedAttackController.TryFire(transform.position, direction, 0f, this))
            {
                _shotsInBurst++;
                animationBridge?.TriggerFire();
                if (_shotsInBurst >= ResolveBurstShotCount())
                {
                    BeginReposition();
                }
            }
        }

private bool ShouldSwitchToStealing()
        {
            ResolvePotRegistry();
            PotController targetPot = ResolveNearestStealablePot();
            if (targetPot == null)
            {
                return false;
            }

            if (IsPotInConcealment(targetPot))
            {
                return true;
            }

            int requiredMobCount = _militaryMobConfig != null
                ? Mathf.Max(1, _militaryMobConfig.StandardCarryMobCount)
                : 2;
            return _owningSpawner != null && _owningSpawner.ActiveMilitaryCount >= requiredMobCount;
        }

        private PotController ResolveNearestStealablePot()
        {
            ResolvePotRegistry();

            IReadOnlyList<PotController> pots = _potRegistry != null
                ? _potRegistry.RegisteredPots
                : FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            PotController bestPot = null;
            float bestDistanceSquared = float.MaxValue;
            Vector2 position = transform.position;

            for (int index = 0; index < pots.Count; index++)
            {
                PotController pot = pots[index];
                if (pot == null || pot.IsDestroyed || pot.IsBeingStolenByMilitary || !pot.HasGold)
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

private MilitaryController FindCarrier(PotController pot)
        {
            MilitaryController[] allMilitary = FindObjectsByType<MilitaryController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < allMilitary.Length; i++)
            {
                if (allMilitary[i] != this
                    && allMilitary[i].IsCarrier
                    && allMilitary[i]._carriedPot == pot
                    && allMilitary[i].Health != null
                    && !allMilitary[i].Health.IsDepleted)
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
            BreakMilitaryCarry();
            RecordKill();

            MilitaryDropType dropType = _militaryMobConfig != null
                ? EnemyDropSelectionUtility.SelectMilitaryDropType(
                    Random.value,
                    Random.value,
                    _militaryMobConfig.DeathDropChance,
                    _militaryMobConfig.WeaponRewardChance)
                : MilitaryDropType.Weapon;

            if (dropType == MilitaryDropType.Weapon)
            {
                SpawnWeaponPickup();
            }
            else if (dropType == MilitaryDropType.Gold)
            {
                SpawnGoldPickup();
            }

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

        private void RecordKill()
        {
            RunDirector runDirector = null;
            if (Core.Bootstrap.ProjectContext.Instance?.Services.TryGet(out RunDirector registeredDirector) == true)
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

                private void BeginMilitaryCarry(PotController pot, MilitaryController partner)
        {
            _carriedPot = pot;
            _stealTargetPot = pot;
            _isCarrier = true;
            _currentState = MilitaryState.Escaping;
            pot.SetMilitaryCarryActive(true);
            StartEscaping();

            if (partner == null)
            {
                return;
            }

            partner._carriedPot = pot;
            partner._stealTargetPot = pot;
            partner._targetPotWasConcealed = false;
            partner._isCarrier = false;
            partner._currentState = MilitaryState.Escaping;
            partner.StartEscaping();
        }

private MilitaryController FindCarryPartner(PotController pot, bool forAssembly = false)
        {
            MilitaryController[] allMilitary = FindObjectsByType<MilitaryController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float partnerRadius = forAssembly
                ? (_militaryMobConfig != null ? _militaryMobConfig.CarryAssemblyRadius : 1.5f)
                : (_militaryMobConfig != null ? _militaryMobConfig.CarryFollowDistance : 2.5f);
            float partnerRadiusSquared = partnerRadius * partnerRadius;
            for (int i = 0; i < allMilitary.Length; i++)
            {
                MilitaryController candidate = allMilitary[i];
                if (candidate == this
                    || candidate._stealTargetPot != pot
                    || candidate.Health == null
                    || candidate.Health.IsDepleted)
                {
                    continue;
                }

                if (forAssembly)
                {
                    if (candidate._currentState != MilitaryState.Stealing || candidate._carriedPot != null)
                    {
                        continue;
                    }
                }
                else if (candidate._currentState != MilitaryState.Escaping || candidate._carriedPot != pot)
                {
                    continue;
                }

                if (((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude <= partnerRadiusSquared)
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool IsPotInConcealment(PotController pot)
        {
            if (pot == null)
            {
                return false;
            }

            ConcealmentZone[] zones = FindObjectsByType<ConcealmentZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < zones.Length; index++)
            {
                if (zones[index] != null && zones[index].Contains(pot.transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        private void BreakMilitaryCarry()
        {
            PotController pot = _carriedPot;
            MilitaryController partner = _isCarrier ? FindCarryPartner(pot) : FindCarrier(pot);
            partner?.ClearCarryState();
            ClearCarryState();
            pot?.SetMilitaryCarryActive(false);
        }

        private void ClearCarryState()
        {
            _carriedPot = null;
            _stealTargetPot = null;
            _targetPotWasConcealed = false;
            _isCarrier = false;
            _currentState = MilitaryState.Combat;
            body.linearVelocity = Vector2.zero;
        }

        private int ResolveBurstShotCount()
        {
            return _militaryMobConfig != null ? Mathf.Max(1, _militaryMobConfig.BurstShotCount) : 3;
        }

private void BeginReposition()
        {
            _combatPhase = MilitaryCombatPhase.Repositioning;
            _combatPhaseTimer = _militaryMobConfig != null ? Mathf.Max(0f, _militaryMobConfig.BurstPauseSeconds) : 1f;
            _strafeSign = Random.value < 0.5f ? -1 : 1;
            SetAimTelegraphVisible(false);
        }

private void BeginAimTelegraph(Vector2 direction)
        {
            _hasTelegraphedCurrentBurst = true;
            _combatPhase = MilitaryCombatPhase.Telegraphing;
            _combatPhaseTimer = Mathf.Max(0f, _militaryMobConfig.AimTelegraphSeconds);
            ConfigureAimTelegraph();
            UpdateAimTelegraph(direction);
            SetAimTelegraphVisible(true);
        }

        private void UpdateAimTelegraph(Vector2 direction)
        {
            if (aimTelegraph == null)
            {
                return;
            }

            Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)(normalizedDirection * Mathf.Max(0.5f, weaponConfig != null ? weaponConfig.Range : 8f));
            aimTelegraph.positionCount = 2;
            aimTelegraph.SetPosition(0, start);
            aimTelegraph.SetPosition(1, end);
        }

private void ResetBurstState()
        {
            _combatPhase = MilitaryCombatPhase.Firing;
            _combatPhaseTimer = 0f;
            _shotsInBurst = 0;
            _hasTelegraphedCurrentBurst = false;
            _strafeSign = 1;
            SetAimTelegraphVisible(false);
        }

private void ConfigureAimTelegraph()
        {
            SetAimTelegraphVisible(false);
            if (aimTelegraph == null)
            {
                return;
            }

            aimTelegraph.useWorldSpace = true;
            aimTelegraph.positionCount = 2;
            aimTelegraph.startWidth = _militaryMobConfig != null ? _militaryMobConfig.AimTelegraphStartWidth : 0.08f;
            aimTelegraph.endWidth = _militaryMobConfig != null ? _militaryMobConfig.AimTelegraphEndWidth : 0.02f;
            aimTelegraph.startColor = _militaryMobConfig != null ? _militaryMobConfig.AimTelegraphColor : Color.red;
            aimTelegraph.endColor = aimTelegraph.startColor;
            aimTelegraph.sortingOrder = 30;
        }

private void SetAimTelegraphVisible(bool isVisible)
        {
            aimTelegraph ??= GetComponent<LineRenderer>();
            if (aimTelegraph != null)
            {
                aimTelegraph.enabled = isVisible;
            }
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
            animationBridge ??= GetComponent<CharacterAnimationBridge>();

                        aimTelegraph ??= GetComponent<LineRenderer>();
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
