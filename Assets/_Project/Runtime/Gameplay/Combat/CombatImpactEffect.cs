using RainbowsEnd.Core.Utilities;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class CombatImpactEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] [Min(0.01f)] private float defaultLifetimeSeconds = 0.12f;

        private readonly CountdownTimer _lifetimeTimer = new CountdownTimer();

        private GameObject _magicVisual;
        private GameObject _firearmVisual;
        private ParticleSystem[] _magicParticles;
        private ParticleSystem[] _firearmParticles;

        private PrefabPool _owningPool;

        public void Play(PrefabPool owningPool, float lifetimeSeconds, float uniformScale, bool isMagic = false)
        {
            _owningPool = owningPool;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, uniformScale);
            ApplyPresentation(isMagic);
            _lifetimeTimer.Start(Mathf.Max(0.01f, lifetimeSeconds > 0f ? lifetimeSeconds : defaultLifetimeSeconds));
        }

        public void OnSpawned()
        {
            _lifetimeTimer.Reset();
            ResetPresentation();
        }

        public void OnDespawned()
        {
            _lifetimeTimer.Reset();
            ResetPresentation();
            _owningPool = null;
        }

        private void Update()
        {
            _lifetimeTimer.Tick(Time.deltaTime);

            if (!_lifetimeTimer.IsComplete || _owningPool == null)
            {
                return;
            }

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            owningPool.Return(gameObject);
        }

        private void CachePresentation()
        {
            _magicVisual ??= transform.Find("FX_MagicBlast_01")?.gameObject;
            _firearmVisual ??= transform.Find("FX_Gunshot_01")?.gameObject;
            _magicParticles ??= _magicVisual != null
                ? _magicVisual.GetComponentsInChildren<ParticleSystem>(true)
                : System.Array.Empty<ParticleSystem>();
            _firearmParticles ??= _firearmVisual != null
                ? _firearmVisual.GetComponentsInChildren<ParticleSystem>(true)
                : System.Array.Empty<ParticleSystem>();
        }

        private void ApplyPresentation(bool isMagic)
        {
            CachePresentation();
            StopParticles(_magicParticles);
            StopParticles(_firearmParticles);

            if (_magicVisual != null)
            {
                _magicVisual.SetActive(isMagic);
            }

            if (_firearmVisual != null)
            {
                _firearmVisual.SetActive(!isMagic);
            }

            if (isMagic)
            {
                PlayParticles(_magicParticles);
            }
            else
            {
                PlayParticles(_firearmParticles);
            }
        }

        private void ResetPresentation()
        {
            CachePresentation();
            StopParticles(_magicParticles);
            StopParticles(_firearmParticles);
            _magicVisual?.SetActive(false);
            _firearmVisual?.SetActive(false);
        }

        private static void StopParticles(ParticleSystem[] particles)
        {
            for (int index = 0; index < (particles != null ? particles.Length : 0); index++)
            {
                particles[index]?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static void PlayParticles(ParticleSystem[] particles)
        {
            for (int index = 0; index < (particles != null ? particles.Length : 0); index++)
            {
                particles[index]?.Play(true);
            }
        }
}
}