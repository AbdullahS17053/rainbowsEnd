using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Systems.Economy;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoldPickup : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D triggerCollider;

        private EconomyState _economyState;
        private PrefabPool _owningPool;
        private int _goldAmount;

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
            _goldAmount = 0;
            _owningPool = null;
        }

        public void OnDespawned()
        {
            _goldAmount = 0;
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
            if (_goldAmount <= 0 || other.GetComponentInParent<PlayerRoot>() == null)
            {
                return;
            }

            ResolveEconomyState();
            _economyState?.AddGold(_goldAmount);

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            owningPool?.Return(gameObject);
        }

        private void EnsureCollider()
        {
            triggerCollider ??= GetComponent<Collider2D>();
        }

        private void ResolveEconomyState()
        {
            if (_economyState != null)
            {
                return;
            }

            if (ProjectContext.Instance?.Services.TryGet(out EconomyState economyState) == true)
            {
                _economyState = economyState;
                return;
            }

            _economyState = FindFirstObjectByType<EconomyState>(FindObjectsInactive.Include);
        }
    }
}