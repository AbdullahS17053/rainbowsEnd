using RainbowsEnd.Audio;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.World;
using QuickOutline;
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
        [SerializeField] private LineRenderer potWarningLine;
        [SerializeField] private bool showPotWarningLine = false;
        [SerializeField] private UnicornTailTrail tailTrail;

        [Header("Audio")]
        [SerializeField] private AudioCueSO spawnCue;

        [Header("Horn Visual Juice")]
        [SerializeField] private Renderer hornRenderer;
        private MaterialPropertyBlock _hornPropBlock;
        private bool _hornSearched;

        public AudioCueSO SpawnCue => spawnCue;
        public UnicornTailTrail TailTrail => tailTrail;
        public void ConfigureSpawnCue(AudioCueSO cue) => spawnCue = cue;
        public void ConfigureHornRenderer(Renderer r) => hornRenderer = r;

        private ArenaBoundsAuthoring _arenaBounds;
        private Vector2 _targetPoint;
        private Vector2 _currentVelocity;
        private float _retargetCooldown;
        private ArenaBoundarySide? _currentTargetSide;
        private Vector2[] _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;
        private float _stuckTimer;
        private Vector2 _lastPosition;

        private bool _isGrazing;
        private float _grazeTimer;
        private float _faeSlowTimer;
        private float _faeSlowMultiplier = 0.5f;
        private float _hoofStepTimer;
        private int _hoofColorIndex;
        private static readonly Color[] HoofColors = new Color[]
        {
            new Color(1f, 0.25f, 0.35f, 0.8f),
            new Color(0.2f, 0.9f, 0.4f, 0.8f),
            new Color(0.2f, 0.6f, 1f, 0.8f),
            new Color(1f, 0.85f, 0.2f, 0.8f)
        };

        public FactionId FactionId => FactionId.Unicorn;

        public UnicornConfig Config => config;

        public Vector2 CurrentVelocity => _currentVelocity;

        public Vector2 CurrentTarget => _targetPoint;

        public void Configure(
            UnicornConfig unicornConfig,
            Rigidbody2D rigidbody2D,
            Collider2D collider2D,
            SpriteRenderer renderer,
            RainbowTrailEmitter emitter,
            UnicornTailTrail trail = null)
        {
            config = unicornConfig;
            body = rigidbody2D;
            bodyCollider = collider2D;
            visualRenderer = renderer;
            trailEmitter = emitter;
            trailEmitter?.Configure(this);
            if (trail != null)
            {
                tailTrail = trail;
            }
            ConfigureBody();
            ConfigureOutline();
        }

        public void NotifyTeleported()
        {
            trailEmitter?.NotifyUnicornTeleported();
            tailTrail?.ClearTrail();
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
            ConfigureOutline();
            ChooseNextTarget();
            if (spawnCue != null)
            {
                AudioManager.Instance?.PlayCue(spawnCue, transform.position);
            }
        }

        private void FixedUpdate()
        {
            if (config == null || body == null)
            {
                HidePotWarning();
                return;
            }

            if (_isGrazing)
            {
                _grazeTimer -= Time.fixedDeltaTime;
                if (_grazeTimer <= 0f)
                {
                    _isGrazing = false;
                }
                _currentVelocity = Vector2.zero;
                HidePotWarning();
                return;
            }

            AcquireArenaBounds();
            _retargetCooldown = Mathf.Max(0f, _retargetCooldown - Time.fixedDeltaTime);

            Vector2 currentPosition = body.position;
            if (ShouldRetarget(currentPosition))
            {
                ChooseNextTarget();
            }

            float currentSpeed = config.MoveSpeed;
            if (_faeSlowTimer > 0f)
            {
                _faeSlowTimer -= Time.fixedDeltaTime;
                currentSpeed *= _faeSlowMultiplier;
            }

            Vector2 toTarget = _targetPoint - currentPosition;
            Vector2 moveDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;

            Vector2 nextPosition = currentPosition + moveDir * (currentSpeed * Time.fixedDeltaTime);
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

            // Detect if stuck against an obstacle
            if (Vector2.Distance(currentPosition, _lastPosition) < 0.04f * currentSpeed * Time.fixedDeltaTime)
            {
                _stuckTimer += Time.fixedDeltaTime;
                if (_stuckTimer >= 1.2f)
                {
                    _stuckTimer = 0f;
                    ChooseNextTarget();
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
            _lastPosition = currentPosition;

            UpdatePotWarning();
            UpdateHoofSparklesNearPlayer(currentPosition);
        }

        public void TriggerGraze(float durationSeconds = 3.0f)
        {
            _isGrazing = true;
            _grazeTimer = durationSeconds;
            _currentVelocity = Vector2.zero;
            HidePotWarning();
        }

        public void ApplyFaeBoltSlow(float duration = 2.0f, float slowMultiplier = 0.5f)
        {
            _faeSlowTimer = Mathf.Max(_faeSlowTimer, duration);
            _faeSlowMultiplier = slowMultiplier;
        }

        private void UpdateHoofSparklesNearPlayer(Vector2 unicornPosition)
        {
            PlayerRoot player = FindFirstObjectByType<PlayerRoot>();
            if (player == null) return;

            Vector2 playerPos = player.Body != null ? player.Body.position : (Vector2)player.transform.position;
            if (Vector2.Distance(unicornPosition, playerPos) > 4.5f) return;

            _hoofStepTimer += Time.fixedDeltaTime;
            if (_hoofStepTimer >= 0.22f)
            {
                _hoofStepTimer = 0f;
                SpawnHoofCircle(unicornPosition, HoofColors[_hoofColorIndex % HoofColors.Length]);
                _hoofColorIndex++;
            }
        }

        private void SpawnHoofCircle(Vector2 position, Color color)
        {
            GameObject hoofObj = new GameObject("HoofCircle");
            hoofObj.transform.position = position;

            LineRenderer lr = hoofObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.01f;
            lr.loop = true;
            lr.sortingOrder = 3;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;

            int segments = 12;
            lr.positionCount = segments + 1;
            float r = 0.18f;
            Vector3 center = position;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
            }

            Destroy(hoofObj, 0.75f);
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
            potWarningLine ??= GetComponent<LineRenderer>();
            tailTrail ??= GetComponent<UnicornTailTrail>();
            if (tailTrail == null)
            {
                tailTrail = GetComponentInChildren<UnicornTailTrail>();
            }

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
            if (bodyCollider != null)
            {
                bodyCollider.isTrigger = true;
            }
        }

        private void ConfigureOutline()
        {
            Outline outline = GetComponent<Outline>();
            if (outline == null && visualRenderer != null)
            {
                outline = visualRenderer.GetComponentInParent<Outline>();
            }
            if (outline == null)
            {
                Transform vRoot = visualRenderer != null ? visualRenderer.transform : transform;
                outline = vRoot.gameObject.AddComponent<Outline>();
            }
            if (outline != null)
            {
                outline.OutlineMode = Outline.Mode.OutlineAll;
                outline.OutlineColor = new Color(1f, 0.45f, 0.95f, 1f); // Vibrant magenta/pink
                outline.OutlineWidth = 2f;
            }
        }

        private bool ShouldRetarget(Vector2 currentPosition)
        {
            if (_retargetCooldown <= 0f)
            {
                return true;
            }

            if (Vector2.Distance(currentPosition, _targetPoint) <= ResolveArrivalDistance())
            {
                return true;
            }

            if (_currentPath != null && _currentWaypointIndex >= _currentPath.Length - 1 &&
                Vector2.Distance(currentPosition, _targetPoint) <= ResolveArrivalDistance() * 2f)
            {
                return true;
            }

            return false;
        }

        private void ChooseNextTarget()
        {
            AcquireArenaBounds();

            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            Rect bounds = _arenaBounds != null
                ? _arenaBounds.WorldRect
                : new Rect(origin - new Vector2(4f, 2.5f), new Vector2(8f, 5f));

            bool pathFound = false;

            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 candidate;
                if (_currentTargetSide.HasValue && attempt == 0)
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
                        candidate = intersection;
                    }
                    else
                    {
                        _currentTargetSide = UnicornRoamingUtility.SelectNextBoundarySide(_currentTargetSide, Random.value);
                        candidate = UnicornRoamingUtility.SelectBoundaryTarget(
                            bounds, _currentTargetSide.Value, Random.value,
                            config != null ? config.BoundaryCornerPadding : 1f);
                    }
                }
                else
                {
                    _currentTargetSide = UnicornRoamingUtility.SelectNextBoundarySide(_currentTargetSide, Random.value);
                    candidate = UnicornRoamingUtility.SelectBoundaryTarget(
                        bounds, _currentTargetSide.Value, Random.value,
                        config != null ? config.BoundaryCornerPadding : 1f);
                }

                _targetPoint = candidate;
                _currentPath = null;
                _currentWaypointIndex = 0;
                break;
            }

            _retargetCooldown = Mathf.Max(0.1f, config != null ? config.RetargetIntervalSeconds : 1f);
            _stuckTimer = 0f;
            _lastPosition = origin;
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

        private void UpdatePotWarning()
        {
            if (potWarningLine == null)
            {
                return;
            }

            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            Vector2 delta = _targetPoint - origin;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                HidePotWarning();
                return;
            }

            Vector2 direction = delta.normalized;
            float maxDistance = delta.magnitude;
            float nearestDistance = maxDistance;
            float unicornRadius = bodyCollider != null
                ? Mathf.Max(bodyCollider.bounds.extents.x, bodyCollider.bounds.extents.y)
                : 0.4f;
            bool foundThreat = false;

            PotController[] pots = FindObjectsOfType<PotController>(true);
            for (int index = 0; index < pots.Length; index++)
            {
                PotController pot = pots[index];
                if (pot == null || pot.IsDestroyed || !pot.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float impactDistance;
                if (!TryGetPotImpactDistance(pot, origin, direction, maxDistance, unicornRadius, out impactDistance))
                {
                    continue;
                }

                if (impactDistance < nearestDistance)
                {
                    nearestDistance = impactDistance;
                    foundThreat = true;
                }
            }

            if (!foundThreat)
            {
                HidePotWarning();
                return;
            }

            potWarningLine.positionCount = 2;
            potWarningLine.SetPosition(0, origin);
            potWarningLine.SetPosition(1, origin + direction * nearestDistance);
            Color purplePulse = Color.Lerp(
                new Color(0.65f, 0.1f, 0.95f, 0.95f),
                new Color(1f, 0.35f, 1f, 1f),
                Mathf.PingPong(Time.time * 8f, 1f));
            potWarningLine.startColor = purplePulse;
            potWarningLine.endColor = purplePulse;
            potWarningLine.enabled = showPotWarningLine;
            SetHornColor(purplePulse);
        }

        private bool TryGetPotImpactDistance(
            PotController pot,
            Vector2 origin,
            Vector2 direction,
            float maxDistance,
            float unicornRadius,
            out float impactDistance)
        {
            impactDistance = 0f;
            Collider2D potCollider = pot.GetComponent<Collider2D>();
            if (potCollider == null)
            {
                return false;
            }

            Vector2 potCenter;
            float potRadius;
            CircleCollider2D circle = potCollider as CircleCollider2D;
            if (circle != null)
            {
                float scale = Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y));
                potCenter = circle.transform.TransformPoint(circle.offset);
                potRadius = circle.radius * scale;
            }
            else
            {
                if (!potCollider.enabled)
                {
                    return false;
                }

                Bounds bounds = potCollider.bounds;
                potCenter = bounds.center;
                potRadius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            }

            float combinedRadius = potRadius + unicornRadius;
            float projectedDistance = Vector2.Dot(potCenter - origin, direction);
            if (projectedDistance < -combinedRadius || projectedDistance > maxDistance + combinedRadius)
            {
                return false;
            }

            Vector2 closestPoint = origin + direction * Mathf.Clamp(projectedDistance, 0f, maxDistance);
            float lateralDistance = Vector2.Distance(potCenter, closestPoint);
            if (lateralDistance > combinedRadius)
            {
                return false;
            }

            float alongLine = Mathf.Sqrt(Mathf.Max(0f, combinedRadius * combinedRadius - lateralDistance * lateralDistance));
            impactDistance = Mathf.Max(0f, projectedDistance - alongLine);
            return impactDistance <= maxDistance;
        }

        private void HidePotWarning()
        {
            if (potWarningLine != null)
            {
                potWarningLine.enabled = false;
            }
            SetHornColor(Color.white);
        }

        private void EnsureHornVisual()
        {
            if (hornRenderer != null || _hornSearched) return;
            _hornSearched = true;

            Transform hornTr = transform.Find("Horn") ?? transform.Find("VisualRoot/Horn");
            if (hornTr == null)
            {
                Transform[] transforms = GetComponentsInChildren<Transform>(true);
                Transform headBone = null;
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                    {
                        headBone = transforms[i];
                        break;
                    }
                }

                Transform parent = headBone != null ? headBone : (visualRenderer != null ? visualRenderer.transform : transform);
                GameObject hornObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hornObj.name = "Horn";
                hornObj.transform.SetParent(parent, false);
                hornObj.transform.localPosition = new Vector3(0f, 0.45f, 0.25f);
                hornObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                hornObj.transform.localScale = new Vector3(0.06f, 0.25f, 0.06f);

                Collider col = hornObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                hornRenderer = hornObj.GetComponent<Renderer>();
                if (hornRenderer != null)
                {
                    hornRenderer.material.color = Color.white;
                }
            }
            else
            {
                hornRenderer = hornTr.GetComponent<Renderer>();
            }
        }

        private void SetHornColor(Color color)
        {
            EnsureHornVisual();
            if (hornRenderer == null) return;

            _hornPropBlock ??= new MaterialPropertyBlock();
            hornRenderer.GetPropertyBlock(_hornPropBlock);
            _hornPropBlock.SetColor("_BaseColor", color);
            _hornPropBlock.SetColor("_Color", color);
            hornRenderer.SetPropertyBlock(_hornPropBlock);
        }

        private void TryDestroyPot(Collider2D candidate)
        {
            if (candidate == null)
            {
                return;
            }

            PotController pot = candidate.GetComponentInParent<PotController>();
            if (pot == null)
            {
                PlayerRoot player = candidate.GetComponentInParent<PlayerRoot>();
                pot = player != null ? player.PotCarryController?.CarriedPot : null;
            }

            pot?.DestroyByUnicorn(this);
        }

        private void TryKnockbackPlayer(Collider2D candidate)
        {
            if (candidate == null || config == null)
            {
                return;
            }

            PlayerRoot player = candidate.GetComponentInParent<PlayerRoot>();
            if (player == null || player.IsCarryingPot)
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