using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Player
{
    public sealed class PlayerWeaponInventoryTests
    {
        private GameObject _gameObject;
        private PlayerWeaponInventory _inventory;
        private WeaponConfig _defaultWeapon;
        private WeaponConfig _temporaryWeapon;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("PlayerWeaponInventoryTests");
            _inventory = _gameObject.AddComponent<PlayerWeaponInventory>();
            _defaultWeapon = ScriptableObject.CreateInstance<WeaponConfig>();
            _temporaryWeapon = ScriptableObject.CreateInstance<WeaponConfig>();
            _inventory.ConfigureDefaultWeapon(_defaultWeapon);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_defaultWeapon);
            Object.DestroyImmediate(_temporaryWeapon);
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void EquipTemporaryWeapon_SetsActiveWeaponAndCooldownReset()
        {
            _inventory.EquipTemporaryWeapon(_temporaryWeapon, 3);

            Assert.That(_inventory.ActiveWeaponConfig, Is.SameAs(_temporaryWeapon));
            Assert.That(_inventory.RemainingTemporaryShots, Is.EqualTo(3));
            Assert.That(_inventory.ConsumePendingCooldownReset(), Is.True);
            Assert.That(_inventory.ConsumePendingCooldownReset(), Is.False);
        }

        [Test]
        public void ConsumeTemporaryShot_RevertsToDefaultWeaponWhenAmmoEnds()
        {
            _inventory.EquipTemporaryWeapon(_temporaryWeapon, 2);
            _inventory.ConsumePendingCooldownReset();

            Assert.That(_inventory.ConsumeTemporaryShot(), Is.False);
            Assert.That(_inventory.ActiveWeaponConfig, Is.SameAs(_temporaryWeapon));
            Assert.That(_inventory.RemainingTemporaryShots, Is.EqualTo(1));

            Assert.That(_inventory.ConsumeTemporaryShot(), Is.True);
            Assert.That(_inventory.ActiveWeaponConfig, Is.SameAs(_defaultWeapon));
            Assert.That(_inventory.RemainingTemporaryShots, Is.EqualTo(0));
            Assert.That(_inventory.ConsumePendingCooldownReset(), Is.False);
        }
    }
}