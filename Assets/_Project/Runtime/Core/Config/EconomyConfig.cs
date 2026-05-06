using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_EconomyConfig", menuName = "Rainbows End/Config/Economy")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [field: SerializeField] public int StartingGold { get; private set; }

        [field: SerializeField] [field: Min(0f)] public float OnePotHarvestMultiplier { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float TwoPotHarvestMultiplier { get; private set; } = 2.5f;

        [field: SerializeField] [field: Min(0f)] public float ThreePotHarvestMultiplier { get; private set; } = 5f;

        [field: SerializeField] [field: Min(0)] public int EnemyDropGoldMin { get; private set; } = 1;

        [field: SerializeField] [field: Min(0)] public int EnemyDropGoldMax { get; private set; } = 3;
    }
}