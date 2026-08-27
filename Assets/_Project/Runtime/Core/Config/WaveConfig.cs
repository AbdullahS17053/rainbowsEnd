using System;
using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [Serializable]
    public struct WaveDefinition
    {
        [SerializeField] private string waveName;
        [SerializeField] [Min(0f)] private float startDelaySeconds;
        [SerializeField] [Min(0.1f)] private float spawnIntervalSeconds;
        [SerializeField] [Min(1)] private int maxConcurrentEnemies;
        [SerializeField] [Min(0)] private int travellerCount;
        [SerializeField] [Min(0)] private int policeCount;
        [SerializeField] [Min(0)] private int militaryCount;

        public WaveDefinition(
            string waveName,
            float startDelaySeconds,
            float spawnIntervalSeconds,
            int maxConcurrentEnemies,
            int travellerCount,
            int policeCount)
            : this(waveName, startDelaySeconds, spawnIntervalSeconds, maxConcurrentEnemies, travellerCount, policeCount, 0)
        {
        }

        public WaveDefinition(
            string waveName,
            float startDelaySeconds,
            float spawnIntervalSeconds,
            int maxConcurrentEnemies,
            int travellerCount,
            int policeCount,
            int militaryCount)
        {
            this.waveName = waveName;
            this.startDelaySeconds = Mathf.Max(0f, startDelaySeconds);
            this.spawnIntervalSeconds = Mathf.Max(0.1f, spawnIntervalSeconds);
            this.maxConcurrentEnemies = Mathf.Max(1, maxConcurrentEnemies);
            this.travellerCount = Mathf.Max(0, travellerCount);
            this.policeCount = Mathf.Max(0, policeCount);
            this.militaryCount = Mathf.Max(0, militaryCount);
        }

        public string WaveName => string.IsNullOrWhiteSpace(waveName) ? "Wave" : waveName;

        public float StartDelaySeconds => Mathf.Max(0f, startDelaySeconds);

        public float SpawnIntervalSeconds => Mathf.Max(0.1f, spawnIntervalSeconds);

        public int MaxConcurrentEnemies => Mathf.Max(1, maxConcurrentEnemies);

        public int TravellerCount => Mathf.Max(0, travellerCount);

        public int PoliceCount => Mathf.Max(0, policeCount);

        public int MilitaryCount => Mathf.Max(0, militaryCount);

        public int TotalEnemyCount => TravellerCount + PoliceCount + MilitaryCount;
    }

    [CreateAssetMenu(fileName = "SO_WaveConfig", menuName = "Rainbows End/Config/Wave")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField] private WaveDefinition[] waves =
        {
            new WaveDefinition("Wave 1", 0.75f, 1.8f, 3, 3, 0, 0),
            new WaveDefinition("Wave 2", 0.75f, 1.6f, 4, 5, 0, 0),
            new WaveDefinition("Wave 3", 0.75f, 1.5f, 5, 4, 1, 2),
            new WaveDefinition("Wave 4", 0.75f, 1.3f, 6, 5, 2, 3),
            new WaveDefinition("Wave 5", 0.75f, 1.2f, 7, 6, 3, 4)
        };

        public WaveDefinition[] Waves => waves ?? Array.Empty<WaveDefinition>();

        public int WaveCount => Waves.Length;

        public int MaxConcurrentEnemies
        {
            get
            {
                int maxConcurrentEnemies = 1;
                WaveDefinition[] authoredWaves = Waves;
                for (int index = 0; index < authoredWaves.Length; index++)
                {
                    maxConcurrentEnemies = Mathf.Max(maxConcurrentEnemies, authoredWaves[index].MaxConcurrentEnemies);
                }

                return maxConcurrentEnemies;
            }
        }
    }
}
