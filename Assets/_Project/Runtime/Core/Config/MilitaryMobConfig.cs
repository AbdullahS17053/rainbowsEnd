using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_MilitaryMobConfig", menuName = "Rainbows End/Config/Military Mob")]
    public sealed class MilitaryMobConfig : ScriptableObject
    {
        [field: SerializeField] public WeaponConfig ShotgunWeapon { get; private set; }

        [field: SerializeField] public WeaponConfig AssaultRifleWeapon { get; private set; }

        [field: SerializeField] [field: Range(0f, 1f)] public float AssaultRifleChance { get; private set; } = 0.5f;

        [field: SerializeField] [field: Min(1)] public int BurstShotCount { get; private set; } = 3;

        [field: SerializeField] [field: Min(0f)] public float BurstPauseSeconds { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float AimTelegraphSeconds { get; private set; } = 0.35f;

        [field: SerializeField] public Color AimTelegraphColor { get; private set; } = Color.red;

        [field: SerializeField] [field: Min(0f)] public float AimTelegraphStartWidth { get; private set; } = 0.08f;

        [field: SerializeField] [field: Min(0f)] public float AimTelegraphEndWidth { get; private set; } = 0.02f;

        [field: SerializeField] [field: Min(1)] public int StandardCarryMobCount { get; private set; } = 2;

        [field: SerializeField] [field: Min(1)] public int ConcealedCarryMobCount { get; private set; } = 1;

        [field: SerializeField] [field: Min(0.1f)] public float CarryAssemblyRadius { get; private set; } = 1.5f;

        [field: SerializeField] [field: Min(0.1f)] public float CarryFollowDistance { get; private set; } = 2.5f;

        [field: SerializeField] [field: Range(0f, 1f)] public float DeathDropChance { get; private set; } = 0.33f;

        [field: SerializeField] [field: Range(0f, 1f)] public float WeaponRewardChance { get; private set; } = 0.5f;

        [field: SerializeField] [field: Min(0)] public int AssaultRifleDropAmmo { get; private set; } = 24;

        [field: SerializeField] [field: Min(0)] public int ShotgunDropAmmo { get; private set; } = 6;

        [field: SerializeField] [field: Min(0)] public int GoldDropAmount { get; private set; } = 10;
    }
}