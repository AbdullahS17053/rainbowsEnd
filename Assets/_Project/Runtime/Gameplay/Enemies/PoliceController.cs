using RainbowsEnd.Core.Animation;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Orchestration;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(ProjectileWeaponLauncher))]
    [RequireComponent(typeof(WeaponImpactFeedbackSpawner))]
    [RequireComponent(typeof(RangedAttackController))]
    public sealed class PoliceController : MonoBehaviour, ITeamAffiliation, IPoolable
    {
        [SerializeField] private EnemyConfig config;
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Health health;
        [SerializeField] private ProjectileWeaponLauncher projectileLauncher;
        [SerializeField] private WeaponImpactFeedbackSpawner impactFeedbackSpawner;
        [SerializeField] private RangedAttackController rangedAttackController;
        [SerializeField] private CharacterAnimationBridge animationBridge;
        [SerializeField] private GameObject pistolVisual;
        [SerializeField] private Transform pistolMuzzle;
        [SerializeField] private Transform shotgunMuzzle;
        [SerializeField] private GameObject shotgunVisual;

        private PrefabPool _owningPool;
        private PrefabPool _weaponPickupPool;
        private PrefabPool _goldPickupPool;
        private PoliceSpawner _owningSpawner;
        private WeaponConfig _dropWeaponConfig;
        private int _dropAmmoCount;
        private PlayerRoot _targetPlayer;
        private bool _deathHandled;
        private bool _usesShotgun;
        private PoliceMobConfig _policeMobConfig;
        private PoliceCombatPhase _combatPhase;
        private float _combatPhaseTimer;
        private int _repositionSign = 1;
        private Vector2[] _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;

        public FactionId FactionId => FactionId.Police;

        public SpriteRenderer VisualRenderer => visualRenderer;

        public Health Health => health;

        public void Configure(
            EnemyConfig enemyConfig,
            WeaponConfig policeWeaponConfig,
            Transform actorVisualRoot,
            SpriteRenderer actorVisualRenderer,
            Rigidbody2D actorBody,
            Collider2D actorCollider,
            Health actorHealth,
            ProjectileWeaponLauncher weaponLauncher,
            WeaponImpactFeedbackSpawner feedbackSpawner,
            RangedAttackController attackController)
        {
            config = enemyConfig;
            weaponConfig = policeWeaponConfig;
            visualRoot = actorVisualRoot;
            visualRenderer = actorVisualRenderer;
            body = actorBody;
            bodyCollider = actorCollider;
            health = actorHealth;
            projectileLauncher = weaponLauncher;
            impactFeedbackSpawner = feedbackSpawner;
            rangedAttackController = attackController;
        }

        public void Activate(
            PoliceSpawner owningSpawner,
            PrefabPool owningPool,
            PrefabPool weaponPickupPool,
            EnemyConfig enemyConfig,
            WeaponConfig policeWeaponConfig,
            WeaponConfig dropWeaponConfig,
            int dropAmmoCount)
        {
            Activate(
                owningSpawner,
                owningPool,
                weaponPickupPool,
                goldPickupPool: null,
                enemyConfig,
                policeWeaponConfig,
                dropWeaponConfig,
                dropAmmoCount,
                useShotgun: false,
                policeMobConfig: null);
        }

        public void Activate(
            PoliceSpawner owningSpawner,
            PrefabPool owningPool,
            PrefabPool weaponPickupPool,
            EnemyConfig enemyConfig,
            WeaponConfig policeWeaponConfig,
            WeaponConfig dropWeaponConfig,
            int dropAmmoCount,
            bool useShotgun,
            PoliceMobConfig policeMobConfig)
        {
            Activate(
                owningSpawner,
                owningPool,
                weaponPickupPool,
                goldPickupPool: null,
                enemyConfig,
                policeWeaponConfig,
                dropWeaponConfig,
                dropAmmoCount,
                useShotgun,
                policeMobConfig);
        }

        public void Activate(
            PoliceSpawner owningSpawner,
            PrefabPool owningPool,
            PrefabPool weaponPickupPool,
            PrefabPool goldPickupPool,
            EnemyConfig enemyConfig,
            WeaponConfig policeWeaponConfig,
            WeaponConfig dropWeaponConfig,
            int dropAmmoCount,
            bool useShotgun,
            PoliceMobConfig policeMobConfig)
        {
            _owningSpawner = owningSpawner;
            _owningPool = owningPool;
            _weaponPickupPool = weaponPickupPool;
            _goldPickupPool = goldPickupPool;
            _dropWeaponConfig = dropWeaponConfig;
            _dropAmmoCount = Mathf.Max(0, dropAmmoCount);
            _usesShotgun = useShotgun;
            _policeMobConfig = policeMobConfig;
            ResetCombatCycle();
            _deathHandled = false;

            if (enemyConfig != null)
            {
                config = enemyConfig;
            }

            if (policeWeaponConfig != null)
            {
                weaponConfig = policeWeaponConfig;
            }

            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }

            ApplyWeaponVisual(_usesShotgun);
            body.linearVelocity = Vector2.zero;
            rangedAttackController.Configure(
                projectileLauncher,
                impactFeedbackSpawner,
                weaponConfig,
                FactionId,
                bodyCollider,
                ResolveWeaponMuzzle());
            RandomizeVariantAndSkinTone();
        }

        public void OnSpawned()
        {
            CacheReferences();
            RandomizeVariantAndSkinTone();
            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }
            if (body != null)
            {
                body.simulated = true;
            }
            _owningPool = null;
            _weaponPickupPool = null;
            _goldPickupPool = null;
            _owningSpawner = null;
            _policeMobConfig = null;
            _dropWeaponConfig = null;
            _dropAmmoCount = 0;
            _usesShotgun = false;
            _targetPlayer = null;
            ResetCombatCycle();
            ApplyWeaponVisual(false);
        }

        private static readonly Color[] NaturalSkinTones = new Color[]
        {
            new Color(1.0f, 0.88f, 0.78f, 1f), // Very light
            new Color(0.95f, 0.82f, 0.72f, 1f), // Fair/Peach
            new Color(0.85f, 0.68f, 0.54f, 1f), // Medium Beige
            new Color(0.72f, 0.52f, 0.38f, 1f), // Golden Tan
            new Color(0.55f, 0.36f, 0.22f, 1f), // Warm Bronze/Caramel
            new Color(0.35f, 0.22f, 0.14f, 1f)  // Deep Rich Brown
        };

        private void RandomizeVariantAndSkinTone()
        {
            Transform vRoot = visualRoot != null ? visualRoot : transform.Find("VisualRoot");
            if (vRoot == null) return;

            Transform maleTr = vRoot.Find("Character_Male_Police");
            Transform femaleTr = vRoot.Find("Character_Female_Police");

            GameObject activeModel = null;
            if (maleTr != null && femaleTr != null)
            {
                bool isFemale = Random.value > 0.5f;
                maleTr.gameObject.SetActive(!isFemale);
                femaleTr.gameObject.SetActive(isFemale);
                activeModel = isFemale ? femaleTr.gameObject : maleTr.gameObject;
            }
            else if (maleTr != null)
            {
                activeModel = maleTr.gameObject;
            }
            else if (femaleTr != null)
            {
                activeModel = femaleTr.gameObject;
            }

            if (activeModel != null)
            {
                Animator activeAnim = activeModel.GetComponentInChildren<Animator>(true);
                if (activeAnim != null)
                {
                    activeAnim.applyRootMotion = false;
                }

                animationBridge ??= GetComponent<CharacterAnimationBridge>();
                if (animationBridge != null)
                {
                    animationBridge.SetActiveAnimator(activeAnim);
                    animationBridge.ResetDeath();
                }

                Color skinTone = NaturalSkinTones[Random.Range(0, NaturalSkinTones.Length)];
                Renderer[] rends = activeModel.GetComponentsInChildren<Renderer>(true);
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                for (int i = 0; i < rends.Length; i++)
                {
                    if (rends[i] == null) continue;
                    rends[i].GetPropertyBlock(mpb);
                    mpb.SetColor("_Color", skinTone);
                    mpb.SetColor("_BaseColor", skinTone);
                    rends[i].SetPropertyBlock(mpb);
                }
            }
        }

        public void OnDespawned()
        {
            _owningPool = null;
            _weaponPickupPool = null;
            _goldPickupPool = null;
            _owningSpawner = null;
            _dropWeaponConfig = null;
            _dropAmmoCount = 0;
            _targetPlayer = null;
            _deathHandled = false;
            _policeMobConfig = null;
            _usesShotgun = false;
            ResetCombatCycle();
            ApplyWeaponVisual(false);
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

            if (health != null && config != null)
            {
                health.Initialize(config.MaxHealth);
            }
        }

