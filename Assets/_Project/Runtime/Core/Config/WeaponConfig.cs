using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    public enum WeaponPresentation
    {
        FaeBolt = 0,
        Pistol = 1,
        Shotgun = 2,
        AssaultRifle = 3
    }

    [CreateAssetMenu(fileName = "SO_WeaponConfig", menuName = "Rainbows End/Config/Weapon")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [field: SerializeField] public WeaponPresentation Presentation { get; private set; } = WeaponPresentation.FaeBolt;

        public bool IsFirearm => Presentation != WeaponPresentation.FaeBolt;

        [field: SerializeField] [field: Min(0f)] public float Damage { get; private set; } = 1f;

        [field: SerializeField] [field: Min(1)] public int ProjectileCount { get; private set; } = 1;

        [field: SerializeField] [field: Min(0.01f)] public float CooldownSeconds { get; private set; } = 0.2f;

        [field: SerializeField] [field: Min(0f)] public float Range { get; private set; } = 8f;

        [field: SerializeField] [field: Min(0f)] public float AutoFireConeDegrees { get; private set; } = 45f;

        [field: SerializeField] [field: Min(0.01f)] public float ProjectileSpeed { get; private set; } = 18f;

        [field: SerializeField] [field: Min(0f)] public float MuzzleOffset { get; private set; } = 0.6f;

        [field: SerializeField] [field: Min(0f)] public float MinSpreadDegrees { get; private set; } = 2f;

        [field: SerializeField] [field: Min(0f)] public float MaxSpreadDegrees { get; private set; } = 18f;

        [field: SerializeField] [field: Min(0f)] public float VolleySpreadDegrees { get; private set; } = 0f;

        [field: SerializeField] [field: Min(0.01f)] public float ImpactEffectLifetimeSeconds { get; private set; } = 0.12f;

        [field: SerializeField] [field: Min(0.01f)] public float ImpactEffectScale { get; private set; } = 0.35f;
    }
}