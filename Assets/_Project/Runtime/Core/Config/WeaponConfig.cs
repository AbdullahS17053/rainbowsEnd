using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_WeaponConfig", menuName = "Rainbows End/Config/Weapon")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [field: SerializeField] [field: Min(0f)] public float Damage { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0.01f)] public float CooldownSeconds { get; private set; } = 0.2f;

        [field: SerializeField] [field: Min(0f)] public float Range { get; private set; } = 8f;

        [field: SerializeField] [field: Min(0.01f)] public float ProjectileSpeed { get; private set; } = 18f;

        [field: SerializeField] [field: Min(0f)] public float MuzzleOffset { get; private set; } = 0.6f;

        [field: SerializeField] [field: Min(0f)] public float MinSpreadDegrees { get; private set; } = 2f;

        [field: SerializeField] [field: Min(0f)] public float MaxSpreadDegrees { get; private set; } = 18f;

        [field: SerializeField] [field: Min(0.01f)] public float ImpactEffectLifetimeSeconds { get; private set; } = 0.12f;

        [field: SerializeField] [field: Min(0.01f)] public float ImpactEffectScale { get; private set; } = 0.35f;
    }
}