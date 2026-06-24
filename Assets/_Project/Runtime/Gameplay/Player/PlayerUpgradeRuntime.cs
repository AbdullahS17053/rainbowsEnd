using System;
using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    [RequireComponent(typeof(PlayerRoot))]
    public sealed class PlayerUpgradeRuntime : MonoBehaviour
    {
        private const float MinimumCooldownMultiplier = 0.2f;
        private const float MinimumSpreadMultiplier = 0.1f;

        [SerializeField] private PlayerRoot playerRoot;

        private int[] _purchasedLevels;
        private int _potsPurchased;
        private float _weaponDamageBonus;
        private float _weaponCooldownReduction;
        private float _weaponSpreadReduction;

        public event Action Changed;

        public int PotsPurchased => _potsPurchased;

        public float WeaponDamageMultiplier => 1f + Mathf.Max(0f, _weaponDamageBonus);

        public float WeaponCooldownMultiplier => Mathf.Max(MinimumCooldownMultiplier, 1f - Mathf.Max(0f, _weaponCooldownReduction));

        public float WeaponSpreadMultiplier => Mathf.Max(MinimumSpreadMultiplier, 1f - Mathf.Max(0f, _weaponSpreadReduction));

        private void Reset()
        {
            CacheReferences();
            EnsureLevelStorage();
        }

        private void OnValidate()
        {
            CacheReferences();
            EnsureLevelStorage();
        }

        private void Awake()
        {
            CacheReferences();
            EnsureLevelStorage();
        }

        public int GetPurchasedLevel(ShopUpgradeEffectType effectType)
        {
            EnsureLevelStorage();

            int effectIndex = (int)effectType;
            if (effectIndex < 0 || effectIndex >= _purchasedLevels.Length)
            {
                return 0;
            }

            return _purchasedLevels[effectIndex];
        }

        public bool CanPurchase(ShopUpgradeDefinition definition)
        {
            return GetPurchasedLevel(definition.EffectType) < definition.MaxPurchaseCount;
        }

        public bool ApplyPurchase(ShopUpgradeDefinition definition)
        {
            if (!CanPurchase(definition))
            {
                return false;
            }

            EnsureLevelStorage();

            int effectIndex = (int)definition.EffectType;
            _purchasedLevels[effectIndex]++;
            ApplyDefinition(definition);
            Changed?.Invoke();
            return true;
        }

        public void ResetPurchasedUpgrades()
        {
            EnsureLevelStorage();

            Array.Clear(_purchasedLevels, 0, _purchasedLevels.Length);
            _potsPurchased = 0;
            _weaponDamageBonus = 0f;
            _weaponCooldownReduction = 0f;
            _weaponSpreadReduction = 0f;
            Changed?.Invoke();
        }

        public float ResolveWeaponDamage(float baseDamage)
        {
            return Mathf.Max(0f, baseDamage) * WeaponDamageMultiplier;
        }

        public float ResolveWeaponCooldownSeconds(float baseCooldownSeconds)
        {
            float clampedBaseCooldown = Mathf.Max(0.01f, baseCooldownSeconds);
            return Mathf.Max(0.01f, clampedBaseCooldown * WeaponCooldownMultiplier);
        }

        public float ResolveWeaponSpreadDegrees(float baseSpreadDegrees)
        {
            return Mathf.Max(0f, baseSpreadDegrees) * WeaponSpreadMultiplier;
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
        }

        private void EnsureLevelStorage()
        {
            int effectCount = Enum.GetValues(typeof(ShopUpgradeEffectType)).Length;
            if (_purchasedLevels != null && _purchasedLevels.Length == effectCount)
            {
                return;
            }

            _purchasedLevels = new int[effectCount];
        }

        private void ApplyDefinition(ShopUpgradeDefinition definition)
        {
            float valuePerPurchase = definition.ValuePerPurchase;

            switch (definition.EffectType)
            {
                case ShopUpgradeEffectType.WeaponDamage:
                    _weaponDamageBonus += valuePerPurchase;
                    break;

                case ShopUpgradeEffectType.WeaponFireRate:
                    _weaponCooldownReduction += valuePerPurchase;
                    break;

                case ShopUpgradeEffectType.WeaponAccuracy:
                    _weaponSpreadReduction += valuePerPurchase;
                    break;

                case ShopUpgradeEffectType.PotPurchase2:
                case ShopUpgradeEffectType.PotPurchase3:
                    _potsPurchased++;
                    break;
            }
        }
    }
}