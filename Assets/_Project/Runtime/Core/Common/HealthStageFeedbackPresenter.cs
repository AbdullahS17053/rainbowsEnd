using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] [Range(0f, 1f)] private float criticalThreshold = 0.33f;

        [Header("Damage Health Flash")]
        [SerializeField] private bool flashOnDamage = true;
        [SerializeField] [Min(0.05f)] private float flashDurationSeconds = 0.15f;
        [SerializeField] private Color healthyFlashColor = new(0.2f, 1f, 0.25f, 1f); // Green (> 65%)
        [SerializeField] private Color woundedFlashColor = new(1f, 0.9f, 0.15f, 1f); // Yellow (33% - 65%)
        [SerializeField] private Color criticalFlashColor = new(1f, 0.25f, 0.2f, 1f); // Red (<= 33%)

        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private bool _healthyColorInitialized;
        private Renderer[] _cachedRenderers = new Renderer[0];
        private MaterialPropertyBlock _flashPropertyBlock;
        private Coroutine _flashCoroutine;
        private bool _isFlashing;

        public bool IsFlashing => _isFlashing;
        public Color CurrentFlashColor { get; private set; }

        public void Configure(Health actorHealth, SpriteRenderer renderer)
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }

            health = actorHealth;
            targetRenderer = renderer;
            CacheReferences();

            if (enabled && health != null)
            {
                health.Damaged += HandleDamaged;
            }

            InitializeHealthyColor();
            RefreshVisual();
        }

        public void Configure(Health actorHealth, Renderer[] renderers)
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }

            health = actorHealth;
            if (renderers != null && renderers.Length > 0)
            {
                _cachedRenderers = renderers;
            }
            CacheReferences();

            if (enabled && health != null)
            {
                health.Damaged += HandleDamaged;
            }

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
            _flashPropertyBlock ??= new MaterialPropertyBlock();
            CacheReferences();
            InitializeHealthyColor();
            RefreshVisual();
        }

        private void OnEnable()
        {
            CacheReferences();
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Damaged += HandleDamaged;
            }
            RefreshVisual();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }

            StopFlashVisuals();
        }

        private void Update()
        {
            if (!_isFlashing)
            {
                RefreshVisual();
            }
        }

        public void TriggerHitFlash(Color color, float duration = -1f)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            float dur = duration > 0f ? duration : flashDurationSeconds;
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(DoFlashRoutine(color, dur));
        }

        public Color ResolveHealthFlashColor()
        {
            if (health == null)
            {
                return healthyFlashColor;
            }

            float healthRatio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
            if (healthRatio <= criticalThreshold)
            {
                return criticalFlashColor;
            }

            if (healthRatio <= woundedThreshold)
            {
                return woundedFlashColor;
            }

            return healthyFlashColor;
        }

        private void HandleDamaged(DamageResult result)
        {
            if (!flashOnDamage || result.AppliedAmount <= 0f)
            {
                return;
            }

            Color flashColor = ResolveHealthFlashColor();
            TriggerHitFlash(flashColor);
        }

        private IEnumerator DoFlashRoutine(Color flashColor, float duration)
        {
            _isFlashing = true;
            CurrentFlashColor = flashColor;
            ApplyRendererColor(flashColor);

            yield return new WaitForSeconds(duration);

            ClearRendererColor();
            _isFlashing = false;
            _flashCoroutine = null;
            RefreshVisual();
        }

        private void StopFlashVisuals()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            _isFlashing = false;
            ClearRendererColor();
        }

        private void ApplyRendererColor(Color color)
        {
            _flashPropertyBlock ??= new MaterialPropertyBlock();
            _flashPropertyBlock.SetColor(BaseColorPropertyId, color);
            _flashPropertyBlock.SetColor(ColorPropertyId, color);

            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                Renderer r = _cachedRenderers[i];
                if (r != null)
                {
                    r.SetPropertyBlock(_flashPropertyBlock);
                }
            }

            if (targetRenderer != null)
            {
                targetRenderer.color = color;
            }
        }

        private void ClearRendererColor()
        {
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                Renderer r = _cachedRenderers[i];
                if (r != null)
                {
                    r.SetPropertyBlock(null);
                }
            }

            if (targetRenderer != null && _healthyColorInitialized)
            {
                targetRenderer.color = healthyColor;
            }
        }

        private void CacheReferences()
        {
            health ??= GetComponent<Health>() ?? GetComponentInParent<Health>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_cachedRenderers == null || _cachedRenderers.Length == 0)
            {
                // Gather all 3D mesh and skinned mesh renderers as well as sprite renderers
                _cachedRenderers = GetComponentsInChildren<Renderer>(true);
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