using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class RangedAttackController : MonoBehaviour
    {
        [SerializeField] private ProjectileWeaponLauncher projectileLauncher;
        [SerializeField] private WeaponImpactFeedbackSpawner impactFeedbackSpawner;
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] private Collider2D ignoredCollider;
        [SerializeField] private FactionId sourceFaction = FactionId.Police;
        [SerializeField] [Min(0f)] private float fallbackMuzzleOffset = 0.55f;

        private readonly WeaponRuntimeState _runtimeState = new WeaponRuntimeState();

        public WeaponConfig WeaponConfig => weaponConfig;

        public float CurrentSpreadOffsetDegrees { get; private set; }

        public void Configure(
            ProjectileWeaponLauncher weaponLauncher,
            WeaponImpactFeedbackSpawner feedbackSpawner,
            WeaponConfig config,
            FactionId factionId,
            Collider2D colliderToIgnore)
        {
            projectileLauncher = weaponLauncher;
            impactFeedbackSpawner = feedbackSpawner;
            weaponConfig = config;
            sourceFaction = factionId;
            ignoredCollider = colliderToIgnore;
            _runtimeState.Configure(weaponConfig);
        }

        private void Awake()
        {
            CacheReferences();
            _runtimeState.Configure(weaponConfig);
        }

        public void Tick(float deltaTime)
        {
            _runtimeState.Tick(deltaTime);
        }

        public bool TryFire(Vector2 position, Vector2 baseDirection, float movementSpreadNormalized, Object instigator)
        {
            if (weaponConfig == null || projectileLauncher == null || !_runtimeState.TryConsumeShot())
            {
                return false;
            }

            Vector2 normalizedAimDirection = baseDirection.sqrMagnitude > 0f ? baseDirection.normalized : Vector2.up;
            CurrentSpreadOffsetDegrees = WeaponSpreadUtility.CalculateSpreadOffsetDegrees(
                Mathf.Clamp01(movementSpreadNormalized),
                weaponConfig.MinSpreadDegrees,
                weaponConfig.MaxSpreadDegrees,
                Random.value);

            Vector2 fireDirection = WeaponSpreadUtility.ApplySpread(normalizedAimDirection, CurrentSpreadOffsetDegrees);
            Vector2 origin = position + fireDirection * ResolveMuzzleOffset();
            return projectileLauncher.LaunchVolley(
                origin,
                fireDirection,
                weaponConfig,
                sourceFaction,
                ignoredCollider,
                instigator,
                impactFeedbackSpawner) > 0;
        }

        private void CacheReferences()
        {
            projectileLauncher ??= GetComponent<ProjectileWeaponLauncher>();
            impactFeedbackSpawner ??= GetComponent<WeaponImpactFeedbackSpawner>();

            if (ignoredCollider == null)
            {
                ignoredCollider = GetComponent<Collider2D>();
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