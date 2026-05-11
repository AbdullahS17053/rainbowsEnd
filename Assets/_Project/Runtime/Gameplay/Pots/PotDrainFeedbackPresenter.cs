using UnityEngine;

namespace RainbowsEnd.Gameplay.Pots
{
    public sealed class PotDrainFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer alertRenderer;
        [SerializeField] private Color alertColor = new Color(0.96f, 0.28f, 0.18f, 0.95f);
        [SerializeField] [Min(0.1f)] private float pulseSpeed = 7f;

        private Vector3 _baseScale = Vector3.one;
        private bool _isVisible;

        public void Configure(SpriteRenderer renderer)
        {
            alertRenderer = renderer;
            CacheReferences();
            ApplyImmediateState();
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            ApplyImmediateState();
        }

        private void Reset()
        {
            CacheReferences();
            ApplyImmediateState();
        }

        private void OnValidate()
        {
            CacheReferences();
            ApplyImmediateState();
        }

        private void Awake()
        {
            CacheReferences();
            ApplyImmediateState();
        }

        private void Update()
        {
            if (!_isVisible || alertRenderer == null)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            Color color = alertColor;
            color.a *= Mathf.Lerp(0.35f, 1f, pulse);
            alertRenderer.color = color;
            transform.localScale = _baseScale * Mathf.Lerp(0.9f, 1.15f, pulse);
        }

        private void CacheReferences()
        {
            alertRenderer ??= GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
        }

        private void ApplyImmediateState()
        {
            if (alertRenderer == null)
            {
                return;
            }

            alertRenderer.enabled = _isVisible;
            alertRenderer.color = alertColor;
            transform.localScale = _baseScale;
        }
    }
}