using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_UnicornConfig", menuName = "Rainbows End/Config/Unicorn")]
    public sealed class UnicornConfig : ActorConfigBase
    {
        [field: SerializeField] [field: Min(0f)] public float RetargetIntervalSeconds { get; private set; } = 1.5f;

        [field: SerializeField] [field: Min(0f)] public float MinRoamDistance { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float MaxRoamDistance { get; private set; } = 4f;

        [field: SerializeField] [field: Min(0f)] public float TrailSpawnIntervalSeconds { get; private set; } = 0.1f;
    }
}