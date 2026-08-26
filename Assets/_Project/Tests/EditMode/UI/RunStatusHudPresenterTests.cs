using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.UI;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.UI
{
    public sealed class RunStatusHudPresenterTests
    {
        private GameObject _gameObject;
        private PlayerWeaponInventory _inventory;
        private WeaponConfig _temporaryWeapon;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("RunStatusHudPresenterTests");
            _inventory = _gameObject.AddComponent<PlayerWeaponInventory>();
            _temporaryWeapon = ScriptableObject.CreateInstance<WeaponConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_temporaryWeapon);
            Object.DestroyImmediate(_gameObject);
        }

        [TestCase(0f, "Time: 00:00")]
        [TestCase(59.9f, "Time: 00:59")]
        [TestCase(60f, "Time: 01:00")]
        [TestCase(3661f, "Time: 61:01")]
        public void FormatTime_UsesWholeMinutesAndSeconds(float seconds, string expected)
        {
            Assert.That(RunStatusHudPresenter.FormatTime(seconds), Is.EqualTo(expected));
        }

        [Test]
        public void FormatAmmo_ShowsInfinityWithoutTemporaryWeapon()
        {
            Assert.That(RunStatusHudPresenter.FormatAmmo(_inventory), Is.EqualTo("Ammo: ∞"));
        }

        [Test]
        public void FormatAmmo_ShowsRemainingTemporaryShots()
        {
            _inventory.EquipTemporaryWeapon(_temporaryWeapon, 3);

            Assert.That(RunStatusHudPresenter.FormatAmmo(_inventory), Is.EqualTo("Ammo: 3"));
        }
    }
}
