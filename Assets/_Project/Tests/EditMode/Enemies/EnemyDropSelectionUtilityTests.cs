using System.Collections.Generic;
using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Enemies;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class EnemyDropSelectionUtilityTests
    {
        private WeaponConfig _pistolConfig;
        private WeaponConfig _shotgunConfig;

        [SetUp]
        public void SetUp()
        {
            _pistolConfig = ScriptableObject.CreateInstance<WeaponConfig>();
            _shotgunConfig = ScriptableObject.CreateInstance<WeaponConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pistolConfig);
            Object.DestroyImmediate(_shotgunConfig);
        }

        [Test]
        public void SelectGoldDropIndex_SkipsInvalidEntries()
        {
            List<GoldDropOption> options = new List<GoldDropOption>
            {
                new GoldDropOption(0),
                new GoldDropOption(2),
                new GoldDropOption(5)
            };

            int selectedIndex = EnemyDropSelectionUtility.SelectGoldDropIndex(options, 0f);

            Assert.That(selectedIndex, Is.EqualTo(1));
        }

        [Test]
        public void SelectGoldAmount_UsesRollAcrossValidEntries()
        {
            List<GoldDropOption> options = new List<GoldDropOption>
            {
                new GoldDropOption(1),
                new GoldDropOption(3),
                new GoldDropOption(5)
            };

            int goldAmount = EnemyDropSelectionUtility.SelectGoldAmount(options, 0.8f);

            Assert.That(goldAmount, Is.EqualTo(5));
        }

        [Test]
        public void TrySelectWeaponDrop_ReturnsConfiguredWeaponAndAmmo()
        {
            List<WeaponDropOption> options = new List<WeaponDropOption>
            {
                new WeaponDropOption(null, 4),
                new WeaponDropOption(_pistolConfig, 10),
                new WeaponDropOption(_shotgunConfig, 6)
            };

            bool selected = EnemyDropSelectionUtility.TrySelectWeaponDrop(options, 0.75f, out WeaponConfig weaponConfig, out int ammoCount);

            Assert.That(selected, Is.True);
            Assert.That(weaponConfig, Is.SameAs(_shotgunConfig));
            Assert.That(ammoCount, Is.EqualTo(6));
        }

        [Test]
        public void TrySelectWeaponDrop_ReturnsFalseWhenNoValidOptionsExist()
        {
            List<WeaponDropOption> options = new List<WeaponDropOption>
            {
                new WeaponDropOption(null, 0),
                new WeaponDropOption(_pistolConfig, 0)
            };

            bool selected = EnemyDropSelectionUtility.TrySelectWeaponDrop(options, 0.25f, out WeaponConfig weaponConfig, out int ammoCount);

            Assert.That(selected, Is.False);
            Assert.That(weaponConfig, Is.Null);
            Assert.That(ammoCount, Is.EqualTo(0));
        }
    }
}