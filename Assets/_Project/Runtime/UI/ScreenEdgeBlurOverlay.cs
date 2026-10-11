using UnityEngine;
using UnityEngine.UI;

namespace RainbowsEnd.UI
{
    /// <summary>
    /// Renders soft gradient panels along all four screen edges to create
    /// a vignette / frosted-glass effect.  Lives on its own Canvas that sits
    /// behind interactive UI and never intercepts raycasts.
    ///
    /// Auto-instantiates itself when any scene loads — no manual setup needed.
    /// </summary>
    public sealed class ScreenEdgeBlurOverlay : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Color edgeColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField, Range(0.02f, 0.35f)] private float edgeThickness = 0.12f;

        [Header("Canvas")]
        [SerializeField, Tooltip("Sort order for this overlay canvas.  Keep it lower than the main UI canvas.")]
        private int sortOrder = -10;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RawImage _flashImage;
        private readonly System.Collections.Generic.List<RawImage> _edgeImages = new System.Collections.Generic.List<RawImage>();
        private Coroutine _flashCoroutine;
        private Coroutine _glowCoroutine;

        /// <summary>
        /// Singleton so only one overlay exists at a time.
        /// </summary>
        public static ScreenEdgeBlurOverlay Instance { get; private set; }

        // ───────────────────────── Auto-bootstrap ─────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject go = new GameObject("[ScreenEdgeBlurOverlay]");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenEdgeBlurOverlay>();
        }

        // ──────────────────────────── Lifecycle ───────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildOverlay();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ──────────────────────── Overlay creation ────────────────────────
        private void BuildOverlay()
        {
            // --- Canvas (Screen-Space Overlay, behind interactive UI) ---
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>(); // required by Canvas

            // Block nothing – purely visual
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            // --- Edge panels ---
            CreateEdgePanel("LeftEdge",   Edge.Left);
            CreateEdgePanel("RightEdge",  Edge.Right);
            CreateEdgePanel("TopEdge",    Edge.Top);
            CreateEdgePanel("BottomEdge", Edge.Bottom);

            // --- Fullscreen Flash Overlay ---
            GameObject flashGo = new GameObject("FlashOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            flashGo.transform.SetParent(transform, false);
            RectTransform flashRt = flashGo.GetComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
            _flashImage = flashGo.GetComponent<RawImage>();
            _flashImage.color = Color.clear;
            _flashImage.raycastTarget = false;
        }

        // ─────────────────────── Per-edge helpers ─────────────────────────
        private enum Edge { Left, Right, Top, Bottom }

        private void CreateEdgePanel(string panelName, Edge edge)
        {
            GameObject panelGo = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            panelGo.transform.SetParent(transform, false);

            RectTransform rt = panelGo.GetComponent<RectTransform>();

            // Create a gradient texture (opaque → transparent)
            Texture2D gradientTex = CreateGradientTexture(edge);

            RawImage img = panelGo.GetComponent<RawImage>();
            img.texture = gradientTex;
            img.color = Color.white; // tint is baked into the texture
            img.raycastTarget = false;
            _edgeImages.Add(img);

            // Position and stretch the rect
            switch (edge)
            {
                case Edge.Left:
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(edgeThickness, 1f);
                    break;
                case Edge.Right:
                    rt.anchorMin = new Vector2(1f - edgeThickness, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    break;
                case Edge.Top:
                    rt.anchorMin = new Vector2(0f, 1f - edgeThickness);
                    rt.anchorMax = new Vector2(1f, 1f);
                    break;
                case Edge.Bottom:
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, edgeThickness);
                    break;
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Texture2D CreateGradientTexture(Edge edge)
        {
            // Small 1-D gradient: 32 pixels is enough for a smooth ramp
            const int size = 32;
            bool horizontal = (edge == Edge.Left || edge == Edge.Right);
            int w = horizontal ? size : 1;
            int h = horizontal ? 1 : size;

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[w * h];
            for (int i = 0; i < size; i++)
            {
                // t goes from 0 (edge of screen) to 1 (inward)
                float t = (float)i / (size - 1);

                // Smoothstep fade-out
                float alpha = edgeColor.a * (1f - Mathf.SmoothStep(0f, 1f, t));
                Color c = new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha);

                // For Right and Top the gradient is reversed in UV space
                bool invert = (edge == Edge.Right || edge == Edge.Top);
                int idx = invert ? (size - 1 - i) : i;
                pixels[idx] = c;
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true); // make non-readable to save memory
            return tex;
        }

        // ────────────────────────── Public API ────────────────────────────
        /// <summary>
        /// Set the overall opacity of the edge overlay at runtime (0 = invisible, 1 = full).
        /// </summary>
        public void SetOverlayAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        public void TriggerGoldFlash(float duration = 0.28f)
        {
            TriggerFlash(new Color(1f, 0.84f, 0.15f, 0.35f), duration);
        }

        public void TriggerFlash(Color flashColor, float duration = 0.25f)
        {
            if (_flashImage == null) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(DoFlash(flashColor, duration));
        }

        private System.Collections.IEnumerator DoFlash(Color flashColor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Color c = flashColor;
                c.a = Mathf.Lerp(flashColor.a, 0f, t);
                _flashImage.color = c;
                yield return null;
            }
            _flashImage.color = Color.clear;
            _flashCoroutine = null;
        }

        public void TriggerEdgeGlowPulse(Color glowColor, float duration = 0.55f)
        {
            if (_edgeImages.Count == 0) return;
            if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
            _glowCoroutine = StartCoroutine(DoEdgeGlow(glowColor, duration));
        }

        private System.Collections.IEnumerator DoEdgeGlow(Color glowColor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                Color c = Color.Lerp(Color.white, glowColor, pulse);
                for (int i = 0; i < _edgeImages.Count; i++)
                {
                    if (_edgeImages[i] != null)
                    {
                        _edgeImages[i].color = c;
                    }
                }
                yield return null;
            }

            for (int i = 0; i < _edgeImages.Count; i++)
            {
                if (_edgeImages[i] != null)
                {
                    _edgeImages[i].color = Color.white;
                }
            }
            _glowCoroutine = null;
        }
    }
}
