using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Audio
{
    public sealed class AudioHearingRadiusService : MonoBehaviour
    {
        private static AudioHearingRadiusService _instance;

        [Header("Top-Down Hearing Settings")]
        [SerializeField] private float minHearingRadius = 16.0f;
        [SerializeField] private float maxHearingRadius = 36.0f;
        [SerializeField] private float falloffMargin = 12.0f;

        private PlayerRoot _player;

        public static AudioHearingRadiusService Instance => _instance;

        public float CurrentHearingRadius { get; private set; }

        public Vector2 ListenerPosition => _player != null ? (Vector2)_player.transform.position : (Vector2)transform.position;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            CurrentHearingRadius = minHearingRadius;
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
                speedNormalized = _player.MovementController.SpeedNormalized;
            }

            CurrentHearingRadius = Mathf.Lerp(minHearingRadius, maxHearingRadius, speedNormalized);
        }

        public bool IsWithinEarshot(Vector2 worldPosition, out float volumeScale)
        {
            Vector2 listenerPos = ListenerPosition;
            float distance = Vector2.Distance(listenerPos, worldPosition);

            if (distance <= CurrentHearingRadius)
            {
                float normalizedDist = distance / Mathf.Max(CurrentHearingRadius, 0.01f);
                volumeScale = Mathf.Clamp(1f - (normalizedDist * 0.35f), 0.65f, 1f);
                return true;
            }

            if (distance <= CurrentHearingRadius + falloffMargin)
            {
                float falloffT = (distance - CurrentHearingRadius) / Mathf.Max(falloffMargin, 0.01f);
                volumeScale = Mathf.Lerp(0.65f, 0.35f, falloffT);
                return true;
            }

            // Outside margin: subtle audible presence across the arena
            volumeScale = 0.35f;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
            Gizmos.DrawWireSphere(ListenerPosition, CurrentHearingRadius);
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.15f);
            Gizmos.DrawWireSphere(ListenerPosition, CurrentHearingRadius + falloffMargin);
        }
    }
}
