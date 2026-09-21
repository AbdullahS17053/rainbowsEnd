using System;
using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    public enum ShopUpgradeEffectType
    {
        WeaponDamage = 0,
        WeaponFireRate = 1,
        WeaponAccuracy = 2,
        PotPurchase2 = 3,
        PotPurchase3 = 4,
        WeaponRange = 5,
        CloverOnPotPickup = 6,
        UnicornFaeBoltSlow = 7,
        ProjectileSpeed = 8
    }

    [Serializable]
    public struct ShopUpgradeDefinition
    {
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 3)] private string description;
        [SerializeField] private ShopUpgradeEffectType effectType;
        [SerializeField] [Min(0)] private int cost;
        [SerializeField] [Min(1)] private int maxPurchaseCount;
        [SerializeField] [Min(0f)] private float valuePerPurchase;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? effectType.ToString() : displayName;

        public string Description => description ?? string.Empty;

        public ShopUpgradeEffectType EffectType => effectType;

        public int Cost => Mathf.Max(0, cost);

        public int MaxPurchaseCount => Mathf.Max(1, maxPurchaseCount);

        public float ValuePerPurchase => Mathf.Max(0f, valuePerPurchase);
    }

    [CreateAssetMenu(fileName = "SO_ShopConfig", menuName = "Rainbows End/Config/Shop")]
    public sealed class ShopConfig : ScriptableObject
    {
        [SerializeField] private string shopTitle = "Fae Tree Shop";
        [SerializeField] [Min(0f)] private float interactionRadius = 1.75f;
        [SerializeField] private ShopUpgradeDefinition[] upgrades = Array.Empty<ShopUpgradeDefinition>();

        public string ShopTitle => string.IsNullOrWhiteSpace(shopTitle) ? "Fae Tree Shop" : shopTitle;

        public float InteractionRadius => Mathf.Max(0f, interactionRadius);

        public ShopUpgradeDefinition[] Upgrades => upgrades ?? Array.Empty<ShopUpgradeDefinition>();
    }
}