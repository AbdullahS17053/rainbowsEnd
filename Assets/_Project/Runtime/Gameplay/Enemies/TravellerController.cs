using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public enum TravellerState
    {
        Seeking,
        Draining,
        Fleeing
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PotDrain))]
    public sealed class TravellerController : MonoBehaviour, ITeamAffiliation, IPoolable
    {
        [SerializeField] private EnemyConfig config;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CircleCollider2D bodyCollider;
        [SerializeField] private Health health;
        [SerializeField] private PotDrain potDrain;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Color roamingColor = new Color(0.88f, 0.45f, 0.24f, 1f);
        [SerializeField] private Color drainingColor = new Color(0.95f, 0.33f, 0.24f, 1f);

        private readonly List<PotController> _candidatePots = new List<PotController>();
        private readonly List<TravellerTargetCandidate> _candidateTargets = new List<TravellerTargetCandidate>();

        private EnemySpawner _owningSpawner;
        private PrefabPool _owningPool;
        private PrefabPool _goldPickupPool;
        private PotRegistry _potRegistry;
        private PotController _currentTarget;
        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private int _goldDropAmount;
        private float _targetRefreshTimerSeconds;
        private bool _deathHandled;
        private TravellerState _currentState;
        private Vector2 _fleeTarget;
        private ArenaBoundsAuthoring _arenaBounds;
        private bool _isPatroling;
        private Vector2 _patrolEdgeTarget;
        private int _patrolDirection;
        private Vector2[] _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;

        public FactionId FactionId => FactionId.Civilian;

        public EnemyConfig Config => config;

        public void ConfigureVisualFeedback(Color drainStateColor)
        {
            drainingColor = drainStateColor;
            CacheBaseColor(forceRefresh: true);
            ApplyVisualState(isDraining: false);
        }

        public void Configure(
            EnemyConfig enemyConfig,
            Rigidbody2D rigidbody2D,
            CircleCollider2D collider2D,
            Health travellerHealth,
            PotDrain drainBehaviour,
            SpriteRenderer renderer)
        {
            config = enemyConfig;
            body = rigidbody2D;
            bodyCollider = collider2D;
            health = travellerHealth;
            potDrain = drainBehaviour;
            visualRenderer = renderer;

            CacheReferences();
            ConfigureBody();
            potDrain?.Configure(this, bodyCollider);
            CacheBaseColor(forceRefresh: true);
            ApplyVisualState(isDraining: false);
        }

        public void Activate(EnemySpawner owningSpawner, PrefabPool owningPool, PrefabPool goldPickupPool, EnemyConfig enemyConfig, int goldDropAmount)
        {
            _owningSpawner = owningSpawner;
            _owningPool = owningPool;
            _goldPickupPool = goldPickupPool;
            config = enemyConfig;
            _currentTarget = null;
            _goldDropAmount = 0;
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;
            _currentState = TravellerState.Seeking;
            _fleeTarget = Vector2.zero;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            _arenaBounds = null;

            _isPatroling = false;

            CacheReferences();
            ConfigureBody();
            potDrain?.Configure(this, bodyCollider);
            potDrain?.ResetStolenGold();
            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }

            ApplyVisualState(isDraining: false);
            _owningSpawner?.NotifyTravellerSpawned(this);
        }

        public void OnSpawned()
        {
            CacheReferences();
            ConfigureBody();
            CacheBaseColor(forceRefresh: true);
            _currentTarget = null;
            _goldDropAmount = 0;
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;
            _currentState = TravellerState.Seeking;
            _fleeTarget = Vector2.zero;
            _arenaBounds = null;
            _isPatroling = false;
            potDrain?.ResetStolenGold();
            ApplyVisualState(isDraining: false);
        }

        public void OnDespawned()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            _owningSpawner?.NotifyTravellerDespawned(this);
            _owningSpawner = null;
            _owningPool = null;
            _goldPickupPool = null;
            _currentTarget = null;
            _goldDropAmount = 0;
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;
            _currentState = TravellerState.Seeking;
            _fleeTarget = Vector2.zero;
            _arenaBounds = null;
            _isPatroling = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            ApplyVisualState(isDraining: false);
        }

        private void Reset()
        {
            CacheReferences();
            ConfigureBody();
            CacheBaseColor(forceRefresh: true);
        }

        private void OnValidate()
        {
            CacheReferences();
            ConfigureBody();
            potDrain?.Configure(this, bodyCollider);
            CacheBaseColor(forceRefresh: true);
            ApplyVisualState(isDraining: false);
        }

        private void Awake()
        {
            CacheReferences();
            ResolveDefaults();
            ConfigureBody();
            potDrain?.Configure(this, bodyCollider);
            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }

            CacheBaseColor(forceRefresh: true);
            ApplyVisualState(isDraining: false);
        }

        private void Update()
        {
            if (health != null && health.IsDepleted)
            {
                HandleDeath();
                return;
            }

            switch (_currentState)
            {
                case TravellerState.Seeking:
                    TickSeeking();
                    break;
                case TravellerState.Draining:
                    TickDraining();
                    break;
                case TravellerState.Fleeing:
                    TickFleeing();
                    break;
            }
        }

        private void TickSeeking()
        {
            bool isDraining = potDrain != null && potDrain.Tick(Time.deltaTime, config != null ? config.StealRate : 1f);
            if (isDraining)
            {
                _currentState = TravellerState.Draining;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }

                _currentTarget = potDrain.CurrentPot;
                ApplyVisualState(isDraining: true);
                return;
            }

            RefreshTargetIfNeeded();
            MoveTowardsTarget();
            ApplyVisualState(isDraining: false);
        }

        private void TickDraining()
        {
            bool isDraining = potDrain != null && potDrain.Tick(Time.deltaTime, config != null ? config.StealRate : 1f);
            if (!isDraining)
            {
                StartFleeing();
                return;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            ApplyVisualState(isDraining: true);

            if (potDrain.StolenGold >= (config != null ? config.StealCapacity : 10f))
            {
                StartFleeing();
            }
        }

        private void TickFleeing()
        {
            if (body == null)
            {
                return;
            }

            Vector2 toFleeTarget = _fleeTarget - body.position;
            float fleeArrivalDistance = ResolveStopDistance() * 2f;
            if (toFleeTarget.sqrMagnitude <= fleeArrivalDistance * fleeArrivalDistance)
            {
                DespawnFleeing();
                return;
            }

            float fleeSpeed = config != null ? config.MoveSpeed * config.FleeSpeedMultiplier : 5f;
            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position,
                bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            Vector2 targetVelocity = toFleeTarget.normalized * fleeSpeed;
            Vector2 combinedVelocity = targetVelocity + separation;
            if (combinedVelocity.sqrMagnitude > fleeSpeed * fleeSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * fleeSpeed;
            }

            body.linearVelocity = combinedVelocity;
            ApplyVisualState(isDraining: false);
        }

        private void StartFleeing()
        {
            _currentState = TravellerState.Fleeing;
            ResolveArenaBounds();

            if (_arenaBounds != null)
            {
                _fleeTarget = _arenaBounds.GetNearestEdgePoint(body != null ? body.position : transform.position);
            }
            else
            {
                _fleeTarget = body != null
                    ? body.position + (Vector2)transform.up * 20f
                    : transform.position + transform.up * 20f;
            }

            _isPatroling = false;
            _currentTarget = null;
            ApplyVisualState(isDraining: false);
        }

        private void DespawnFleeing()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            if (_owningPool != null)
            {
                _owningPool.Return(gameObject);
            }
            else
            {
                _deathHandled = true;
                _owningSpawner?.NotifyTravellerDespawned(this);
                gameObject.SetActive(false);
            }
        }

        private void ResolveArenaBounds()
        {
            if (_arenaBounds != null)
            {
                return;
            }

            if (ProjectContext.Instance?.Services.TryGet(out Core.SceneManagement.ArenaSceneContext arenaCtx) == true)
            {
                _arenaBounds = arenaCtx.ArenaBounds;
            }

            if (_arenaBounds == null)
            {
                _arenaBounds = FindFirstObjectByType<ArenaBoundsAuthoring>(FindObjectsInactive.Exclude);
            }
        }

        private void InitPatrol(Vector2 currentEdgePoint)
        {
            _isPatroling = true;
            _patrolDirection = Random.value > 0.5f ? 1 : -1;

            Rect rect = _arenaBounds.WorldRect;
            int nearestCorner = GetNearestCornerIndex(rect, currentEdgePoint);
            _patrolEdgeTarget = GetCorner(rect, (nearestCorner + _patrolDirection + 4) % 4);
        }

        private void AdvancePatrolCorner()
        {
            Rect rect = _arenaBounds.WorldRect;
            int currentCorner = GetNearestCornerIndex(rect, _patrolEdgeTarget);
            _patrolEdgeTarget = GetCorner(rect, (currentCorner + _patrolDirection + 4) % 4);
        }

        private static Vector2 GetCorner(Rect rect, int index)
        {
            return index switch
            {
                0 => new Vector2(rect.xMin, rect.yMin),
                1 => new Vector2(rect.xMin, rect.yMax),
                2 => new Vector2(rect.xMax, rect.yMax),
                3 => new Vector2(rect.xMax, rect.yMin),
                _ => new Vector2(rect.xMin, rect.yMin),
            };
        }

        private static int GetNearestCornerIndex(Rect rect, Vector2 point)
        {
            int nearestIndex = 0;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                float d = Vector2.Distance(point, GetCorner(rect, i));
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<CircleCollider2D>();
            health ??= GetComponent<Health>();
            potDrain ??= GetComponent<PotDrain>();
            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void ResolveDefaults()
        {
            if (config == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                config = ProjectContext.Instance.ConfigRegistry.TravellerConfig;
            }
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearDamping = 0f;
        }

        private void RefreshTargetIfNeeded()
        {
            _targetRefreshTimerSeconds -= Time.deltaTime;

            bool hasCurrentTarget = _currentTarget != null;
            bool currentTargetDestroyed = hasCurrentTarget && _currentTarget.IsDestroyed;
            bool currentTargetHarvesting = hasCurrentTarget && _currentTarget.HarvestStatus == PotHarvestStatus.Harvesting;

            if (hasCurrentTarget && !currentTargetDestroyed && currentTargetHarvesting && _targetRefreshTimerSeconds > 0f)
            {
                return;
            }

            _candidatePots.Clear();
            _candidateTargets.Clear();

            ResolvePotRegistry();

            IReadOnlyList<PotController> pots = _potRegistry != null
                ? _potRegistry.RegisteredPots
                : FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int index = 0; index < pots.Count; index++)
            {
                PotController pot = pots[index];
                if (pot == null || pot.IsDestroyed)
                {
                    continue;
                }

                _candidatePots.Add(pot);
                _candidateTargets.Add(new TravellerTargetCandidate(pot.transform.position, pot.HarvestStatus == PotHarvestStatus.Harvesting, pot.HasGold));
            }

            int selectedIndex = TravellerTargetSelectionUtility.SelectBestCandidateIndex(
                transform.position,
                _candidateTargets,
                config != null ? config.EngagementRange : 0f);

            _currentTarget = selectedIndex >= 0 ? _candidatePots[selectedIndex] : null;
            _targetRefreshTimerSeconds = config != null ? Mathf.Max(0.05f, config.TargetRefreshSeconds) : 0.25f;
        }

        private void MoveTowardsTarget()
        {
            if (body == null)
            {
                return;
            }

            float moveSpeed = config != null ? Mathf.Max(0f, config.MoveSpeed) : 0f;
            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position,
                bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            if (_currentTarget == null)
            {
                if (_arenaBounds == null)
                {
                    ResolveArenaBounds();
                }

                if (_arenaBounds != null)
                {
                    Vector2 nearestEdge = _arenaBounds.GetNearestEdgePoint(body.position);
                    float edgeArrivalDistance = ResolveStopDistance() * 2f;

                    if (Vector2.Distance(body.position, nearestEdge) > edgeArrivalDistance)
                    {
                        // Phase 1 — Retreat to nearest edge
                        float fleeSpeed = config != null ? config.MoveSpeed * config.FleeSpeedMultiplier : 5f;
                        Vector2 toEdge = nearestEdge - body.position;
                        Vector2 retreatVelocity = toEdge.normalized * fleeSpeed;
                        Vector2 retreatCombined = retreatVelocity + separation;
                        if (retreatCombined.sqrMagnitude > fleeSpeed * fleeSpeed)
                        {
                            retreatCombined = retreatCombined.normalized * fleeSpeed;
                        }

                        _isPatroling = false;
                        body.linearVelocity = retreatCombined;
                        return;
                    }

                    // Phase 2 — Patrol along the perimeter
                    if (!_isPatroling)
                    {
                        InitPatrol(nearestEdge);
                    }

                    Vector2 toPatrolTarget = _patrolEdgeTarget - body.position;
                    if (Vector2.Distance(body.position, _patrolEdgeTarget) < edgeArrivalDistance)
                    {
                        AdvancePatrolCorner();
                        toPatrolTarget = _patrolEdgeTarget - body.position;
                    }

                    float patrolSpeed = moveSpeed * 0.25f;
                    Vector2 patrolVelocity = toPatrolTarget.normalized * patrolSpeed;
                    Vector2 patrolCombined = patrolVelocity + separation;
                    if (patrolCombined.sqrMagnitude > moveSpeed * moveSpeed)
                    {
                        patrolCombined = patrolCombined.normalized * moveSpeed;
                    }

                    body.linearVelocity = patrolCombined;
                    return;
                }

                // Fallback — no arena bounds resolved
                body.linearVelocity = separation;
                return;
            }

            _isPatroling = false;

            Vector2 toTarget = (Vector2)_currentTarget.transform.position - body.position;
            float stopDistance = ResolveStopDistance();
            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                body.linearVelocity = separation;
                return;
            }

            _pathRefreshTimer -= Time.deltaTime;
            if (_currentPath == null || _pathRefreshTimer <= 0f)
            {
                if (NavMeshPathfinder.TryGetPath(body.position, _currentTarget.transform.position, out Vector2[] newPath))
                {
                    _currentPath = newPath;
                    _currentWaypointIndex = 0;
                }

                _pathRefreshTimer = 0.5f;
            }

            Vector2 moveDir = Vector2.zero;
            if (_currentPath != null)
            {
                moveDir = NavMeshPathfinder.GetSteeringDirection(body.position, _currentPath, ref _currentWaypointIndex);
            }

            if (moveDir == Vector2.zero)
            {
                moveDir = toTarget.normalized;
            }

            Vector2 targetVelocity = moveDir * moveSpeed;
            Vector2 combinedVelocity = targetVelocity + separation;
            if (combinedVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * moveSpeed;
            }

            body.linearVelocity = combinedVelocity;

            if (_currentPath != null)
            {
                for (int i = 0; i < _currentPath.Length - 1; i++)
                {
                    Debug.DrawLine(_currentPath[i], _currentPath[i + 1], Color.cyan);
                }
            }
        }

        private float ResolveStopDistance()
        {
            float colliderRadius = bodyCollider != null ? Mathf.Max(bodyCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y), 0.1f) : 0.1f;
            return colliderRadius + 0.1f;
        }

        private void ApplyVisualState(bool isDraining)
        {
            if (visualRenderer == null)
            {
                return;
            }

            Color targetColor = isDraining
                ? drainingColor
                : (_hasBaseColor ? _baseColor : roamingColor);

            visualRenderer.color = targetColor;
        }

        private void CacheBaseColor(bool forceRefresh = false)
        {
            if (visualRenderer != null && (forceRefresh || !_hasBaseColor))
            {
                _baseColor = visualRenderer.color;
                _hasBaseColor = true;
            }
        }

        private void HandleDeath()
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            int stolenGold = potDrain != null ? Mathf.FloorToInt(potDrain.StolenGold) : 0;
            int pocketChange = Mathf.Max(0, _goldDropAmount);
            int totalDrop = pocketChange + stolenGold;
            if (totalDrop > 0)
            {
                SpawnGoldPickup(totalDrop);
            }

            if (_owningPool != null)
            {
                _owningPool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SpawnGoldPickup(int goldAmount)
        {
            if (_goldPickupPool == null || goldAmount <= 0)
            {
                return;
            }

            GoldPickup goldPickup = _goldPickupPool.Rent<GoldPickup>(transform.position, Quaternion.identity);
            goldPickup.Activate(_goldPickupPool, goldAmount);
        }

        private void ResolvePotRegistry()
        {
            if (_potRegistry != null)
            {
                return;
            }

            if (ProjectContext.Instance?.Services.TryGet(out PotRegistry potRegistry) == true)
            {
                _potRegistry = potRegistry;
                return;
            }

            _potRegistry = FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
        }
    }
}