private void Update()
        {
            rangedAttackController.Tick(Time.deltaTime);

            if (health == null || health.IsDepleted)
            {
                HandleDeath();
                return;
            }

            TickPoliceCombat();
        }

        private void HandleDeath()
        {
            if (_deathHandled)
            {
                return;
            }

            _deathHandled = true;
            RecordKill();
            SpawnDeathReward();

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            animationBridge?.TriggerDeath();

            CombatFeedbackController.DelayedDespawn(this, () =>
            {
                _owningSpawner?.NotifyPoliceDespawned(this);

                PrefabPool owningPool = _owningPool;
                _owningPool = null;
                if (owningPool != null)
                {
                    owningPool.Return(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }, 3.0f);
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

        private void SpawnDeathReward()
        {
            bool weaponAvailable = _weaponPickupPool != null
                && _dropWeaponConfig != null
                && _dropAmmoCount > 0;
            int goldAmount = _policeMobConfig != null
                ? Mathf.Max(0, _policeMobConfig.GoldDropAmount)
                : 10;
            bool goldAvailable = _goldPickupPool != null && goldAmount > 0;

            PoliceDeathRewardKind rewardKind =
                PoliceDeathRewardUtility.ResolveRewardKind(
                    Random.value,
                    Random.value,
                    _policeMobConfig != null ? _policeMobConfig.DeathDropChance : 0.33f,
                    _policeMobConfig != null ? _policeMobConfig.WeaponRewardChance : 0.5f,
                    weaponAvailable,
                    goldAvailable);

            switch (rewardKind)
            {
                case PoliceDeathRewardKind.Weapon:
                    SpawnWeaponPickup();
                    break;
                case PoliceDeathRewardKind.Gold:
                    SpawnGoldPickup(goldAmount);
                    break;
            }
        }

        private void SpawnGoldPickup(int goldAmount)
        {
            if (_goldPickupPool == null || goldAmount <= 0)
            {
                return;
            }

            GoldPickup goldPickup = _goldPickupPool.Rent<GoldPickup>(
                transform.position,
                Quaternion.identity);
            if (goldPickup == null)
            {
                return;
            }

            goldPickup.Activate(_goldPickupPool, goldAmount);
        }

        private void SpawnWeaponPickup()
        {
            if (_weaponPickupPool == null || _dropWeaponConfig == null || _dropAmmoCount <= 0)
            {
                return;
            }

            GameObject pickupInstance = _weaponPickupPool.Rent(transform.position, Quaternion.identity);
            WeaponPickup pickup = pickupInstance.GetComponent<WeaponPickup>();
            if (pickup == null)
            {
                _weaponPickupPool.Return(pickupInstance);
                return;
            }

            pickup.Activate(_weaponPickupPool, _dropWeaponConfig, _dropAmmoCount);
        }

        private void ApplyWeaponVisual(bool useShotgun)
        {
            if (pistolVisual != null)
            {
                pistolVisual.transform.localPosition = new Vector3(0.08f, 0.02f, 0f);
                pistolVisual.transform.localRotation = Quaternion.Euler(0f, 82f, 90f);
                pistolVisual.SetActive(!useShotgun);
            }

            if (shotgunVisual != null)
            {
                shotgunVisual.transform.localPosition = new Vector3(0.10f, 0.02f, 0f);
                shotgunVisual.transform.localRotation = Quaternion.Euler(0f, 82f, -180f);
                shotgunVisual.SetActive(useShotgun);
            }
        }

        private Transform ResolveWeaponMuzzle()
        {
            return _usesShotgun ? shotgunMuzzle : pistolMuzzle;
        }

        private void ResolveTargetPlayer()
        {
            if (_targetPlayer != null && !_targetPlayer.Health.IsDepleted)
            {
                return;
            }

            _targetPlayer = FindFirstObjectByType<PlayerRoot>();
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            projectileLauncher ??= GetComponent<ProjectileWeaponLauncher>();
            impactFeedbackSpawner ??= GetComponent<WeaponImpactFeedbackSpawner>();
            rangedAttackController ??= GetComponent<RangedAttackController>();
            animationBridge ??= GetComponent<CharacterAnimationBridge>();

            if (visualRoot == null)
            {
                Transform child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }

            pistolMuzzle ??= pistolVisual?.transform.Find("Muzzle");
            shotgunMuzzle ??= shotgunVisual?.transform.Find("Muzzle");

            if (visualRenderer == null && visualRoot != null)
            {
                visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
            }
        }


private void BeginReposition()
        {
            _combatPhase = PoliceCombatPhase.Reposition;
            _combatPhaseTimer = _policeMobConfig != null
                ? Mathf.Max(0f, _policeMobConfig.RepositionSeconds)
                : 0.75f;
            _repositionSign = PoliceCombatCycleUtility.ResolveRepositionSign(Random.value);
        }


private void BeginAimHold()
        {
            _combatPhase = PoliceCombatPhase.AimHold;
            _combatPhaseTimer = _policeMobConfig != null
                ? Mathf.Max(0f, _policeMobConfig.AimHoldSeconds)
                : 0.25f;
        }


private void ResetCombatCycle()
        {
            _combatPhase = PoliceCombatPhase.Approach;
            _combatPhaseTimer = 0f;
            _repositionSign = 1;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }


private void TickReposition(Vector2 direction, Vector2 separation)
        {
            float moveSpeed = Mathf.Max(0f, config.MoveSpeed);
            if (TerrainZone.IsInForest(transform.position))
            {
                moveSpeed *= 0.90f;
            }
            Vector2 strafeDirection = new Vector2(-direction.y, direction.x) * _repositionSign;
            Vector2 combinedVelocity = strafeDirection * moveSpeed + separation;
            if (combinedVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * moveSpeed;
            }

            body.linearVelocity = combinedVelocity;
            if (visualRoot != null && combinedVelocity.sqrMagnitude > 0.001f)
            {
                visualRoot.up = combinedVelocity.normalized;
            }

            _combatPhaseTimer -= Time.deltaTime;
            _combatPhase = PoliceCombatCycleUtility.ResolveReposition(
                _combatPhaseTimer,
                true);
            if (_combatPhase == PoliceCombatPhase.Approach)
            {
                ResetCombatCycle();
            }
        }


private void TickAimHold(Vector2 direction, bool canShoot)
        {
            body.linearVelocity = Vector2.zero;
            if (visualRoot != null && direction.sqrMagnitude > 0.001f)
            {
                visualRoot.up = direction;
            }

            if (!canShoot)
            {
                ResetCombatCycle();
                return;
            }

            _combatPhaseTimer -= Time.deltaTime;
            if (_combatPhaseTimer > 0f)
            {
                return;
            }

            bool fireSucceeded = rangedAttackController.TryFire(
                transform.position,
                direction,
                0f,
                this);
            if (fireSucceeded)
            {
                animationBridge?.TriggerFire(lockFacing: true);
            }

            _combatPhase = PoliceCombatCycleUtility.ResolveAimHold(
                0f,
                true,
                fireSucceeded);
            if (_combatPhase == PoliceCombatPhase.Reposition)
            {
                BeginReposition();
            }
        }


private void TickApproach(
            float distanceToTarget,
            float preferredRange,
            Vector2 direction,
            bool hasLineOfSight,
            bool canShoot,
            Vector2 separation)
        {
            if (PoliceCombatCycleUtility.ShouldBeginAimHold(
                distanceToTarget,
                preferredRange,
                canShoot))
            {
                body.linearVelocity = Vector2.zero;
                if (visualRoot != null)
                {
                    visualRoot.up = direction;
                }

                BeginAimHold();
                return;
            }

            bool shouldMove = distanceToTarget > preferredRange || !hasLineOfSight;
            Vector2 moveDirection = Vector2.zero;
            if (shouldMove)
            {
                _pathRefreshTimer -= Time.deltaTime;
                if (_currentPath == null || _pathRefreshTimer <= 0f)
                {
                    if (NavMeshPathfinder.TryGetPath(
                        transform.position,
                        _targetPlayer.transform.position,
                        out Vector2[] newPath))
                    {
                        _currentPath = newPath;
                        _currentWaypointIndex = 0;
                    }

                    _pathRefreshTimer = 0.5f;
                }

                if (_currentPath != null)
                {
                    moveDirection = NavMeshPathfinder.GetSteeringDirection(
                        transform.position,
                        _currentPath,
                        ref _currentWaypointIndex);
                }

                if (moveDirection == Vector2.zero)
                {
                    moveDirection = direction;
                }
            }

            float moveSpeed = Mathf.Max(0f, config.MoveSpeed);
            if (TerrainZone.IsInForest(transform.position))
            {
                moveSpeed *= 0.90f;
            }
            Vector2 combinedVelocity = moveDirection * moveSpeed + separation;
            if (combinedVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            {
                combinedVelocity = combinedVelocity.normalized * moveSpeed;
            }

            body.linearVelocity = combinedVelocity;

            if (visualRoot != null)
            {
                Vector2 facingDirection = moveDirection.sqrMagnitude > 0.001f
                    ? moveDirection
                    : direction;
                if (facingDirection.sqrMagnitude > 0.001f)
                {
                    visualRoot.up = facingDirection;
                }
            }

            if (_currentPath != null)
            {
                for (int index = 0; index < _currentPath.Length - 1; index++)
                {
                    Debug.DrawLine(_currentPath[index], _currentPath[index + 1], Color.cyan);
                }
            }
        }


private void TickPoliceCombat()
        {
            ResolveTargetPlayer();

            Vector2 separation = EnemySeparationUtility.CalculateSeparation(
                transform.position,
                bodyCollider,
                config != null ? config.SeparationRadius : 0.6f,
                config != null ? config.SeparationStrength : 2f);

            Vector3 targetPosition;
            if (PlayerDecoy.ActiveDecoy != null)
            {
                targetPosition = PlayerDecoy.ActiveDecoy.transform.position;
            }
            else if (_targetPlayer != null && !_targetPlayer.IsDisabled && (!_targetPlayer.IsConcealed || _targetPlayer.IsCarryingPot))
            {
                targetPosition = _targetPlayer.transform.position;
            }
            else
            {
                ResetCombatCycle();
                body.linearVelocity = separation;
                return;
            }

            if (config == null || weaponConfig == null)
            {
                ResetCombatCycle();
                body.linearVelocity = separation;
                return;
            }

            Vector2 toTarget = targetPosition - transform.position;
            float distanceToTarget = toTarget.magnitude;
            float engagementRange = Mathf.Max(0.5f, config.EngagementRange);
            if (distanceToTarget > engagementRange)
            {
                ResetCombatCycle();
                body.linearVelocity = separation;
                return;
            }

            float preferredRange = Mathf.Clamp(weaponConfig.Range * 0.7f, 2f, engagementRange);
            Vector2 direction = distanceToTarget > 0.001f ? toTarget / distanceToTarget : (Vector2)transform.up;
            bool hasLineOfSight = distanceToTarget <= 0.001f
                || CoverBlocker.GetNearestBlockDistance(
                    transform.position,
                    direction,
                    Mathf.Min(distanceToTarget, weaponConfig.Range)) >= distanceToTarget;
            bool canShoot = distanceToTarget <= weaponConfig.Range && hasLineOfSight;

            switch (_combatPhase)
            {
                case PoliceCombatPhase.AimHold:
                    TickAimHold(direction, canShoot);
                    return;
                case PoliceCombatPhase.Reposition:
                    TickReposition(direction, separation);
                    return;
                default:
                    TickApproach(
                        distanceToTarget,
                        preferredRange,
                        direction,
                        hasLineOfSight,
                        canShoot,
                        separation);
                    return;
            }
        }
}
}