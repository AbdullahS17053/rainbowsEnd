using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class FloatingNumberSpawner : MonoBehaviour
    {
        private static FloatingNumberSpawner _instance;

        [SerializeField] private Color positiveGoldColor = new Color(1f, 0.84f, 0f, 1f); // Gold
        [SerializeField] private Color negativeGoldColor = new Color(0.95f, 0.3f, 0.2f, 1f); // Red
        [SerializeField] private Color killstreakColor = new Color(0.3f, 0.95f, 0.4f, 1f); // Vibrant green
        [SerializeField] private float defaultDuration = 1.2f;

        private sealed class ActiveFloatingText
        {
            public GameObject GameObject;
            public TextMeshPro TextComponent;
            public Vector3 StartPosition;
            public float Elapsed;
            public float Duration;
            public float ScaleMultiplier;
            public Color BaseColor;
        }

        private readonly List<ActiveFloatingText> _activeItems = new List<ActiveFloatingText>();
        private readonly Queue<ActiveFloatingText> _pool = new Queue<ActiveFloatingText>();

        public static FloatingNumberSpawner Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        public void SpawnGoldNumber(Vector3 worldPosition, int amount, bool isGain = true)
        {
            string sign = isGain ? "+" : "-";
            string text = $"{sign}{Mathf.Abs(amount)}g";
            Color color = isGain ? positiveGoldColor : negativeGoldColor;
            Spawn(worldPosition, text, color, scale: 1.0f);
        }

        public void SpawnKillstreakCallout(Vector3 worldPosition, string title, float scale = 1.3f)
        {
            Spawn(worldPosition + new Vector3(0f, 0.6f, 0f), title, killstreakColor, scale, duration: 1.8f);
        }

        public void Spawn(Vector3 worldPosition, string text, Color color, float scale = 1.0f, float duration = -1f)
        {
            ActiveFloatingText item = GetOrCreateItem();
            item.GameObject.transform.position = worldPosition;
            item.StartPosition = worldPosition;
            item.TextComponent.text = text;
            item.TextComponent.color = color;
            item.BaseColor = color;
            item.Elapsed = 0f;
            item.Duration = duration > 0f ? duration : defaultDuration;
            item.ScaleMultiplier = scale;
            item.GameObject.transform.localScale = Vector3.one * (scale * 0.5f);
            item.GameObject.SetActive(true);

            _activeItems.Add(item);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                ActiveFloatingText item = _activeItems[i];
                item.Elapsed += dt;
                float t = Mathf.Clamp01(item.Elapsed / item.Duration);

                // Float upward
                Vector3 currentPos = item.StartPosition + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 1.2f, 0f);
                item.GameObject.transform.position = currentPos;

                // Scale pop then settle
                float scalePop = t < 0.2f
                    ? Mathf.Lerp(item.ScaleMultiplier * 0.5f, item.ScaleMultiplier * 1.3f, t / 0.2f)
                    : Mathf.Lerp(item.ScaleMultiplier * 1.3f, item.ScaleMultiplier, (t - 0.2f) / 0.8f);
                item.GameObject.transform.localScale = Vector3.one * scalePop;

                // Fade alpha in the second half
                float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                Color c = item.BaseColor;
                c.a = alpha;
                item.TextComponent.color = c;

                if (item.Elapsed >= item.Duration)
                {
                    item.GameObject.SetActive(false);
                    _activeItems.RemoveAt(i);
                    _pool.Enqueue(item);
                }
            }
        }

        private ActiveFloatingText GetOrCreateItem()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            GameObject obj = new GameObject("FloatingTextItem");
            obj.transform.SetParent(transform);
            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4.5f;
            tmp.sortingOrder = 50;

            return new ActiveFloatingText
            {
                GameObject = obj,
                TextComponent = tmp
            };
        }
    }
}
