using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Rigidbody2D body;

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

        private void FixedUpdate()
        {
            if (playerRoot?.Config == null || inputReader == null || body == null)
            {
                return;
            }

            MoveInput = Vector2.ClampMagnitude(inputReader.MoveInput, 1f);
            Vector2 desiredVelocity = MoveInput * playerRoot.Config.MoveSpeed;
            body.linearVelocity = PlayerMovementUtility.MoveTowards(body.linearVelocity, desiredVelocity, playerRoot.Config.Acceleration, Time.fixedDeltaTime);
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
    }
}