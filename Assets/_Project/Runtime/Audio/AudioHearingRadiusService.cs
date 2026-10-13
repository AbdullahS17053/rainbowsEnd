using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Audio
{
    public sealed class AudioHearingRadiusService : MonoBehaviour
    {
        private static AudioHearingRadiusService _instance;

        [Header("Top-Down Dynamic Hearing Settings (Player Widths)")]
        [SerializeField] private float minHearingRadiusWidths = 2.5f;
        [SerializeField] private float maxHearingRadiusWidths = 7.0f;
        [SerializeField] private float falloffMarginWidths = 1.5f;

        private PlayerRoot _player;

        public static AudioHearingRadiusService Instance => _instance;

        public float CurrentHearingRadius { get; private set; }
        public float FalloffMargin => falloffMarginWidths * ResolvePlayerWidth();

        public Vector2 ListenerPosition => _player != null ? (Vector2)_player.transform.position : (Vector2)transform.position;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            CurrentHearingRadius = maxHearingRadiusWidths * ResolvePlayerWidth();
        }

        private float ResolvePlayerWidth()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerRoot>();
            }

            if (_player != null && _player.GetComponent<Collider2D>() != null)
            {
                return Mathf.Max(0.5f, _player.GetComponent<Collider2D>().bounds.size.x);
            }

            return 0.8f;
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerRoot>();
            }

            float speedNormalized = 0f;
            if (_player != null && _player.MovementController != null)
            {
                speedNormalized = Mathf.Clamp01(_player.MovementController.SpeedNormalized);
            }

            float playerWidth = ResolvePlayerWidth();
            float minRadius = minHearingRadiusWidths * playerWidth;
            float maxRadius = maxHearingRadiusWidths * playerWidth;

            // Stationary expands to maximum, moving shrinks to minimum
            CurrentHearingRadius = Mathf.Lerp(maxRadius, minRadius, speedNormalized);
        }

        public bool IsWithinEarshot(Vector2 worldPosition, out float volumeScale)
        {
            Vector2 listenerPos = ListenerPosition;
            float distance = Vector2.Distance(listenerPos, worldPosition);
            float margin = FalloffMargin;

            if (distance <= CurrentHearingRadius)
            {
                float normalizedDist = distance / Mathf.Max(CurrentHearingRadius, 0.01f);
                volumeScale = Mathf.Clamp(1f - (normalizedDist * 0.2f), 0.8f, 1f);
                return true;
            }

            if (distance <= CurrentHearingRadius + margin)
            {
                float falloffT = (distance - CurrentHearingRadius) / Mathf.Max(margin, 0.01f);
                volumeScale = Mathf.Lerp(0.8f, 0.0f, falloffT);
                return true;
            }

            // Outside the radius: sounds simply do not play
            volumeScale = 0f;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
            Gizmos.DrawWireSphere(ListenerPosition, CurrentHearingRadius);
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.15f);
            Gizmos.DrawWireSphere(ListenerPosition, CurrentHearingRadius + FalloffMargin);
        }
    }
}
