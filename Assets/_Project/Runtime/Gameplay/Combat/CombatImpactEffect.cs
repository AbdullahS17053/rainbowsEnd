using RainbowsEnd.Core.Utilities;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class CombatImpactEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] [Min(0.01f)] private float defaultLifetimeSeconds = 0.12f;

        private readonly CountdownTimer _lifetimeTimer = new CountdownTimer();
        private PrefabPool _owningPool;

        public void Play(PrefabPool owningPool, float lifetimeSeconds, float uniformScale)
        {
            _owningPool = owningPool;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, uniformScale);
            _lifetimeTimer.Start(Mathf.Max(0.01f, lifetimeSeconds > 0f ? lifetimeSeconds : defaultLifetimeSeconds));
        }

        public void OnSpawned()
        {
            _lifetimeTimer.Reset();
        }

        public void OnDespawned()
        {
            _lifetimeTimer.Reset();
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
    }
}