using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_EnemyConfig", menuName = "Rainbows End/Config/Enemy")]
    public sealed class EnemyConfig : ActorConfigBase
    {
        [field: SerializeField] [field: Min(0f)] public float ContactDamage { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float TargetRefreshSeconds { get; private set; } = 0.25f;

        [field: SerializeField] [field: Min(0f)] public float EngagementRange { get; private set; } = 10f;

        [field: SerializeField] [field: Min(0f)] public float SeparationRadius { get; private set; } = 0.6f;

        [field: SerializeField] [field: Min(0f)] public float SeparationStrength { get; private set; } = 2f;
    }
}