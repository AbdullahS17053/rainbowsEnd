using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_UnicornConfig", menuName = "Rainbows End/Config/Unicorn")]
    public sealed class UnicornConfig : ActorConfigBase
    {
        [field: SerializeField] [field: Min(0f)] public float RetargetIntervalSeconds { get; private set; } = 1.5f;

        [field: SerializeField] [field: Min(0f)] public float MinRoamDistance { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float MaxRoamDistance { get; private set; } = 4f;

        [field: SerializeField] [field: Min(0.01f)] public float TargetArrivalDistance { get; private set; } = 0.2f;

        [field: SerializeField] [field: Min(0f)] public float BoundaryCornerPadding { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float TrailSpawnIntervalSeconds { get; private set; } = 0.1f;

        [field: SerializeField] [field: Min(0.01f)] public float TrailCellSizeWorldUnits { get; private set; } = 0.32f;

        [field: SerializeField] [field: Range(0f, 180f)] public float BounceAngleMinDegrees { get; private set; } = 45f;

        [field: SerializeField] [field: Range(0f, 180f)] public float BounceAngleMaxDegrees { get; private set; } = 135f;

        [field: SerializeField] [field: Min(0f)] public float PlayerKnockbackDistance { get; private set; } = 2f;

        [field: SerializeField] [field: Min(0f)] public float PlayerDazeDurationSeconds { get; private set; } = 0.75f;
    }
}