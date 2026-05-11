using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class WeaponPickup : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D triggerCollider;

        private PrefabPool _owningPool;
        private Core.Config.WeaponConfig _weaponConfig;
        private int _ammoCount;

        public void Activate(PrefabPool owningPool, Core.Config.WeaponConfig weaponConfig, int ammoCount)
        {
            _owningPool = owningPool;
            _weaponConfig = weaponConfig;
            _ammoCount = Mathf.Max(0, ammoCount);

            EnsureCollider();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        public void OnSpawned()
        {
            EnsureCollider();
            _owningPool = null;
            _weaponConfig = null;
            _ammoCount = 0;
        }

        public void OnDespawned()
        {
            _owningPool = null;
            _weaponConfig = null;
            _ammoCount = 0;
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
            if (_weaponConfig == null || _ammoCount <= 0)
            {
                return;
            }

            PlayerWeaponInventory weaponInventory = other.GetComponentInParent<PlayerWeaponInventory>();
            if (weaponInventory == null)
            {
                return;
            }

            weaponInventory.EquipTemporaryWeapon(_weaponConfig, _ammoCount);

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            owningPool?.Return(gameObject);
        }

        private void EnsureCollider()
        {
            triggerCollider ??= GetComponent<Collider2D>();
        }
    }
}