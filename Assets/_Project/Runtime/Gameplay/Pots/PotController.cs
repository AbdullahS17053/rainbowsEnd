using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Unicorn;
using RainbowsEnd.Systems.Economy;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Pots
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PotController : MonoBehaviour, ITeamAffiliation
    {
        [SerializeField] private PotConfig config;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Health health;
        [SerializeField] private PotTrailCaptureSensor trailCaptureSensor;
        [SerializeField] private PotDrainFeedbackPresenter drainFeedbackPresenter;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Color harvestingColor = new Color(1f, 0.88f, 0.32f, 1f);
        [SerializeField] private Color interruptedColor = new Color(0.93f, 0.55f, 0.24f, 1f);
        [SerializeField] private Color destroyedColor = new Color(0.24f, 0.24f, 0.24f, 1f);

        private readonly PotHarvestState _harvestState = new PotHarvestState();
        private readonly HashSet<int> _activeDrainerIds = new HashSet<int>();
        private EconomyState _economyState;
        private PotRegistry _potRegistry;
        private Color _idleColor = Color.white;
        private bool _hasIdleColor;
        private bool _isRegisteredWithPotRegistry;

        public FactionId FactionId => FactionId.Player;

        public PotConfig Config => config;

        public PotHarvestStatus HarvestStatus => _harvestState.Status;

        public bool IsDestroyed => _harvestState.Status == PotHarvestStatus.Destroyed || (health != null && health.IsDepleted);

        public bool IsBeingDrained => _activeDrainerIds.Count > 0;

        public void Configure(
            PotConfig potConfig,
            Rigidbody2D rigidbody2D,
            Collider2D collider2D,
            Health potHealth,
            PotTrailCaptureSensor captureSensor,
            SpriteRenderer renderer)
        {
            config = potConfig;
            body = rigidbody2D;
            bodyCollider = collider2D;
            health = potHealth;
            trailCaptureSensor = captureSensor;
            visualRenderer = renderer;

            CacheReferences();
            ConfigureBody();
            ConfigureCaptureSensor();
            InitializeHealth();
            CacheIdleColor(forceRefresh: true);
            ApplyVisualState();
        }

        private void Reset()
        {
            CacheReferences();
            ConfigureBody();
            ConfigureCaptureSensor();
            CacheIdleColor(forceRefresh: true);
        }

        private void OnValidate()
        {
            CacheReferences();
            ConfigureBody();
            ConfigureCaptureSensor();
            CacheIdleColor(forceRefresh: true);
            ApplyVisualState();
        }

        private void Awake()
        {
            CacheReferences();
            ResolveDefaults();
            ConfigureBody();
            ConfigureCaptureSensor();
            InitializeHealth();
            TryRegisterWithPotRegistry();
            CacheIdleColor(forceRefresh: true);
            ApplyVisualState();
        }

        private void OnEnable()
        {
            TryRegisterWithPotRegistry();
            ApplyVisualState();
        }

        private void Update()
        {
            if (!_isRegisteredWithPotRegistry)
            {
                TryRegisterWithPotRegistry();
            }

            if (_harvestState.Status == PotHarvestStatus.Destroyed)
            {
                drainFeedbackPresenter?.SetVisible(false);
                return;
            }

            if (health != null && health.IsDepleted)
            {
                _harvestState.MarkDestroyed();
                ResolveEconomyState();
                _economyState?.SetHarvesterActive(this, false);
                if (body != null)
                {
                    body.simulated = false;
                }

                ApplyVisualState();
                return;
            }

            ResolveEconomyState();

            if (trailCaptureSensor != null && trailCaptureSensor.TryGetOldestOverlappingTrailTimestamp(out float trailTimestampSeconds))
            {
                float capturedTrailAgeSeconds = Mathf.Max(0f, Time.time - trailTimestampSeconds);
                float baseHarvestGoldPerSecond = PotHarvestUtility.CalculateBaseHarvestGoldPerSecond(
                    capturedTrailAgeSeconds,
                    config != null ? config.BaseHarvestGoldPerSecond : 1f,
                    config != null ? config.TrailAgeGoldMultiplier : 0f);
                _harvestState.CaptureTrail(trailTimestampSeconds, baseHarvestGoldPerSecond);
            }
            else
            {
                _harvestState.ReleaseTrail();
            }

            bool isMoving = body != null && body.linearVelocity.magnitude > ResolveMovementCancelSpeedThreshold();
            _harvestState.UpdateState(Time.deltaTime, isMoving, config != null ? config.HarvestCancelDelaySeconds : 0f);

            if (_economyState != null)
            {
                _economyState.SetHarvesterActive(this, _harvestState.Status == PotHarvestStatus.Harvesting);
                float generatedGold = _harvestState.GenerateGold(Time.deltaTime, _economyState.CurrentHarvestMultiplier);
                if (generatedGold > 0f)
                {
                    _economyState.AddGold(generatedGold);
                }
            }

            ApplyVisualState();
        }

        private void OnDisable()
        {
            _economyState?.SetHarvesterActive(this, false);
            _activeDrainerIds.Clear();
            drainFeedbackPresenter?.SetVisible(false);
            UnregisterFromPotRegistry();
        }

        private void OnDestroy()
        {
            UnregisterFromPotRegistry();
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            trailCaptureSensor ??= GetComponentInChildren<PotTrailCaptureSensor>(true);
            drainFeedbackPresenter ??= GetComponentInChildren<PotDrainFeedbackPresenter>(true);

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void ResolveDefaults()
        {
            if (config == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                config = ProjectContext.Instance.ConfigRegistry.PotConfig;
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
            body.linearDamping = config != null ? Mathf.Max(0f, config.PushResistance) : 1f;
        }

        private void ConfigureCaptureSensor()
        {
            trailCaptureSensor?.Configure(this, config != null ? config.CaptureRadius : 0.65f);
        }

        private void InitializeHealth()
        {
            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }
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

        public void SetDrainerActive(Object source, bool isActive)
        {
            if (source == null)
            {
                return;
            }

            if (isActive)
            {
                _activeDrainerIds.Add(source.GetInstanceID());
            }
            else
            {
                _activeDrainerIds.Remove(source.GetInstanceID());
            }

            ApplyVisualState();
        }

        public void DestroyByUnicorn(Object instigator)
        {
            if (IsDestroyed || health == null)
            {
                return;
            }

            float lethalDamage = Mathf.Max(health.CurrentHealth, config != null ? config.MaxHealth : 1f);
            health.ApplyDamage(new DamageInfo(lethalDamage, FactionId.Unicorn, transform.position, instigator));
        }

        private void TryRegisterWithPotRegistry()
        {
            if (_isRegisteredWithPotRegistry)
            {
                return;
            }

            ResolvePotRegistry();
            if (_potRegistry == null)
            {
                return;
            }

            _potRegistry.RegisterPot(this);
            _isRegisteredWithPotRegistry = true;
        }

        private void UnregisterFromPotRegistry()
        {
            if (!_isRegisteredWithPotRegistry)
            {
                return;
            }

            _potRegistry?.UnregisterPot(this);
            _isRegisteredWithPotRegistry = false;
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

        private float ResolveMovementCancelSpeedThreshold()
        {
            return config != null ? Mathf.Max(0f, config.MovementCancelSpeedThreshold) : 0.1f;
        }

        private void CacheIdleColor(bool forceRefresh = false)
        {
            if (visualRenderer != null && (forceRefresh || !_hasIdleColor))
            {
                _idleColor = visualRenderer.color;
                _hasIdleColor = true;
            }
        }

        private void ApplyVisualState()
        {
            if (visualRenderer == null)
            {
                return;
            }

            Color targetColor = _harvestState.Status switch
            {
                PotHarvestStatus.Harvesting => harvestingColor,
                PotHarvestStatus.Interrupted => interruptedColor,
                PotHarvestStatus.Destroyed => destroyedColor,
                _ => _idleColor
            };

            visualRenderer.color = targetColor;
            drainFeedbackPresenter?.SetVisible(!IsDestroyed && IsBeingDrained);
        }
    }
}