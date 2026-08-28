using DG.Tweening;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.World;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RainbowsEnd.Gameplay.Player
{
    public sealed class PlayerRoot : MonoBehaviour, ITeamAffiliation
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private InputActionAsset inputActionsAsset;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Health health;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private PlayerPotCarryController potCarryController;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Animator feedbackAnimator;
        [SerializeField] private GameObject stunnedStars;
        private bool _isRecovering;
        private bool _isDowned;
        private float _downedTimerSeconds;
        private bool _isDazed;
        private float _dazedTimerSeconds;


        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int StunTriggerHash = Animator.StringToHash("Stun");
        private static readonly int StandUpTriggerHash = Animator.StringToHash("StandUp");

        private sealed class RendererColorTarget
        {
            public Renderer Renderer { get; }
            public int ColorPropertyId { get; }
            public Color OriginalColor { get; }

            public RendererColorTarget(Renderer renderer, int colorPropertyId, Color originalColor)
            {
                Renderer = renderer;
                ColorPropertyId = colorPropertyId;
                OriginalColor = originalColor;
            }
        }

        private readonly System.Collections.Generic.List<RendererColorTarget> _rendererColorTargets =
            new System.Collections.Generic.List<RendererColorTarget>();

        private static readonly int SurfacePropertyId = Shader.PropertyToID("_Surface");
        private static readonly int BlendPropertyId = Shader.PropertyToID("_Blend");
        private static readonly int SourceBlendPropertyId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DestinationBlendPropertyId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWritePropertyId = Shader.PropertyToID("_ZWrite");
        private MaterialPropertyBlock _visualPropertyBlock;
        private ConcealmentZone[] _concealmentZones = new ConcealmentZone[0];
        private bool _presentationCached;
        private bool _isConcealed;
        private float _concealmentRevealTimerSeconds;
        private float _currentVisualOpacity = 1f;
        private Tween _knockbackTween;

        public FactionId FactionId => FactionId.Player;

        public PlayerConfig Config => config;

        public InputActionAsset InputActionsAsset => inputActionsAsset;

        public Transform VisualRoot => visualRoot != null ? visualRoot : transform;

        public SpriteRenderer VisualRenderer => visualRenderer;

        public Rigidbody2D Body => body;

        public Collider2D BodyCollider => bodyCollider;

        public Health Health => health;

        public PlayerInputReader InputReader => inputReader;

        public PlayerMovementController MovementController => movementController;

        public PlayerAimController AimController => aimController;

        public PlayerPotCarryController PotCarryController => potCarryController;

        public PlayerWeaponInventory WeaponInventory => weaponInventory;

        public WeaponController WeaponController => weaponController;

        public bool IsDowned => _isDowned;

        public bool IsRecovering => _isRecovering;

        public bool IsDazed => _isDazed;

        public bool IsDisabled => _isDowned || _isDazed;

        public bool CanFire => !_isDowned && !_isRecovering && !_isDazed && !IsCarryingPot;

        public bool CanUsePortals => !_isDowned && !_isRecovering && !_isDazed;

        public bool CanInteractWithPot => !_isDowned && !_isDazed;



        public bool IsConcealed => _isConcealed;

        public float CurrentVisualOpacity => _currentVisualOpacity;
        public bool CanOpenUpgradeMenu => !_isDowned && !_isDazed;

        public bool IsCarryingPot => potCarryController != null && potCarryController.IsCarryingPot;

        public void Configure(
            PlayerConfig playerConfig,
            InputActionAsset actionsAsset,
            Transform playerVisualRoot,
            SpriteRenderer playerVisualRenderer,
            Rigidbody2D playerBody,
            Collider2D playerCollider,
            Health playerHealth,
            PlayerInputReader playerInputReader,
            PlayerMovementController playerMovementController,
            PlayerAimController playerAimController)
        {
            config = playerConfig;
            inputActionsAsset = actionsAsset;
            visualRoot = playerVisualRoot;
            visualRenderer = playerVisualRenderer;
            body = playerBody;
            bodyCollider = playerCollider;
            health = playerHealth;
            inputReader = playerInputReader;
            movementController = playerMovementController;
            aimController = playerAimController;
            potCarryController ??= GetComponent<PlayerPotCarryController>();

            if (inputReader != null && inputActionsAsset != null)
            {
                inputReader.SetInputActions(inputActionsAsset);
            }

            potCarryController?.Configure(this, inputReader, aimController);
        }

        public void ConfigureCombat(WeaponController playerWeaponController)
        {
            weaponController = playerWeaponController;
        }

        public void NotifyShotFired()
        {
            _concealmentRevealTimerSeconds = config != null
                ? Mathf.Max(0f, config.ConcealmentRevealDurationSeconds)
                : 0f;
            RefreshVisualState();
        }

        public void RefreshVisualState()
        {
            if (!_presentationCached)
            {
                CachePresentation();
            }

            bool isConcealed = ResolveIsInConcealmentZone();
            float opacity = ResolveVisualOpacity(isConcealed);
            if (_isConcealed == isConcealed && Mathf.Approximately(_currentVisualOpacity, opacity))
            {
                return;
            }

            _isConcealed = isConcealed;
            _currentVisualOpacity = opacity;
            ApplyVisualOpacity(opacity);
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
            ResolveDefaults();

            if (inputReader != null && inputActionsAsset != null)
            {
                inputReader.SetInputActions(inputActionsAsset);
            }

            potCarryController?.Configure(this, inputReader, aimController);

            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }

            ApplyPlayerEnabledState(true);
            CachePresentation();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Depleted -= HandleHealthDepleted;
                health.Depleted += HandleHealthDepleted;
            }

            ResetAnimationFeedback();
            CachePresentation();
            RefreshVisualState();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Depleted -= HandleHealthDepleted;
            }

            if (_isDowned || _isRecovering)
            {
                health?.SetInvulnerable(false);
                ApplyPlayerEnabledState(true);
                _isDowned = false;
                _isRecovering = false;
                _downedTimerSeconds = 0f;
            }

            if (_isDazed)
            {
                _knockbackTween?.Kill();
                _knockbackTween = null;
                ApplyDazeEnabledState(true);
                _isDazed = false;
                _dazedTimerSeconds = 0f;
            }

            ResetAnimationFeedback();
            ClearVisualOverrides();
            _concealmentRevealTimerSeconds = 0f;
            _isConcealed = false;
            _currentVisualOpacity = 1f;
            _presentationCached = false;
        }

        private void Update()
        {
            TickConcealmentReveal();

            if (_isDazed)
            {
                _dazedTimerSeconds -= Time.deltaTime;
                if (_dazedTimerSeconds <= 0f)
                {
                    RecoverFromDazedState();
                }

                RefreshVisualState();
                return;
            }

            if (_isDowned || _isRecovering)
            {
                _downedTimerSeconds -= Time.deltaTime;
                if (_downedTimerSeconds <= 0f)
                {
                    if (_isDowned)
                    {
                        RecoverFromDownedState();
                    }
                    else
                    {
                        RecoverFromRecoveryState();
                    }
                }
            }

            RefreshVisualState();
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            inputReader ??= GetComponent<PlayerInputReader>();
            movementController ??= GetComponent<PlayerMovementController>();
            aimController ??= GetComponent<PlayerAimController>();
            potCarryController ??= GetComponent<PlayerPotCarryController>();
            weaponInventory ??= GetComponent<PlayerWeaponInventory>();
            weaponController ??= GetComponent<WeaponController>();

            if (visualRoot == null)
            {
                Transform child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }

            if (visualRenderer == null && visualRoot != null)
            {
                visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
            }

            feedbackAnimator ??= VisualRoot.GetComponentInChildren<Animator>(true);
        }

        private void CachePresentation()
        {
            _rendererColorTargets.Clear();
            _visualPropertyBlock ??= new MaterialPropertyBlock();

            Transform root = VisualRoot;
            Renderer[] renderers = root != null
                ? root.GetComponentsInChildren<Renderer>(false)
                : new Renderer[0];

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                Material sharedMaterial = renderer != null ? renderer.sharedMaterial : null;
                if (sharedMaterial == null)
                {
                    continue;
                }

                Material material = Application.isPlaying ? renderer.material : sharedMaterial;
                int colorPropertyId = material.HasProperty(BaseColorPropertyId)
                    ? BaseColorPropertyId
                    : material.HasProperty(ColorPropertyId)
                        ? ColorPropertyId
                        : 0;
                if (colorPropertyId == 0)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    ConfigureTransparentMaterial(material);
                }

                _rendererColorTargets.Add(new RendererColorTarget(
                    renderer,
                    colorPropertyId,
                    material.GetColor(colorPropertyId)));
            }

            _concealmentZones = FindObjectsByType<ConcealmentZone>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            _presentationCached = true;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material == null || !material.HasProperty(SurfacePropertyId))
            {
                return;
            }

            material.SetFloat(SurfacePropertyId, 1f);
            material.SetFloat(BlendPropertyId, 0f);
            material.SetFloat(SourceBlendPropertyId, 5f);
            material.SetFloat(DestinationBlendPropertyId, 10f);
            material.SetFloat(ZWritePropertyId, 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }


        private bool ResolveIsInConcealmentZone()
        {
            Vector2 position = body != null ? body.position : (Vector2)transform.position;
            for (int index = 0; index < _concealmentZones.Length; index++)
            {
                ConcealmentZone zone = _concealmentZones[index];
                if (zone != null && zone.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        private float ResolveVisualOpacity(bool isConcealed)
        {
            if (IsCarryingPot || _concealmentRevealTimerSeconds > 0f)
            {
                return 1f;
            }

            if (_isRecovering)
            {
                return config != null ? Mathf.Clamp01(config.RecoveryVeilOpacity) : 1f;
            }

            if (isConcealed)
            {
                return config != null ? Mathf.Clamp01(config.ConcealedOpacity) : 1f;
            }

            return 1f;
        }

        private void TickConcealmentReveal()
        {
            if (_concealmentRevealTimerSeconds <= 0f)
            {
                return;
            }

            _concealmentRevealTimerSeconds = Mathf.Max(
                0f,
                _concealmentRevealTimerSeconds - Time.deltaTime);
        }

        private void ApplyVisualOpacity(float opacity)
        {
            for (int index = 0; index < _rendererColorTargets.Count; index++)
            {
                RendererColorTarget target = _rendererColorTargets[index];
                if (target.Renderer == null)
                {
                    continue;
                }

                Color color = target.OriginalColor;
                color.a *= opacity;
                _visualPropertyBlock.Clear();
                target.Renderer.GetPropertyBlock(_visualPropertyBlock);
                _visualPropertyBlock.SetColor(target.ColorPropertyId, color);
                target.Renderer.SetPropertyBlock(_visualPropertyBlock);
            }
        }

        private void ClearVisualOverrides()
        {
            for (int index = 0; index < _rendererColorTargets.Count; index++)
            {
                Renderer renderer = _rendererColorTargets[index].Renderer;
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }

        private bool HasAnimationParameter(int parameterHash)
        {
            if (feedbackAnimator == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in feedbackAnimator.parameters)
            {
                if (parameter.nameHash == parameterHash)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayStunFeedback()
        {
            if (stunnedStars != null)
            {
                stunnedStars.SetActive(true);
            }

            if (HasAnimationParameter(StunTriggerHash))
            {
                feedbackAnimator.ResetTrigger(StandUpTriggerHash);
                feedbackAnimator.SetTrigger(StunTriggerHash);
            }
        }

        private void PlayRecoveryFeedback()
        {
            if (stunnedStars != null)
            {
                stunnedStars.SetActive(false);
            }

            if (HasAnimationParameter(StandUpTriggerHash))
            {
                feedbackAnimator.SetTrigger(StandUpTriggerHash);
            }
        }

        private void ResetAnimationFeedback()
        {
            if (stunnedStars != null)
            {
                stunnedStars.SetActive(false);
            }

            if (feedbackAnimator != null)
            {
                feedbackAnimator.ResetTrigger(StunTriggerHash);
                feedbackAnimator.ResetTrigger(StandUpTriggerHash);
            }
        }




        private void ResolveDefaults()
        {
            if (config == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                config = ProjectContext.Instance.ConfigRegistry.PlayerConfig;
            }
        }

        private void HandleHealthDepleted()
        {
            if (_isDowned || _isRecovering)
            {
                return;
            }

            _isDowned = true;
            _downedTimerSeconds = config != null ? Mathf.Max(0f, config.StunDurationSeconds) : 0f;
            PlayStunFeedback();
            ApplyPlayerEnabledState(false);

            if (_downedTimerSeconds <= 0f)
            {
                RecoverFromDownedState();
            }
        }

        private void RecoverFromDownedState()
        {
            if (!_isDowned)
            {
                return;
            }

            health?.ResetToFull();
            health?.SetInvulnerable(true);
            _isDowned = false;
            _isRecovering = true;
            _downedTimerSeconds = config != null ? Mathf.Max(0f, config.RecoveryVeilDurationSeconds) : 0f;
            PlayRecoveryFeedback();
            ApplyPlayerEnabledState(true);

            if (_downedTimerSeconds <= 0f)
            {
                RecoverFromRecoveryState();
            }
        }

        private void RecoverFromRecoveryState()
        {
            if (!_isRecovering)
            {
                return;
            }

            health?.SetInvulnerable(false);
            _isRecovering = false;
            _downedTimerSeconds = 0f;
            ResetAnimationFeedback();
            ApplyPlayerEnabledState(true);
        }
        private void RecoverFromDazedState()
        {
            if (!_isDazed)
            {
                return;
            }

            _knockbackTween?.Kill();
            _knockbackTween = null;
            _isDazed = false;
            _dazedTimerSeconds = 0f;
            if (stunnedStars != null)
            {
                stunnedStars.SetActive(false);
            }

            ApplyDazeEnabledState(true);
        }

        public void ApplyKnockbackAndDaze(Vector2 knockbackDirection, float knockbackDistance, float dazeDurationSeconds)
        {
            if (_isDowned || _isRecovering || _isDazed)
            {
                return;
            }

            if (dazeDurationSeconds <= 0f)
            {
                return;
            }

            Vector2 direction = knockbackDirection.sqrMagnitude > 0.0001f
                ? knockbackDirection.normalized
                : Vector2.up;

            _isDazed = true;
            _dazedTimerSeconds = dazeDurationSeconds;
            if (stunnedStars != null)
            {
                stunnedStars.SetActive(true);
            }

            ApplyDazeEnabledState(false);

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;

                if (knockbackDistance > 0f)
                {
                    Vector2 targetPosition = body.position + direction * knockbackDistance;
                    ArenaBoundsAuthoring arenaBounds = ResolveArenaBounds();
                    if (arenaBounds != null)
                    {
                        targetPosition = arenaBounds.Clamp(targetPosition);
                    }

                    _knockbackTween?.Kill();
                    _knockbackTween = transform.DOMove(targetPosition, 0.15f)
                        .SetEase(Ease.OutQuad);
                }
            }
        }

        private void ApplyDazeEnabledState(bool isEnabled)
        {
            if (movementController != null)
            {
                movementController.enabled = isEnabled;
            }

            if (aimController != null)
            {
                aimController.enabled = isEnabled;
            }

            if (potCarryController != null)
            {
                potCarryController.enabled = isEnabled;
            }

            if (weaponController != null)
            {
                weaponController.enabled = isEnabled;
            }
        }

        private ArenaBoundsAuthoring ResolveArenaBounds()
        {
            ArenaBoundsAuthoring bounds = null;
            if (ProjectContext.Instance?.Services.TryGet(out Core.SceneManagement.ArenaSceneContext arenaCtx) == true)
            {
                bounds = arenaCtx.ArenaBounds;
            }

            if (bounds == null)
            {
                bounds = FindFirstObjectByType<ArenaBoundsAuthoring>(FindObjectsInactive.Exclude);
            }

            return bounds;
        }

        private void ApplyPlayerEnabledState(bool isEnabled)
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = isEnabled;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = isEnabled;
            }

            if (movementController != null)
            {
                movementController.enabled = isEnabled;
            }

            if (aimController != null)
            {
                aimController.enabled = isEnabled;
            }

            if (potCarryController != null)
            {
                potCarryController.enabled = isEnabled;
            }

            if (weaponController != null)
            {
                weaponController.enabled = isEnabled;
            }
        }
    }
}
