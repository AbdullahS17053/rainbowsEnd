using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Rigidbody2D body;

        private ArenaBoundsAuthoring _arenaBounds;

        public Vector2 MoveInput { get; private set; }

        public Vector2 CurrentVelocity => body != null ? body.linearVelocity : Vector2.zero;

        public float SpeedNormalized =>
            playerRoot?.Config == null || playerRoot.Config.MoveSpeed <= 0f
                ? 0f
                : Mathf.Clamp01(CurrentVelocity.magnitude / playerRoot.Config.MoveSpeed);

        public void Configure(PlayerRoot root, PlayerInputReader reader, Rigidbody2D rigidbody2D)
        {
            playerRoot = root;
            inputReader = reader;
            body = rigidbody2D;
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
            ConfigureBody();
        }

        [SerializeField] private LineRenderer waterRippleRenderer;
        private float _waterRippleTimer;

        private void FixedUpdate()
        {
            if (playerRoot?.Config == null || inputReader == null || body == null)
            {
                return;
            }

            MoveInput = Vector2.ClampMagnitude(inputReader.MoveInput, 1f);
            float currentMoveSpeed = playerRoot.Config.MoveSpeed;
            if (TerrainZone.IsInForest(body.position))
            {
                currentMoveSpeed *= 0.90f;
            }

            Vector2 desiredVelocity = MoveInput * currentMoveSpeed;
            body.linearVelocity = PlayerMovementUtility.MoveTowards(body.linearVelocity, desiredVelocity, playerRoot.Config.Acceleration, Time.fixedDeltaTime);

            UpdateWaterRipples();

            if (_arenaBounds == null)
            {
                ResolveArenaBounds();
            }

            if (_arenaBounds != null)
            {
                body.position = _arenaBounds.Clamp(body.position);
            }
        }

        private void UpdateWaterRipples()
        {
            bool inWater = TerrainZone.IsInWater(body.position);
            if (!inWater)
            {
                if (waterRippleRenderer != null) waterRippleRenderer.enabled = false;
                return;
            }

            EnsureWaterRippleRenderer();
            if (waterRippleRenderer == null) return;

            waterRippleRenderer.enabled = true;
            _waterRippleTimer += Time.fixedDeltaTime * 4f;
            float radius = 0.35f + Mathf.PingPong(_waterRippleTimer, 0.2f);

            int segments = 16;
            waterRippleRenderer.positionCount = segments + 1;
            Vector3 center = (Vector3)body.position;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                waterRippleRenderer.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void EnsureWaterRippleRenderer()
        {
            if (waterRippleRenderer != null) return;

            GameObject rippleObj = new GameObject("PlayerWaterRipple");
            rippleObj.transform.SetParent(transform, false);
            waterRippleRenderer = rippleObj.AddComponent<LineRenderer>();
            waterRippleRenderer.useWorldSpace = true;
            waterRippleRenderer.startWidth = 0.05f;
            waterRippleRenderer.endWidth = 0.05f;
            waterRippleRenderer.loop = true;
            waterRippleRenderer.sortingOrder = 4;
            waterRippleRenderer.material = new Material(Shader.Find("Sprites/Default"));
            Color greenWaterRipple = new Color(0.2f, 0.95f, 0.6f, 0.75f);
            waterRippleRenderer.startColor = greenWaterRipple;
            waterRippleRenderer.endColor = greenWaterRipple;
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            body ??= GetComponent<Rigidbody2D>();
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

        private void OnDisable()
        {
            _arenaBounds = null;
        }

        private void ResolveArenaBounds()
        {
            if (ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaCtx) == true)
            {
                _arenaBounds = arenaCtx.ArenaBounds;
            }

            if (_arenaBounds == null)
            {
                _arenaBounds = FindFirstObjectByType<ArenaBoundsAuthoring>(FindObjectsInactive.Exclude);
            }
        }
    }
}