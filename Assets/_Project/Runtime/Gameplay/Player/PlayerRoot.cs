using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
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
        [SerializeField] private WeaponController weaponController;

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

        public WeaponController WeaponController => weaponController;

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

            if (inputReader != null && inputActionsAsset != null)
            {
                inputReader.SetInputActions(inputActionsAsset);
            }
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

            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            inputReader ??= GetComponent<PlayerInputReader>();
            movementController ??= GetComponent<PlayerMovementController>();
            aimController ??= GetComponent<PlayerAimController>();
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
    }
}