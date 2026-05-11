using UnityEngine;

namespace RainbowsEnd.Core.Common
{
    public sealed class HealthStageFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private bool useCurrentColorAsHealthy = true;
        [SerializeField] private Color healthyColor = Color.white;
        [SerializeField] private Color woundedColor = new(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color criticalColor = new(1f, 0.45f, 0.35f, 1f);
        [SerializeField] private Color depletedColor = new(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] [Range(0f, 1f)] private float woundedThreshold = 0.65f;
        [SerializeField] [Range(0f, 1f)] private float criticalThreshold = 0.3f;

        private bool _healthyColorInitialized;

        public void Configure(Health actorHealth, SpriteRenderer renderer)
        {
            health = actorHealth;
            targetRenderer = renderer;
            InitializeHealthyColor();
            RefreshVisual();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
            InitializeHealthyColor();
            RefreshVisual();
        }

        private void Awake()
        {
            CacheReferences();
            InitializeHealthyColor();
            RefreshVisual();
        }

        private void Update()
        {
            RefreshVisual();
        }

        private void CacheReferences()
        {
            health ??= GetComponent<Health>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void InitializeHealthyColor()
        {
            if (_healthyColorInitialized || targetRenderer == null || !useCurrentColorAsHealthy)
            {
                return;
            }

            healthyColor = targetRenderer.color;
            _healthyColorInitialized = true;
        }

        private void RefreshVisual()
        {
            if (health == null || targetRenderer == null)
            {
                return;
            }

            InitializeHealthyColor();

            if (health.IsDepleted)
            {
                targetRenderer.color = depletedColor;
                return;
            }

            float healthRatio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
            if (healthRatio <= criticalThreshold)
            {
                targetRenderer.color = criticalColor;
                return;
            }

            if (healthRatio <= woundedThreshold)
            {
                targetRenderer.color = woundedColor;
                return;
            }

            targetRenderer.color = healthyColor;
        }
    }
}