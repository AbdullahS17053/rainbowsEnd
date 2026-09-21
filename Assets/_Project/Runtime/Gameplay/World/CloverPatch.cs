using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CloverPatch : MonoBehaviour
    {
        [SerializeField] private float lifetimeSeconds = 20.0f;
        [SerializeField] private float grazeDuration = 3.0f;
        [SerializeField] private CircleCollider2D triggerCollider;
        [SerializeField] private SpriteRenderer cloverRenderer;

        public static CloverPatch Spawn(Vector2 position)
        {
            GameObject cloverObj = new GameObject("CloverPatch");
            cloverObj.transform.position = position;

            CloverPatch patch = cloverObj.AddComponent<CloverPatch>();
            CircleCollider2D col = cloverObj.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.0f;
            patch.triggerCollider = col;

            GameObject visualObj = new GameObject("CloverVisual");
            visualObj.transform.SetParent(cloverObj.transform, false);
            SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;

            // Lush green procedural clover icon
            Texture2D tex = new Texture2D(16, 16);
            Color cloverGreen = new Color(0.15f, 0.85f, 0.3f, 0.9f);
            Color[] cols = new Color[16 * 16];
            for (int i = 0; i < cols.Length; i++) cols[i] = cloverGreen;
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            visualObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            patch.cloverRenderer = sr;
            return patch;
        }

        private void Awake()
        {
            triggerCollider ??= GetComponent<CircleCollider2D>();
            Destroy(gameObject, lifetimeSeconds);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            UnicornController unicorn = other.GetComponentInParent<UnicornController>();
            if (unicorn != null)
            {
                unicorn.TriggerGraze(grazeDuration);
                Destroy(gameObject);
            }
        }
    }
}
