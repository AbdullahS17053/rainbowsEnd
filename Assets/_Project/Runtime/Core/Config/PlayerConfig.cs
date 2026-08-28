using UnityEngine;
using UnityEngine.Serialization;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_PlayerConfig", menuName = "Rainbows End/Config/Player")]
    public sealed class PlayerConfig : ActorConfigBase
    {
        [field: SerializeField] [field: Min(0f)] public float Acceleration { get; private set; } = 25f;

        [field: SerializeField] [field: Min(0f)] public float InteractionRadius { get; private set; } = 1.25f;

        [field: SerializeField] [field: Min(0f)] public float AimTurnSpeed { get; private set; } = 540f;

        [field: SerializeField] [field: Min(0f)] [field: FormerlySerializedAs("DownedDurationSeconds")] public float StunDurationSeconds { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float RecoveryVeilDurationSeconds { get; private set; } = 2f;

        [field: SerializeField] [field: Range(0f, 1f)] public float ConcealedOpacity { get; private set; } = 0.35f;

        [field: SerializeField] [field: Range(0f, 1f)] public float RecoveryVeilOpacity { get; private set; } = 0.35f;

        [field: SerializeField] [field: Min(0f)] public float ConcealmentRevealDurationSeconds { get; private set; } = 0.25f;
    }
}