using RainbowsEnd.Audio;
using RainbowsEnd.Core.Animation;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.World;
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
        [SerializeField] private Transform faeBoltMuzzle;
        [SerializeField] private Transform pistolMuzzle;
        [SerializeField] private Transform shotgunMuzzle;
        [SerializeField] private Transform assaultRifleMuzzle;
        [SerializeField] private GameObject pistolVisual;
        [SerializeField] private GameObject shotgunVisual;
        [SerializeField] private GameObject assaultRifleVisual;
        [SerializeField] private CharacterAnimationBridge animationBridge;

        [Header("Audio")]
        [SerializeField] private AudioCueSO fireCue;

        public AudioCueSO FireCue => fireCue;
        public void ConfigureFireCue(AudioCueSO cue) => fireCue = cue;

        private readonly System.Collections.Generic.List<Vector2> _targetCandidatePositions = new System.Collections.Generic.List<Vector2>();
        private readonly WeaponRuntimeState _runtimeState = new WeaponRuntimeState();

        private readonly System.Collections.Generic.List<Collider2D> _targetCandidateColliders = new System.Collections.Generic.List<Collider2D>();

        private MeshRenderer _runtimeConeMeshRenderer;
        private MeshFilter _runtimeConeMeshFilter;
        private Mesh _runtimeConeMesh;
                private Material _runtimeConeMaterial;
        private bool _isHipFiring;
        private float _resolvedTargetingRange;
        private float _resolvedAutoFireConeDegrees;
        private int _resolvedPostureFrame = -1;

        [Header("Recoil & Presentation Juice")]
        [SerializeField] private float firearmScaleMultiplier = 1.35f;
        [SerializeField] private float recoilKickDistance = 0.15f;
        [SerializeField] private float recoilRecoverySpeed = 16f;

        private Vector3 _pistolBaseLocalPos;
        private Vector3 _shotgunBaseLocalPos;
        private Vector3 _assaultRifleBaseLocalPos;
        private Vector3 _faeBoltBaseLocalPos;
        private float _currentRecoilKick;

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
            CacheVisualBasesAndScale();
            ResolveDefaults();
            weaponInventory?.ConfigureDefaultWeapon(weaponConfig);
            _runtimeState.Configure(weaponConfig);
        }

        private void CacheVisualBasesAndScale()
        {
            if (pistolVisual != null)
            {
                _pistolBaseLocalPos = pistolVisual.transform.localPosition;
                pistolVisual.transform.localScale = Vector3.one * firearmScaleMultiplier;
            }
            if (shotgunVisual != null)
            {
                _shotgunBaseLocalPos = shotgunVisual.transform.localPosition;
                shotgunVisual.transform.localScale = Vector3.one * firearmScaleMultiplier;
            }
            if (assaultRifleVisual != null)
            {
                _assaultRifleBaseLocalPos = assaultRifleVisual.transform.localPosition;
                assaultRifleVisual.transform.localScale = Vector3.one * firearmScaleMultiplier;
            }
            if (faeBoltMuzzle != null)
            {
                _faeBoltBaseLocalPos = faeBoltMuzzle.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (_currentRecoilKick > 0.0005f)
            {
                _currentRecoilKick = Mathf.MoveTowards(_currentRecoilKick, 0f, Time.deltaTime * recoilRecoverySpeed);
            }
            else
            {
                _currentRecoilKick = 0f;
            }

            ApplyRecoilOffset();
        }

        private void ApplyRecoilOffset()
        {
            Vector3 recoilOffset = -Vector3.forward * _currentRecoilKick;
            if (pistolVisual != null && pistolVisual.activeSelf)
            {
                pistolVisual.transform.localPosition = _pistolBaseLocalPos + recoilOffset;
            }
            if (shotgunVisual != null && shotgunVisual.activeSelf)
            {
                shotgunVisual.transform.localPosition = _shotgunBaseLocalPos + recoilOffset;
            }
            if (assaultRifleVisual != null && assaultRifleVisual.activeSelf)
            {
                assaultRifleVisual.transform.localPosition = _assaultRifleBaseLocalPos + recoilOffset;
            }
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
            ApplyWeaponPresentation();
            ResolvePlayerWeaponPosture();

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
            Transform muzzle = ResolveActiveMuzzle();
            Vector2 origin = RangedAttackController.ResolveFireOrigin(
                transform.position,
                fireDirection,
                muzzle,
                ResolveMuzzleOffset());
            int launchedCount = projectileLauncher.LaunchVolley(
                origin,
                fireDirection,
                weaponConfig,
                playerRoot != null ? playerRoot.FactionId : FactionId.Player,
                playerRoot != null ? playerRoot.BodyCollider : null,
                this,
                impactFeedbackSpawner,
                ResolveDamage(),
                rangeOverride: ResolveTargetingRange());

            if (launchedCount > 0)
            {
                ProjectileWeaponLauncher.PlayMuzzleFeedback(muzzle);
                if (fireCue != null)
                {
                    AudioManager.Instance?.PlayCue(fireCue, origin);
                }
            }

            ApplyRecoilFeedback();
            playerRoot?.NotifyShotFired();
            Debug.DrawRay(origin, fireDirection * 0.75f, Color.white, 0.15f);

            animationBridge?.TriggerFire();
        }

        private void ApplyRecoilFeedback()
        {
            float kick = recoilKickDistance;
            float shakeIntensity = 0.08f;
            float shakeDuration = 0.08f;

            if (weaponConfig != null)
            {
                switch (weaponConfig.Presentation)
                {
                    case WeaponPresentation.Shotgun:
                        kick *= 1.8f;
                        shakeIntensity = 0.22f;
                        shakeDuration = 0.14f;
                        break;
                    case WeaponPresentation.AssaultRifle:
                        kick *= 1.1f;
                        shakeIntensity = 0.1f;
                        shakeDuration = 0.07f;
                        break;
                    case WeaponPresentation.Pistol:
                        kick *= 1.2f;
                        shakeIntensity = 0.12f;
                        shakeDuration = 0.09f;
                        break;
                    case WeaponPresentation.FaeBolt:
                    default:
                        kick *= 0.6f;
                        shakeIntensity = 0.04f;
                        shakeDuration = 0.05f;
                        break;
                }
            }

            _currentRecoilKick = kick;
            CombatFeedbackController.Instance?.PlayScreenShake(shakeIntensity, shakeDuration);
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
            animationBridge ??= GetComponent<CharacterAnimationBridge>();

            Transform hand = transform.Find("VisualRoot/Fairy Visual/Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R");
            if (hand == null)
            {
                return;
            }

            faeBoltMuzzle ??= hand.Find("FaeBoltMuzzle");
            pistolVisual ??= hand.Find("PlayerPistolVisual")?.gameObject;
            shotgunVisual ??= hand.Find("PlayerShotgunVisual")?.gameObject;
            assaultRifleVisual ??= hand.Find("PlayerAssaultRifleVisual")?.gameObject;
            pistolMuzzle ??= pistolVisual?.transform.Find("Muzzle");
            shotgunMuzzle ??= shotgunVisual?.transform.Find("Muzzle");
            assaultRifleMuzzle ??= assaultRifleVisual?.transform.Find("Muzzle");
        }

        private void ApplyWeaponPresentation()
        {
            WeaponPresentation presentation = weaponConfig != null
                ? weaponConfig.Presentation
                : WeaponPresentation.FaeBolt;

            if (faeBoltMuzzle != null)
            {
                faeBoltMuzzle.gameObject.SetActive(presentation == WeaponPresentation.FaeBolt);
            }

            if (pistolVisual != null)
            {
                pistolVisual.SetActive(presentation == WeaponPresentation.Pistol);
            }

            if (shotgunVisual != null)
            {
                shotgunVisual.SetActive(presentation == WeaponPresentation.Shotgun);
            }

            if (assaultRifleVisual != null)
            {
                assaultRifleVisual.SetActive(presentation == WeaponPresentation.AssaultRifle);
            }
        }

        private Transform ResolveActiveMuzzle()
        {
            switch (weaponConfig != null ? weaponConfig.Presentation : WeaponPresentation.FaeBolt)
            {
                case WeaponPresentation.Pistol:
                    return pistolMuzzle;
                case WeaponPresentation.Shotgun:
                    return shotgunMuzzle;
                case WeaponPresentation.AssaultRifle:
                    return assaultRifleMuzzle;
                default:
                    return faeBoltMuzzle;
            }
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
            _targetCandidateColliders.Clear();

            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];
                if (!WeaponHitUtility.IsValidDamageableTarget(overlap, ignoredCollider, sourceFaction))
                {
                    continue;
                }

                _targetCandidateColliders.Add(overlap);
                _targetCandidatePositions.Add(overlap.bounds.center);
            }

            while (_targetCandidatePositions.Count > 0)
            {
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

                Vector2 targetPosition = _targetCandidatePositions[selectedIndex];
                if (HasLineOfSight(targetingOrigin, targetPosition, targetingRange))
                {
                    nearestTargetPosition = targetPosition;
                    return true;
                }

                _targetCandidatePositions.RemoveAt(selectedIndex);
                _targetCandidateColliders.RemoveAt(selectedIndex);
            }

            return false;
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
            ResolvePlayerWeaponPosture();
            return _resolvedTargetingRange;
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
            ResolvePlayerWeaponPosture();
            return _resolvedAutoFireConeDegrees;
        }

        private void ResolvePlayerWeaponPosture()
        {
            if (Application.isPlaying && _resolvedPostureFrame == Time.frameCount)
            {
                return;
            }

            float normalRange = ResolveBaseTargetingRange();
            float normalConeDegrees = ResolveBaseAutoFireConeDegrees();
            PlayerConfig playerConfig = playerRoot != null ? playerRoot.Config : null;
            float movementNormalized = movementController != null ? movementController.SpeedNormalized : 0f;
            float movementThreshold = playerConfig != null ? playerConfig.HipFireMovementThreshold : 0.1f;
            float rangeMultiplier = playerConfig != null ? playerConfig.HipFireRangeMultiplier : 0.7f;
            float coneMultiplier = playerConfig != null ? playerConfig.HipFireConeMultiplier : 1.75f;

            _isHipFiring = PlayerWeaponPostureUtility.IsHipFiring(movementNormalized, movementThreshold);
            _resolvedTargetingRange = PlayerWeaponPostureUtility.ResolveRange(
                normalRange,
                movementNormalized,
                movementThreshold,
                rangeMultiplier);
            _resolvedAutoFireConeDegrees = PlayerWeaponPostureUtility.ResolveConeDegrees(
                normalConeDegrees,
                movementNormalized,
                movementThreshold,
                coneMultiplier);
            _resolvedPostureFrame = Application.isPlaying ? Time.frameCount : -1;
        }

        private float ResolveBaseTargetingRange()
        {
            if (weaponConfig == null)
            {
                return 0f;
            }

            return upgradeRuntime != null
                ? upgradeRuntime.ResolveWeaponRange(weaponConfig.Range)
                : Mathf.Max(0f, weaponConfig.Range);
        }

        private float ResolveBaseAutoFireConeDegrees()
        {
            if (weaponConfig != null && weaponConfig.AutoFireConeDegrees > 0f)
            {
                return weaponConfig.AutoFireConeDegrees;
            }

            return legacyAutoFireConeDegrees > 0f ? legacyAutoFireConeDegrees : DefaultAutoFireConeDegrees;
        }
        private float GetNearestCoverBlockDistance(Vector2 origin, Vector2 direction, float maxDistance)
        {
            return CoverBlocker.GetNearestBlockDistance(origin, direction, maxDistance);
        }

        private bool HasLineOfSight(Vector2 from, Vector2 targetPosition, float maxRange)
        {
            Vector2 toTarget = targetPosition - from;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget < 0.01f)
            {
                return true;
            }

            float coverDistance = GetNearestCoverBlockDistance(from, toTarget / distanceToTarget, Mathf.Min(distanceToTarget, maxRange));
            return coverDistance >= distanceToTarget;
        }

        private bool ShouldAttemptFire()
        {
            if (playerRoot != null && !playerRoot.CanFire)
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
            Vector2 worldOrigin = ResolveTargetingOrigin();
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
                float arcDistance = GetNearestCoverBlockDistance(worldOrigin, arcDirection, targetingRange);
                vertices[index + 1] = origin3D + transform.InverseTransformDirection(arcDirection * arcDistance);
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