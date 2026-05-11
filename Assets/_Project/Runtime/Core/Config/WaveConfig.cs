using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_WaveConfig", menuName = "Rainbows End/Config/Wave")]
    public sealed class WaveConfig : ScriptableObject
    {
        [field: SerializeField] [field: Min(0f)] public float InitialWaveDelaySeconds { get; private set; } = 0.75f;

        [field: SerializeField] [field: Min(0f)] public float SpawnIntervalSeconds { get; private set; } = 2f;

        [field: SerializeField] [field: Min(1)] public int MaxConcurrentEnemies { get; private set; } = 6;

        [field: SerializeField] [field: Min(1)] public int InitialTravellerCount { get; private set; } = 1;

        [field: SerializeField] [field: Min(1)] public int PoliceUnlockWave { get; private set; } = 3;
    }
}