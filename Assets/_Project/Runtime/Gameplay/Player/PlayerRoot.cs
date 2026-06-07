using DG.Tweening;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.World;
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

        private bool _isDowned;
        private float _downedTimerSeconds;
        private bool _isDazed;
        private float _dazedTimerSeconds;
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

        public bool IsDazed => _isDazed;

        public bool IsDisabled => _isDowned || _isDazed;

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
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Depleted -= HandleHealthDepleted;
                health.Depleted += HandleHealthDepleted;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Depleted -= HandleHealthDepleted;
            }

            if (_isDowned)
            {
                ApplyPlayerEnabledState(true);
                _isDowned = false;
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
        }

        private void Update()
        {
            if (_isDazed)
            {
                _dazedTimerSeconds -= Time.deltaTime;
                if (_dazedTimerSeconds <= 0f)
                {
                    RecoverFromDazedState();
                }

                return;
            }

            if (!_isDowned)
            {
                return;
            }

            _downedTimerSeconds -= Time.deltaTime;
            if (_downedTimerSeconds > 0f)
            {
                return;
            }

            RecoverFromDownedState();
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
            if (_isDowned)
            {
                return;
            }

            _isDowned = true;
            _downedTimerSeconds = config != null ? Mathf.Max(0f, config.DownedDurationSeconds) : 0f;
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
            _isDowned = false;
            _downedTimerSeconds = 0f;
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
            ApplyDazeEnabledState(true);
        }

        public void ApplyKnockbackAndDaze(Vector2 knockbackDirection, float knockbackDistance, float dazeDurationSeconds)
        {
            if (_isDowned || _isDazed)
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