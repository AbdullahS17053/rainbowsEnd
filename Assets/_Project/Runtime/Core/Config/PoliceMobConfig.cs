using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_PoliceMobConfig", menuName = "Rainbows End/Config/Police Mob")]
    public sealed class PoliceMobConfig : ScriptableObject
    {
        [field: SerializeField] public WeaponConfig PistolWeapon { get; private set; }

        [field: SerializeField] public WeaponConfig ShotgunWeapon { get; private set; }

        [field: SerializeField] [field: Range(0f, 1f)] public float PistolChance { get; private set; } = 0.5f;

        [field: SerializeField] [field: Min(0)] public float AimHoldSeconds { get; private set; } = 0.25f;

        [field: SerializeField] [field: Min(0)] public float RepositionSeconds { get; private set; } = 0.75f;

        [field: SerializeField] [field: Range(0f, 1f)] public float DeathDropChance { get; private set; } = 0.33f;

        [field: SerializeField] [field: Range(0f, 1f)] public float WeaponRewardChance { get; private set; } = 0.5f;

        [field: SerializeField] [field: Min(0)] public int PistolDropAmmo { get; private set; } = 12;

        [field: SerializeField] [field: Min(0)] public int ShotgunDropAmmo { get; private set; } = 6;

        [field: SerializeField] [field: Min(0)] public int GoldDropAmount { get; private set; } = 10;
    }
}
