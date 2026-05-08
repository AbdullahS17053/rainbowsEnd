using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    [RequireComponent(typeof(PlayerRoot))]
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private ProjectileWeaponLauncher projectileLauncher;
        [SerializeField] private WeaponImpactFeedbackSpawner impactFeedbackSpawner;
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] [Min(0f)] private float fallbackMuzzleOffset = 0.55f;

        private readonly WeaponRuntimeState _runtimeState = new WeaponRuntimeState();

        public WeaponConfig WeaponConfig => weaponConfig;

        public float CurrentSpreadOffsetDegrees { get; private set; }

        public void Configure(
            PlayerRoot root,
            PlayerInputReader reader,
            PlayerAimController playerAimController,
            PlayerMovementController playerMovementController,
            ProjectileWeaponLauncher weaponLauncher,
            WeaponImpactFeedbackSpawner feedbackSpawner,
            WeaponConfig config)
        {
            playerRoot = root;
            inputReader = reader;
            aimController = playerAimController;
            movementController = playerMovementController;
            projectileLauncher = weaponLauncher;
            impactFeedbackSpawner = feedbackSpawner;
            weaponConfig = config;
            _runtimeState.Configure(weaponConfig);
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
            _runtimeState.Configure(weaponConfig);
        }

        private void Awake()
        {
            CacheReferences();
            ResolveDefaults();
            _runtimeState.Configure(weaponConfig);
        }

        private void Update()
        {
            _runtimeState.Tick(Time.deltaTime);

            if (inputReader == null || !inputReader.FireHeld || weaponConfig == null || projectileLauncher == null)
            {
                return;
            }

            if (!_runtimeState.TryConsumeShot())
            {
                return;
            }

            FireShot();
        }

        private void FireShot()
        {
            Vector2 baseAimDirection = aimController != null ? aimController.CurrentAimDirection : Vector2.up;
            if (baseAimDirection.sqrMagnitude <= 0f)
            {
                baseAimDirection = Vector2.up;
            }

            CurrentSpreadOffsetDegrees = WeaponSpreadUtility.CalculateSpreadOffsetDegrees(
                movementController != null ? movementController.SpeedNormalized : 0f,
                weaponConfig.MinSpreadDegrees,
                weaponConfig.MaxSpreadDegrees,
                Random.value);

            Vector2 fireDirection = WeaponSpreadUtility.ApplySpread(baseAimDirection, CurrentSpreadOffsetDegrees);
            Vector2 origin = (Vector2)transform.position + fireDirection * ResolveMuzzleOffset();
            projectileLauncher.Launch(
                origin,
                fireDirection,
                weaponConfig,
                playerRoot != null ? playerRoot.FactionId : FactionId.Player,
                playerRoot != null ? playerRoot.BodyCollider : null,
                this,
                impactFeedbackSpawner);

            Debug.DrawRay(origin, fireDirection * 0.75f, Color.white, 0.15f);
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            aimController ??= GetComponent<PlayerAimController>();
            movementController ??= GetComponent<PlayerMovementController>();
            projectileLauncher ??= GetComponent<ProjectileWeaponLauncher>();
            impactFeedbackSpawner ??= GetComponent<WeaponImpactFeedbackSpawner>();
        }

        private void ResolveDefaults()
        {
            if (weaponConfig == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                weaponConfig = ProjectContext.Instance.ConfigRegistry.DefaultWeaponConfig;
            }
        }

        private float ResolveMuzzleOffset()
        {
            if (weaponConfig != null && weaponConfig.MuzzleOffset > 0f)
            {
                return weaponConfig.MuzzleOffset;
            }

            return fallbackMuzzleOffset;
        }
    }
}