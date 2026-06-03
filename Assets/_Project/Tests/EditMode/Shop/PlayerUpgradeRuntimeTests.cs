using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Shop
{
    public sealed class PlayerUpgradeRuntimeTests
    {
        private GameObject _playerObject;
        private PlayerUpgradeRuntime _upgradeRuntime;
        private ShopConfig _shopConfig;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("Player");
            _playerObject.AddComponent<PlayerRoot>();
            _upgradeRuntime = _playerObject.AddComponent<PlayerUpgradeRuntime>();
            _shopConfig = ScriptableObject.CreateInstance<ShopConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_shopConfig);
            Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void ApplyPurchase_UpdatesResolvedWeaponStats()
        {
            ShopUpgradeDefinition damageUpgrade = CreateDefinition(0, "Damage Up", ShopUpgradeEffectType.WeaponDamage, 20, 3, 0.25f);
            ShopUpgradeDefinition fireRateUpgrade = CreateDefinition(1, "Fire Rate Up", ShopUpgradeEffectType.WeaponFireRate, 25, 3, 0.1f);
            ShopUpgradeDefinition accuracyUpgrade = CreateDefinition(2, "Accuracy Up", ShopUpgradeEffectType.WeaponAccuracy, 15, 3, 0.15f);

            Assert.That(_upgradeRuntime.ApplyPurchase(damageUpgrade), Is.True);
            Assert.That(_upgradeRuntime.ApplyPurchase(fireRateUpgrade), Is.True);
            Assert.That(_upgradeRuntime.ApplyPurchase(accuracyUpgrade), Is.True);

            Assert.That(_upgradeRuntime.ResolveWeaponDamage(8f), Is.EqualTo(10f).Within(0.001f));
            Assert.That(_upgradeRuntime.ResolveWeaponCooldownSeconds(1f), Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(_upgradeRuntime.ResolveWeaponSpreadDegrees(20f), Is.EqualTo(17f).Within(0.001f));
        }

        [Test]
        public void CanPurchase_BlocksPurchasesBeyondConfiguredMax()
        {
            ShopUpgradeDefinition damageUpgrade = CreateDefinition(0, "Damage Up", ShopUpgradeEffectType.WeaponDamage, 20, 2, 0.25f);

            Assert.That(_upgradeRuntime.CanPurchase(damageUpgrade), Is.True);
            Assert.That(_upgradeRuntime.ApplyPurchase(damageUpgrade), Is.True);
            Assert.That(_upgradeRuntime.CanPurchase(damageUpgrade), Is.True);
            Assert.That(_upgradeRuntime.ApplyPurchase(damageUpgrade), Is.True);

            Assert.That(_upgradeRuntime.CanPurchase(damageUpgrade), Is.False);
            Assert.That(_upgradeRuntime.ApplyPurchase(damageUpgrade), Is.False);
        }

        private ShopUpgradeDefinition CreateDefinition(
            int arrayIndex,
            string displayName,
            ShopUpgradeEffectType effectType,
            int cost,
            int maxPurchaseCount,
            float valuePerPurchase)
        {
            SerializedObject serializedObject = new SerializedObject(_shopConfig);
            SerializedProperty upgradesProperty = serializedObject.FindProperty("upgrades");
            upgradesProperty.arraySize = Mathf.Max(upgradesProperty.arraySize, arrayIndex + 1);

            SerializedProperty definitionProperty = upgradesProperty.GetArrayElementAtIndex(arrayIndex);
            definitionProperty.FindPropertyRelative("displayName").stringValue = displayName;
            definitionProperty.FindPropertyRelative("description").stringValue = displayName;
            definitionProperty.FindPropertyRelative("effectType").enumValueIndex = (int)effectType;
            definitionProperty.FindPropertyRelative("cost").intValue = cost;
            definitionProperty.FindPropertyRelative("maxPurchaseCount").intValue = maxPurchaseCount;
            definitionProperty.FindPropertyRelative("valuePerPurchase").floatValue = valuePerPurchase;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return _shopConfig.Upgrades[arrayIndex];
        }
    }
}