using TMPro;
using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Orchestration;
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
        [SerializeField] private TextMeshPro theftLabel;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject carriedBag;
        [SerializeField] private HealthStageFeedbackPresenter healthFeedbackPresenter;


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
        private bool _escapeHandled;
        private TravellerState _currentState;
        private Vector2 _fleeTarget;
        private ArenaBoundsAuthoring _arenaBounds;
        private bool _isPatroling;
        private Vector2 _patrolEdgeTarget;
        private int _patrolDirection;
        private Vector2[] _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;
        private int _displayedStolenGold = -1;
        private bool _hasRunToCarriedPotParameter;
        private bool _hasCarryLootParameter;
        private bool _hasPickParameter;
        private bool _hasDieParameter;
        private Camera _activeCamera;
        private Transform _headTransform;

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
            RefreshTheftLabel();
            ResetPresentation();
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
            _escapeHandled = false;
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
            RefreshTheftLabel();
            ResetPresentation();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (_hasDieParameter)
                {
                    animator.ResetTrigger("Die");
                }
                animator.Play("Idle", 0, 0f);
            }
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
            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }
            if (body != null)
            {
                body.simulated = true;
            }
            CacheBaseColor(forceRefresh: true);
            _currentTarget = null;
            _goldDropAmount = 0;
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;
            _escapeHandled = false;
            _currentState = TravellerState.Seeking;
            _fleeTarget = Vector2.zero;
            _arenaBounds = null;
            _isPatroling = false;
            potDrain?.ResetStolenGold();
            RefreshTheftLabel();
            ResetPresentation();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (_hasDieParameter)
                {
                    animator.ResetTrigger("Die");
                }
                animator.Play("Idle", 0, 0f);
            }
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
            _escapeHandled = false;
            _currentState = TravellerState.Seeking;
            _fleeTarget = Vector2.zero;
            _arenaBounds = null;
            _isPatroling = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            ClearTheftLabel();
            ResetPresentation();
            ApplyVisualState(isDraining: false);
        }

        private void RefreshTheftLabel()
        {
            ClearTheftLabel();
        }


        private void ClearTheftLabel()
        {
            _displayedStolenGold = -1;
            if (theftLabel == null)
            {
                return;
            }

            theftLabel.text = string.Empty;
            if (theftLabel.gameObject.activeSelf)
            {
                theftLabel.gameObject.SetActive(false);
            }
        }

        private void UpdateTheftLabelTransform()
        {
            if (theftLabel == null || !theftLabel.gameObject.activeSelf)
            {
                return;
            }

            Camera cam = ResolveCamera();
            if (cam != null)
            {
                // Always face camera directly (screen-aligned billboard)
                theftLabel.transform.rotation = cam.transform.rotation;

                // Position cleanly above traveller's head
                Transform head = ResolveHeadTransform();
                if (head != null)
                {
                    theftLabel.transform.position = head.position + (cam.transform.up * 0.45f) - (cam.transform.forward * 0.1f);
                }
                else
                {
                    theftLabel.transform.position = transform.position + (cam.transform.up * 3f) - (cam.transform.forward * 0.1f);
                }
            }
            else
            {
                theftLabel.transform.rotation = Quaternion.identity;
                theftLabel.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            }
        }

        private Camera ResolveCamera()
        {
            if (_activeCamera != null && _activeCamera.isActiveAndEnabled)
            {
                return _activeCamera;
            }

            if (ProjectContext.Instance?.Services.TryGet(out Core.SceneManagement.ArenaSceneContext arenaCtx) == true && arenaCtx.MainCamera != null)
            {
                _activeCamera = arenaCtx.MainCamera;
                return _activeCamera;
            }

            _activeCamera = Camera.main;
            return _activeCamera;
        }

        private Transform ResolveHeadTransform()
        {
            if (_headTransform != null)
            {
                return _headTransform;
            }

            if (animator != null && animator.isHuman)
            {
                _headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                if (_headTransform != null)
                {
                    return _headTransform;
                }
            }

            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i].name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                {
                    _headTransform = allChildren[i];
                    return _headTransform;
                }
            }

            return null;
        }


        private void OnDisable()
        {
            ClearTheftLabel();
            ResetPresentation();
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
            if (Application.isPlaying)
            {
                ApplyVisualState(isDraining: false);
                ResetPresentation();
            }
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
            ResetPresentation();
            RefreshTheftLabel();
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

            RefreshTheftLabel();
            RefreshPresentation();
        }

        private void LateUpdate()
        {
            UpdateTheftLabelTransform();
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
            bool isOutside = _arenaBounds != null && !_arenaBounds.Contains(body.position);
            bool reachedTarget = toFleeTarget.sqrMagnitude <= fleeArrivalDistance * fleeArrivalDistance;

            if (reachedTarget || isOutside)
            {
                DespawnFleeing();
                return;
            }

            _pathRefreshTimer -= Time.fixedDeltaTime;
            if (_currentPath == null || _pathRefreshTimer <= 0f)
            {
                if (NavMeshPathfinder.TryGetPath(body.position, _fleeTarget, out Vector2[] newPath))
                {
                    _currentPath = newPath;
                    _currentWaypointIndex = 0;
                }
                else
                {
                    if (EnemyEscapeRoutingUtility.TryFindBestEscapePath(body.position, _arenaBounds, out Vector2 newExit, out Vector2[] altPath))
                    {
                        _fleeTarget = newExit;
                        _currentPath = altPath;
                        _currentWaypointIndex = 0;
                    }
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
                moveDir = toFleeTarget.normalized;
            }

            float fleeSpeed = config != null ? config.MoveSpeed * config.FleeSpeedMultiplier : 5f;
            if (TerrainZone.IsInForest(body.position))
            {
                fleeSpeed *= 0.90f;
            }
            float stolenGold = potDrain != null ? potDrain.StolenGold : 0f;
            float carryWeightFactor = 1f / (1f + (stolenGold * 0.05f));
            fleeSpeed *= carryWeightFactor;

            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                body.position,
                bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            Vector2 targetVelocity = moveDir * fleeSpeed;
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
            _isPatroling = false;
            _currentTarget = null;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;

            Vector2 currentPos = body != null ? body.position : (Vector2)transform.position;
            if (EnemyEscapeRoutingUtility.TryFindBestEscapePath(currentPos, _arenaBounds, out Vector2 bestExit, out Vector2[] path))
            {
                _fleeTarget = bestExit;
                _currentPath = path;
                _currentWaypointIndex = 0;
            }
            else
            {
                _fleeTarget = _arenaBounds != null
                    ? _arenaBounds.GetNearestEdgePoint(currentPos)
                    : currentPos + (Vector2)transform.up * 20f;
            }

            ApplyVisualState(isDraining: false);
        }

        private void DespawnFleeing()
        {
            RecordSuccessfulTheftEscape();
            ClearTheftLabel();
            ResetPresentation();
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

        private void RecordKill()
        {
            RunDirector runDirector = null;
            if (ProjectContext.Instance?.Services.TryGet(out RunDirector registeredDirector) == true)
            {
                runDirector = registeredDirector;
            }

            runDirector ??= FindFirstObjectByType<RunDirector>(FindObjectsInactive.Include);
            runDirector?.RecordKill(transform.position);
        }

        private void RecordSuccessfulTheftEscape()
        {
            if (_escapeHandled)
            {
                return;
            }

            _escapeHandled = true;
            if (potDrain == null || potDrain.StolenGold <= 0f)
            {
                return;
            }

            RunDirector runDirector = null;
            if (ProjectContext.Instance?.Services.TryGet(out RunDirector registeredDirector) == true)
            {
                runDirector = registeredDirector;
            }

            runDirector ??= FindFirstObjectByType<RunDirector>(FindObjectsInactive.Include);
            runDirector?.RecordSuccessfulTheftEscape();
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
            theftLabel ??= GetComponentInChildren<TextMeshPro>(true);
            if (theftLabel != null)
            {
                theftLabel.sortingOrder = 50;
                theftLabel.alignment = TextAlignmentOptions.Center;
            }
            animator ??= GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
            _hasRunToCarriedPotParameter = HasAnimatorParameter("RunToCarriedPot");
            _hasCarryLootParameter = HasAnimatorParameter("CarryLoot");
            _hasPickParameter = HasAnimatorParameter("Pick");
            _hasDieParameter = HasAnimatorParameter("Die");
            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            healthFeedbackPresenter ??= GetComponent<HealthStageFeedbackPresenter>() ?? GetComponentInChildren<HealthStageFeedbackPresenter>();
            if (healthFeedbackPresenter == null && Application.isPlaying)
            {
                healthFeedbackPresenter = gameObject.AddComponent<HealthStageFeedbackPresenter>();
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
            if (TerrainZone.IsInForest(body.position))
            {
                moveSpeed *= 0.90f;
            }
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
                : (_currentState == TravellerState.Fleeing
                    ? new Color(1f, 0.28f, 0.28f, 1f) // Red/thermal fleeing cue
                    : (_hasBaseColor ? _baseColor : roamingColor));

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
            RecordKill();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            ClearTheftLabel();
            ResetPresentation();
            ApplyVisualState(isDraining: false);
            if (animator != null && animator.runtimeAnimatorController != null && _hasDieParameter)
            {
                animator.SetTrigger("Die");
            }

            int stolenGold = potDrain != null ? Mathf.FloorToInt(potDrain.StolenGold) : 0;
            int pocketChange = Mathf.Max(0, _goldDropAmount);
            int totalDrop = pocketChange + stolenGold;
            if (totalDrop > 0)
            {
                SpawnGoldPickup(totalDrop);
            }

            // Mobs remain on the map for 3 seconds following death
            RainbowsEnd.Gameplay.Combat.CombatFeedbackController.DelayedDespawn(this, () =>
            {
                if (_owningPool != null)
                {
                    _owningPool.Return(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }, 3.0f);
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
        private bool ShouldRunToCarriedPot()
        {
            return _currentState == TravellerState.Seeking
                && _currentTarget != null
                && _currentTarget.IsCarried;
        }

        private bool ShouldCarryLoot()
        {
            return _currentState == TravellerState.Fleeing
                && potDrain != null
                && potDrain.StolenGold > 0f;
        }

        private bool ShouldPick()
        {
            return _currentState == TravellerState.Draining;
        }

        private bool HasAnimatorParameter(string parameterName)
        {
            if (animator == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshPresentation()
        {
            bool runToCarriedPot = ShouldRunToCarriedPot();
            bool carryLoot = ShouldCarryLoot();
            bool pick = ShouldPick();

            if (animator != null && animator.runtimeAnimatorController != null && Application.isPlaying)
            {
                if (_hasRunToCarriedPotParameter)
                {
                    animator.SetBool("RunToCarriedPot", runToCarriedPot);
                }

                if (_hasCarryLootParameter)
                {
                    animator.SetBool("CarryLoot", carryLoot);
                }
                if (_hasPickParameter)
                {
                    animator.SetBool("Pick", pick);
                }
            }

            if (carriedBag != null && carriedBag.activeSelf != carryLoot)
            {
                carriedBag.SetActive(carryLoot);
            }
        }

        private void ResetPresentation()
        {
            if (animator != null && animator.runtimeAnimatorController != null && Application.isPlaying)
            {
                if (_hasRunToCarriedPotParameter)
                {
                    animator.SetBool("RunToCarriedPot", false);
                }

                if (_hasCarryLootParameter)
                {
                    animator.SetBool("CarryLoot", false);
                }
                if (_hasPickParameter)
                {
                    animator.SetBool("Pick", false);
                }
                if (_hasDieParameter)
                {
                    animator.ResetTrigger("Die");
                }

                animator.applyRootMotion = false;
                if (animator.transform != transform)
                {
                    animator.transform.localPosition = Vector3.zero;
                }
            }

            if (carriedBag != null && carriedBag.activeSelf)
            {
                carriedBag.SetActive(false);
            }
        }

    }
}