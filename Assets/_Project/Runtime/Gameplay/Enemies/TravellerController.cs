using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Systems.Economy;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
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
        private EconomyState _economyState;
        private PotController _currentTarget;
        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private float _targetRefreshTimerSeconds;
        private bool _deathHandled;

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

        public void Activate(EnemySpawner owningSpawner, PrefabPool owningPool, PrefabPool goldPickupPool, EnemyConfig enemyConfig)
        {
            _owningSpawner = owningSpawner;
            _owningPool = owningPool;
            _goldPickupPool = goldPickupPool;
            config = enemyConfig;
            _economyState = null;
            _currentTarget = null;
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;

            CacheReferences();
            ConfigureBody();
            potDrain?.Configure(this, bodyCollider);
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
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;
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
            _economyState = null;
            _currentTarget = null;
            _targetRefreshTimerSeconds = 0f;
            _deathHandled = false;
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

            bool isDraining = potDrain != null && potDrain.Tick(Time.deltaTime, config != null ? config.ContactDamage : 0f);
            if (isDraining)
            {
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

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearDamping = 0f;

            if (bodyCollider != null)
            {
                bodyCollider.isTrigger = true;
            }
        }

        private void RefreshTargetIfNeeded()
        {
            _targetRefreshTimerSeconds -= Time.deltaTime;
            if (_currentTarget != null && _currentTarget.HarvestStatus != PotHarvestStatus.Destroyed && _targetRefreshTimerSeconds > 0f)
            {
                return;
            }

            _candidatePots.Clear();
            _candidateTargets.Clear();

            PotController[] pots = FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < pots.Length; index++)
            {
                PotController pot = pots[index];
                if (pot == null || pot.HarvestStatus == PotHarvestStatus.Destroyed)
                {
                    continue;
                }

                _candidatePots.Add(pot);
                _candidateTargets.Add(new TravellerTargetCandidate(pot.transform.position, pot.HarvestStatus == PotHarvestStatus.Harvesting));
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

            if (_currentTarget == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toTarget = (Vector2)_currentTarget.transform.position - body.position;
            float stopDistance = ResolveStopDistance();
            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            float moveSpeed = config != null ? Mathf.Max(0f, config.MoveSpeed) : 0f;
            body.linearVelocity = toTarget.normalized * moveSpeed;
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

            SpawnGoldPickup();
            if (_owningPool != null)
            {
                _owningPool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SpawnGoldPickup()
        {
            ResolveEconomyState();
            EconomyConfig economyConfig = _economyState != null ? _economyState.Config : null;
            if (_goldPickupPool == null || economyConfig == null)
            {
                return;
            }

            int minGold = Mathf.Max(0, economyConfig.EnemyDropGoldMin);
            int maxGold = Mathf.Max(minGold, economyConfig.EnemyDropGoldMax);
            int goldAmount = Random.Range(minGold, maxGold + 1);
            if (goldAmount <= 0)
            {
                return;
            }

            GoldPickup goldPickup = _goldPickupPool.Rent<GoldPickup>(transform.position, Quaternion.identity);
            goldPickup.Activate(_goldPickupPool, goldAmount);
        }

        private void ResolveEconomyState()
        {
            if (_economyState != null)
            {
                return;
            }

            if (ProjectContext.Instance?.Services.TryGet(out EconomyState economyState) == true)
            {
                _economyState = economyState;
                return;
            }

            _economyState = FindFirstObjectByType<EconomyState>(FindObjectsInactive.Include);
        }
    }
}