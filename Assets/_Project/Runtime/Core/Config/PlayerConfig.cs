using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_PlayerConfig", menuName = "Rainbows End/Config/Player")]
    public sealed class PlayerConfig : ActorConfigBase
    {
        [field: SerializeField] [field: Min(0f)] public float Acceleration { get; private set; } = 25f;

        [field: SerializeField] [field: Min(0f)] public float InteractionRadius { get; private set; } = 1.25f;

        [field: SerializeField] [field: Min(0f)] public float AimTurnSpeed { get; private set; } = 540f;

        [field: SerializeField] [field: Min(0f)] public float DownedDurationSeconds { get; private set; } = 3f;
    }
}