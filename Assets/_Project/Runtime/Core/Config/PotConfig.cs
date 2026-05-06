using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_PotConfig", menuName = "Rainbows End/Config/Pot")]
    public sealed class PotConfig : ScriptableObject
    {
        [field: SerializeField] [field: Min(0.01f)] public float MaxHealth { get; private set; } = 5f;

        [field: SerializeField] [field: Min(0f)] public float PushResistance { get; private set; } = 1f;

        [field: SerializeField] [field: Min(0f)] public float CaptureRadius { get; private set; } = 0.65f;

        [field: SerializeField] [field: Min(0f)] public float HarvestCancelDelaySeconds { get; private set; } = 0f;
    }
}