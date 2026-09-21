using System.Collections;
using RainbowsEnd.Core.Common;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PlayerDecoy : MonoBehaviour, ITeamAffiliation
    {
        private static PlayerDecoy _activeDecoy;

        [SerializeField] private float lifetimeSeconds = 3.0f;
        [SerializeField] private Color smokeColor = new Color(0.2f, 0.9f, 0.3f, 0.85f);
        [SerializeField] private SpriteRenderer decoyRenderer;

        public static PlayerDecoy ActiveDecoy => _activeDecoy;

        public FactionId FactionId => FactionId.Player;

        public static PlayerDecoy Spawn(Vector2 position, Sprite playerSprite = null)
        {
            if (_activeDecoy != null)
            {
                _activeDecoy.Dismiss();
            }

            GameObject decoyObj = new GameObject("Downed_Decoy");
            decoyObj.transform.position = position;

            PlayerDecoy decoy = decoyObj.AddComponent<PlayerDecoy>();
            CircleCollider2D col = decoyObj.GetComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = false;

            GameObject visualObj = new GameObject("DecoyVisual");
            visualObj.transform.SetParent(decoyObj.transform, false);
            SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            if (playerSprite != null)
            {
                sr.sprite = playerSprite;
                sr.color = new Color(0.6f, 1f, 0.6f, 0.8f);
            }
            else
            {
                // Fallback procedural dummy visual if no sprite
                Texture2D tex = new Texture2D(16, 16);
                Color[] cols = new Color[16 * 16];
                for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0.3f, 0.85f, 0.4f, 0.8f);
                tex.SetPixels(cols);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            }

            decoy.decoyRenderer = sr;
            return decoy;
        }

        private void Awake()
        {
            _activeDecoy = this;
            SpawnSmokeBomb(transform.position);
            StartCoroutine(LifetimeRoutine());
        }

        private void OnDestroy()
        {
            if (_activeDecoy == this)
            {
                _activeDecoy = null;
            }
        }

        public void Dismiss()
        {
            StopAllCoroutines();
            SpawnSmokeBomb(transform.position);
            if (_activeDecoy == this)
            {
                _activeDecoy = null;
            }
            Destroy(gameObject);
        }

        private IEnumerator LifetimeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < lifetimeSeconds)
            {
                elapsed += Time.deltaTime;
                // Subtle breathing / wobble
                if (decoyRenderer != null)
                {
                    float wobble = 1f + Mathf.Sin(elapsed * 10f) * 0.08f;
                    decoyRenderer.transform.localScale = new Vector3(wobble, 1f / wobble, 1f);
                }
                yield return null;
            }

            Dismiss();
        }

        private void SpawnSmokeBomb(Vector2 position)
        {
            GameObject smokeObj = new GameObject("GreenSmokeBombEffect");
            smokeObj.transform.position = position;

            ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 2.5f;
            main.startSize = 0.6f;
            main.startColor = smokeColor;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            ParticleSystemRenderer psr = smokeObj.GetComponent<ParticleSystemRenderer>();
            psr.sortingOrder = 15;
            psr.material = new Material(Shader.Find("Sprites/Default"));

            ps.Play();
        }
    }
}
