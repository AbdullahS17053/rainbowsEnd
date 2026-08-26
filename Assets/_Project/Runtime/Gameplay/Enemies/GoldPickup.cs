using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoldPickup : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D triggerCollider;

        private PotRegistry _potRegistry;
        private PrefabPool _owningPool;
        private float _goldAmount;

        public void Activate(PrefabPool owningPool, int goldAmount)
        {
            _owningPool = owningPool;
            _goldAmount = Mathf.Max(0, goldAmount);
            EnsureCollider();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void OnSpawned()
        {
            EnsureCollider();
            _goldAmount = 0f;
            _potRegistry = null;
            _owningPool = null;
        }

        public void OnDespawned()
        {
            _goldAmount = 0f;
            _potRegistry = null;
            _owningPool = null;
        }

        private void Reset()
        {
            EnsureCollider();
        }

        private void OnValidate()
        {
            EnsureCollider();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_goldAmount <= 0f || other.GetComponentInParent<PlayerRoot>() == null)
            {
                return;
            }

            ResolvePotRegistry();
            float acceptedGold = _potRegistry != null ? _potRegistry.StoreGold(_goldAmount) : 0f;
            _goldAmount -= acceptedGold;
            if (_goldAmount > 0.0001f)
            {
                return;
            }

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            owningPool?.Return(gameObject);
        }

        private void EnsureCollider()
        {
            triggerCollider ??= GetComponent<Collider2D>();
        }

        private void ResolvePotRegistry()
        {
            if (_potRegistry != null)
            {
                return;
            }

            if (ProjectContext.Instance?.Services.TryGet(out PotRegistry service) == true)
            {
                _potRegistry = service;
                return;
            }

            _potRegistry = FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
        }
    }
}