using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class UnicornController : MonoBehaviour, ITeamAffiliation
    {
        [SerializeField] private UnicornConfig config;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private RainbowTrailEmitter trailEmitter;

        private ArenaBoundsAuthoring _arenaBounds;
        private Vector2 _targetPoint;
        private Vector2 _currentVelocity;
        private float _retargetCooldown;
        private ArenaBoundarySide? _currentTargetSide;

        public FactionId FactionId => FactionId.Unicorn;

        public UnicornConfig Config => config;

        public Vector2 CurrentVelocity => _currentVelocity;

        public Vector2 CurrentTarget => _targetPoint;

        public void Configure(
            UnicornConfig unicornConfig,
            Rigidbody2D rigidbody2D,
            Collider2D collider2D,
            SpriteRenderer renderer,
            RainbowTrailEmitter emitter)
        {
            config = unicornConfig;
            body = rigidbody2D;
            bodyCollider = collider2D;
            visualRenderer = renderer;
            trailEmitter = emitter;
            trailEmitter?.Configure(this);
            ConfigureBody();
        }

        private void Reset()
        {
            CacheReferences();
            ConfigureBody();
        }

        private void OnValidate()
        {
            CacheReferences();
            ConfigureBody();
        }

        private void Awake()
        {
            CacheReferences();
            ResolveDefaults();
            ConfigureBody();
            ChooseNextTarget();
        }

        private void FixedUpdate()
        {
            if (config == null || body == null)
            {
                return;
            }

            AcquireArenaBounds();
            _retargetCooldown = Mathf.Max(0f, _retargetCooldown - Time.fixedDeltaTime);

            Vector2 currentPosition = body.position;
            if (ShouldRetarget(currentPosition))
            {
                ChooseNextTarget();
            }

            Vector2 nextPosition = UnicornRoamingUtility.MoveTowards(currentPosition, _targetPoint, config.MoveSpeed, Time.fixedDeltaTime);
            if (_arenaBounds != null)
            {
                nextPosition = _arenaBounds.Clamp(nextPosition);
            }

            Vector2 delta = nextPosition - currentPosition;
            _currentVelocity = delta / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            body.MovePosition(nextPosition);

            if (delta.sqrMagnitude > 0.0001f)
            {
                transform.up = delta.normalized;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDestroyPot(other);
            TryKnockbackPlayer(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDestroyPot(collision.collider);
            TryKnockbackPlayer(collision.collider);
        }

        private void CacheReferences()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            trailEmitter ??= GetComponent<RainbowTrailEmitter>();

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            trailEmitter?.Configure(this);
        }

        private void ResolveDefaults()
        {
            if (config == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                config = ProjectContext.Instance.ConfigRegistry.UnicornConfig;
            }

            trailEmitter?.Configure(this);
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
        }

        private bool ShouldRetarget(Vector2 currentPosition)
        {
            if (_retargetCooldown <= 0f)
            {
                return true;
            }

            return Vector2.Distance(currentPosition, _targetPoint) <= ResolveArrivalDistance();
        }

        private void ChooseNextTarget()
        {
            AcquireArenaBounds();

            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            Rect bounds = _arenaBounds != null
                ? _arenaBounds.WorldRect
                : new Rect(origin - new Vector2(4f, 2.5f), new Vector2(8f, 5f));

            if (_currentTargetSide.HasValue)
            {
                Vector2 wallNormal = UnicornRoamingUtility.GetWallNormal(_currentTargetSide.Value);
                float angle = Random.Range(
                    config != null ? config.BounceAngleMinDegrees : 45f,
                    config != null ? config.BounceAngleMaxDegrees : 135f);
                if (Random.value < 0.5f)
                {
                    angle = -angle;
                }

                Vector2 bounceDirection = Quaternion.Euler(0f, 0f, angle) * wallNormal;

                if (UnicornRoamingUtility.TryGetBoundaryIntersection(
                        origin, bounceDirection, bounds, _currentTargetSide,
                        out ArenaBoundarySide nextSide, out Vector2 intersection))
                {
                    _currentTargetSide = nextSide;
                    _targetPoint = intersection;
                }
                else
                {
                    _currentTargetSide = UnicornRoamingUtility.SelectNextBoundarySide(_currentTargetSide, Random.value);
                    _targetPoint = UnicornRoamingUtility.SelectBoundaryTarget(
                        bounds, _currentTargetSide.Value, Random.value,
                        config != null ? config.BoundaryCornerPadding : 1f);
                }
            }
            else
            {
                _currentTargetSide = UnicornRoamingUtility.SelectNextBoundarySide(null, Random.value);
                _targetPoint = UnicornRoamingUtility.SelectBoundaryTarget(
                    bounds, _currentTargetSide.Value, Random.value,
                    config != null ? config.BoundaryCornerPadding : 1f);
            }

            _retargetCooldown = Mathf.Max(0.1f, config != null ? config.RetargetIntervalSeconds : 1f);
        }

        private float ResolveArrivalDistance()
        {
            return config != null ? Mathf.Max(0.01f, config.TargetArrivalDistance) : 0.2f;
        }

        private void AcquireArenaBounds()
        {
            if (_arenaBounds != null)
            {
                return;
            }

            if (ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaSceneContext) == true)
            {
                _arenaBounds = arenaSceneContext.ArenaBounds;
            }

            if (_arenaBounds == null)
            {
                _arenaBounds = FindFirstObjectByType<ArenaBoundsAuthoring>(FindObjectsInactive.Exclude);
            }
        }

        private void TryDestroyPot(Collider2D candidate)
        {
            if (candidate == null)
            {
                return;
            }

            PotController pot = candidate.GetComponentInParent<PotController>();
            pot?.DestroyByUnicorn(this);
        }

        private void TryKnockbackPlayer(Collider2D candidate)
        {
            if (candidate == null || config == null)
            {
                return;
            }

            PlayerRoot player = candidate.GetComponentInParent<PlayerRoot>();
            if (player == null)
            {
                return;
            }

            Vector2 unicornPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 playerPosition = player.Body != null ? player.Body.position : (Vector2)player.transform.position;
            Vector2 knockbackDirection = playerPosition - unicornPosition;

            player.ApplyKnockbackAndDaze(knockbackDirection, config.PlayerKnockbackDistance, config.PlayerDazeDurationSeconds);
        }
    }
}