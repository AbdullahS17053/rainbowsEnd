using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace RainbowsEnd.Gameplay.Combat
{
    [RequireComponent(typeof(PlayerRoot))]
    public sealed class WeaponController : MonoBehaviour
    {
        private const float DefaultAutoFireConeDegrees = 45f;
        private const int AutoFireConeGizmoSegments = 16;

        private enum FireControlMode
        {
            ManualConeGated = 0,
            PureAutoFire = 1
        }

        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerUpgradeRuntime upgradeRuntime;
        [SerializeField] private ProjectileWeaponLauncher projectileLauncher;
        [SerializeField] private WeaponImpactFeedbackSpawner impactFeedbackSpawner;
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] private FireControlMode fireControlMode = FireControlMode.ManualConeGated;
        [FormerlySerializedAs("autoFireConeDegrees")]
        [SerializeField] [HideInInspector] [Min(0f)] private float legacyAutoFireConeDegrees = DefaultAutoFireConeDegrees;
        [FormerlySerializedAs("showAutoFireConeInEditor")]
        [SerializeField] private bool showAutoFireCone;
        [SerializeField] private Color autoFireConeColor = new Color(1f, 0.82f, 0.24f, 0.9f);
        [SerializeField] [Min(0f)] private float fallbackMuzzleOffset = 0.55f;

        private readonly System.Collections.Generic.List<Vector2> _targetCandidatePositions = new System.Collections.Generic.List<Vector2>();
        private readonly WeaponRuntimeState _runtimeState = new WeaponRuntimeState();

        private MeshRenderer _runtimeConeMeshRenderer;
        private MeshFilter _runtimeConeMeshFilter;
        private Mesh _runtimeConeMesh;
        private Material _runtimeConeMaterial;

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
            weaponInventory = root != null ? root.WeaponInventory : null;
            upgradeRuntime = root != null ? root.GetComponent<PlayerUpgradeRuntime>() : null;
            projectileLauncher = weaponLauncher;
            impactFeedbackSpawner = feedbackSpawner;
            weaponConfig = config;
            weaponInventory?.ConfigureDefaultWeapon(weaponConfig);
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
            weaponInventory?.ConfigureDefaultWeapon(weaponConfig);
            _runtimeState.Configure(weaponConfig);
        }

        private void OnDisable()
        {
            SetRuntimeConeVisible(false);
        }

        private void OnDestroy()
        {
            if (_runtimeConeMesh != null)
            {
                Destroy(_runtimeConeMesh);
                _runtimeConeMesh = null;
            }
        }

        private void Update()
        {
            _runtimeState.Tick(Time.deltaTime);
            SyncActiveWeaponConfig();

            Vector2 baseAimDirection = ResolveBaseAimDirection();
            UpdateRuntimeConeVisualization(baseAimDirection);

            if (weaponConfig == null || projectileLauncher == null)
            {
                return;
            }

            if (!ShouldAttemptFire())
            {
                return;
            }

            if (!TryGetNearestHostileTargetInCone(baseAimDirection, out Vector2 nearestTargetPosition))
            {
                return;
            }

            if (!_runtimeState.TryConsumeShot(ResolveCooldownSeconds()))
            {
                return;
            }

            Vector2 fireDirection = fireControlMode == FireControlMode.PureAutoFire
                ? nearestTargetPosition - ResolveTargetingOrigin()
                : baseAimDirection;
            FireShot(fireDirection);

            if (weaponInventory != null && weaponInventory.ConsumeTemporaryShot())
            {
                SyncActiveWeaponConfig(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showAutoFireCone)
            {
                return;
            }

            float targetingRange = ResolveTargetingRange();
            if (targetingRange <= 0f)
            {
                return;
            }

            Vector2 origin = ResolveTargetingOrigin();
            Vector2 forward = ResolveEditorAimDirection();
            DrawAutoFireConeGizmo(origin, forward, targetingRange, ResolveAutoFireConeDegrees());
        }

        private void FireShot(Vector2 baseAimDirection)
        {
            if (baseAimDirection.sqrMagnitude <= 0f)
            {
                baseAimDirection = Vector2.up;
            }

            CurrentSpreadOffsetDegrees = WeaponSpreadUtility.CalculateSpreadOffsetDegrees(
                movementController != null ? movementController.SpeedNormalized : 0f,
                ResolveMinimumSpreadDegrees(),
                ResolveMaximumSpreadDegrees(),
                Random.value);

            Vector2 fireDirection = WeaponSpreadUtility.ApplySpread(baseAimDirection, CurrentSpreadOffsetDegrees);
            Vector2 origin = (Vector2)transform.position + fireDirection * ResolveMuzzleOffset();
            projectileLauncher.LaunchVolley(
                origin,
                fireDirection,
                weaponConfig,
                playerRoot != null ? playerRoot.FactionId : FactionId.Player,
                playerRoot != null ? playerRoot.BodyCollider : null,
                this,
                impactFeedbackSpawner,
                ResolveDamage());

            Debug.DrawRay(origin, fireDirection * 0.75f, Color.white, 0.15f);
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            aimController ??= GetComponent<PlayerAimController>();
            movementController ??= GetComponent<PlayerMovementController>();
            weaponInventory ??= GetComponent<PlayerWeaponInventory>();
            upgradeRuntime ??= GetComponent<PlayerUpgradeRuntime>();
            projectileLauncher ??= GetComponent<ProjectileWeaponLauncher>();
            impactFeedbackSpawner ??= GetComponent<WeaponImpactFeedbackSpawner>();
        }

        private bool TryGetNearestHostileTargetInCone(Vector2 aimDirection, out Vector2 nearestTargetPosition)
        {
            nearestTargetPosition = default;
            float targetingRange = ResolveTargetingRange();
            if (targetingRange <= 0f)
            {
                return false;
            }

            Vector2 targetingOrigin = ResolveTargetingOrigin();
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(targetingOrigin, targetingRange);
            FactionId sourceFaction = playerRoot != null ? playerRoot.FactionId : FactionId.Player;
            Collider2D ignoredCollider = playerRoot != null ? playerRoot.BodyCollider : null;

            _targetCandidatePositions.Clear();

            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];
                if (!WeaponHitUtility.IsValidDamageableTarget(overlap, ignoredCollider, sourceFaction))
                {
                    continue;
                }

                _targetCandidatePositions.Add(overlap.bounds.center);
            }

            int selectedIndex = WeaponAutoFireTargetingUtility.SelectNearestTargetIndex(
                targetingOrigin,
                aimDirection,
                _targetCandidatePositions,
                targetingRange,
                ResolveAutoFireConeDegrees());
            if (selectedIndex < 0)
            {
                return false;
            }

            nearestTargetPosition = _targetCandidatePositions[selectedIndex];
            return true;
        }

        private void ResolveDefaults()
        {
            if (weaponConfig == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                weaponConfig = ProjectContext.Instance.ConfigRegistry.DefaultWeaponConfig;
            }
        }

        private Vector2 ResolveBaseAimDirection()
        {
            Vector2 aimDirection = aimController != null ? aimController.CurrentAimDirection : Vector2.up;
            if (aimDirection.sqrMagnitude > 0f)
            {
                return aimDirection.normalized;
            }

            return transform.up.sqrMagnitude > 0f ? ((Vector2)transform.up).normalized : Vector2.up;
        }

        private Vector2 ResolveEditorAimDirection()
        {
            if (Application.isPlaying)
            {
                return ResolveBaseAimDirection();
            }

            return transform.up.sqrMagnitude > 0f ? ((Vector2)transform.up).normalized : Vector2.up;
        }

        private Vector2 ResolveTargetingOrigin()
        {
            if (playerRoot != null && playerRoot.Body != null)
            {
                return playerRoot.Body.position;
            }

            return transform.position;
        }

        private float ResolveTargetingRange()
        {
            return weaponConfig != null ? Mathf.Max(0f, weaponConfig.Range) : 0f;
        }

        private float ResolveDamage()
        {
            if (weaponConfig == null)
            {
                return 0f;
            }

            return upgradeRuntime != null
                ? upgradeRuntime.ResolveWeaponDamage(weaponConfig.Damage)
                : weaponConfig.Damage;
        }

        private float ResolveCooldownSeconds()
        {
            if (weaponConfig == null)
            {
                return 0.01f;
            }

            return upgradeRuntime != null
                ? upgradeRuntime.ResolveWeaponCooldownSeconds(weaponConfig.CooldownSeconds)
                : weaponConfig.CooldownSeconds;
        }

        private float ResolveMinimumSpreadDegrees()
        {
            if (weaponConfig == null)
            {
                return 0f;
            }

            return upgradeRuntime != null
                ? upgradeRuntime.ResolveWeaponSpreadDegrees(weaponConfig.MinSpreadDegrees)
                : weaponConfig.MinSpreadDegrees;
        }

        private float ResolveMaximumSpreadDegrees()
        {
            if (weaponConfig == null)
            {
                return 0f;
            }

            float resolvedMinimumSpread = ResolveMinimumSpreadDegrees();
            float resolvedMaximumSpread = upgradeRuntime != null
                ? upgradeRuntime.ResolveWeaponSpreadDegrees(weaponConfig.MaxSpreadDegrees)
                : weaponConfig.MaxSpreadDegrees;
            return Mathf.Max(resolvedMinimumSpread, resolvedMaximumSpread);
        }

        private float ResolveAutoFireConeDegrees()
        {
            if (weaponConfig != null && weaponConfig.AutoFireConeDegrees > 0f)
            {
                return weaponConfig.AutoFireConeDegrees;
            }

            return legacyAutoFireConeDegrees > 0f ? legacyAutoFireConeDegrees : DefaultAutoFireConeDegrees;
        }

        private bool ShouldAttemptFire()
        {
            if (playerRoot != null && playerRoot.IsCarryingPot)
            {
                return false;
            }

            return fireControlMode switch
            {
                FireControlMode.PureAutoFire => true,
                _ => inputReader != null && inputReader.FireHeld
            };
        }

        private void UpdateRuntimeConeVisualization(Vector2 forward)
        {
            if (!Application.isPlaying || !showAutoFireCone || (playerRoot != null && playerRoot.IsCarryingPot))
            {
                SetRuntimeConeVisible(false);
                return;
            }

            float targetingRange = ResolveTargetingRange();
            if (targetingRange <= 0f)
            {
                SetRuntimeConeVisible(false);
                return;
            }

            EnsureRuntimeConeRenderer();
            if (_runtimeConeMeshRenderer == null)
            {
                return;
            }

            Vector3 origin3D = transform.InverseTransformPoint(ResolveTargetingOrigin());
            float halfConeDegrees = ResolveAutoFireConeDegrees() * 0.5f;
            int arcPointCount = AutoFireConeGizmoSegments + 2;
            int[] triangles = new int[AutoFireConeGizmoSegments * 3];

            for (int i = 0; i < AutoFireConeGizmoSegments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            Vector3[] vertices = new Vector3[arcPointCount];
            vertices[0] = origin3D;

            for (int index = 0; index <= AutoFireConeGizmoSegments; index++)
            {
                float t = index / (float)AutoFireConeGizmoSegments;
                float angle = Mathf.Lerp(-halfConeDegrees, halfConeDegrees, t);
                Vector2 arcDirection = Quaternion.Euler(0f, 0f, angle) * forward;
                vertices[index + 1] = origin3D + transform.InverseTransformDirection(arcDirection * targetingRange);
            }

            _runtimeConeMesh.Clear();
            _runtimeConeMesh.vertices = vertices;
            _runtimeConeMesh.triangles = triangles;
            _runtimeConeMesh.RecalculateBounds();

            _runtimeConeMaterial.color = autoFireConeColor;
            _runtimeConeMeshRenderer.enabled = true;
        }

        private void EnsureRuntimeConeRenderer()
        {
            if (_runtimeConeMeshRenderer != null)
            {
                return;
            }

            GameObject rendererObject = new GameObject("AutoFireConeRuntime");
            rendererObject.transform.SetParent(transform, false);

            _runtimeConeMeshFilter = rendererObject.AddComponent<MeshFilter>();
            _runtimeConeMeshRenderer = rendererObject.AddComponent<MeshRenderer>();

            _runtimeConeMesh = new Mesh();
            _runtimeConeMesh.MarkDynamic();
            _runtimeConeMeshFilter.mesh = _runtimeConeMesh;

            _runtimeConeMeshRenderer.enabled = false;
            _runtimeConeMeshRenderer.sortingOrder = 9;

            _runtimeConeMaterial = new Material(Shader.Find("Sprites/Default"));
            _runtimeConeMaterial.color = Color.white;
            _runtimeConeMeshRenderer.material = _runtimeConeMaterial;
        }

        private void SetRuntimeConeVisible(bool isVisible)
        {
            if (_runtimeConeMeshRenderer != null)
            {
                _runtimeConeMeshRenderer.enabled = isVisible;
            }
        }

        private void DrawAutoFireConeGizmo(Vector2 origin, Vector2 forward, float targetingRange, float coneDegrees)
        {
            Gizmos.color = autoFireConeColor;

            float halfConeDegrees = coneDegrees * 0.5f;
            Vector2 leftDirection = Quaternion.Euler(0f, 0f, halfConeDegrees) * forward;
            Vector2 rightDirection = Quaternion.Euler(0f, 0f, -halfConeDegrees) * forward;
            Gizmos.DrawLine(origin, origin + leftDirection * targetingRange);
            Gizmos.DrawLine(origin, origin + rightDirection * targetingRange);

            Vector2 previousPoint = origin + rightDirection * targetingRange;
            for (int index = 1; index <= AutoFireConeGizmoSegments; index++)
            {
                float t = index / (float)AutoFireConeGizmoSegments;
                float angle = Mathf.Lerp(-halfConeDegrees, halfConeDegrees, t);
                Vector2 arcDirection = Quaternion.Euler(0f, 0f, angle) * forward;
                Vector2 currentPoint = origin + arcDirection * targetingRange;
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        private void SyncActiveWeaponConfig(bool preserveCooldownByDefault = true)
        {
            WeaponConfig desiredConfig = weaponInventory != null ? weaponInventory.ActiveWeaponConfig : weaponConfig;
            bool resetCooldown = weaponInventory?.ConsumePendingCooldownReset() ?? false;

            if (desiredConfig == weaponConfig && !resetCooldown)
            {
                return;
            }

            weaponConfig = desiredConfig;
            _runtimeState.AssignConfig(weaponConfig, resetCooldown || !preserveCooldownByDefault);
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