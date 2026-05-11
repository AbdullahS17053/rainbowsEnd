using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    public sealed class PlayerWeaponInventory : MonoBehaviour
    {
        [SerializeField] private WeaponConfig defaultWeaponConfig;

        private WeaponConfig _temporaryWeaponConfig;
        private int _remainingTemporaryShots;
        private bool _pendingCooldownReset;

        public WeaponConfig DefaultWeaponConfig => defaultWeaponConfig;

        public WeaponConfig ActiveWeaponConfig => _temporaryWeaponConfig != null ? _temporaryWeaponConfig : defaultWeaponConfig;

        public int RemainingTemporaryShots => _temporaryWeaponConfig != null ? _remainingTemporaryShots : 0;

        public bool HasTemporaryWeapon => _temporaryWeaponConfig != null && _remainingTemporaryShots > 0;

        public void ConfigureDefaultWeapon(WeaponConfig weaponConfig)
        {
            if (weaponConfig == null)
            {
                return;
            }

            defaultWeaponConfig = weaponConfig;
        }

        public void EquipTemporaryWeapon(WeaponConfig weaponConfig, int shotCount)
        {
            if (weaponConfig == null || shotCount <= 0)
            {
                return;
            }

            _temporaryWeaponConfig = weaponConfig;
            _remainingTemporaryShots = shotCount;
            _pendingCooldownReset = true;
        }

        public bool ConsumeTemporaryShot()
        {
            if (!HasTemporaryWeapon)
            {
                return false;
            }

            _remainingTemporaryShots = Mathf.Max(0, _remainingTemporaryShots - 1);
            if (_remainingTemporaryShots > 0)
            {
                return false;
            }

            _temporaryWeaponConfig = null;
            return true;
        }

        public bool ConsumePendingCooldownReset()
        {
            bool shouldResetCooldown = _pendingCooldownReset;
            _pendingCooldownReset = false;
            return shouldResetCooldown;
        }
    }
}