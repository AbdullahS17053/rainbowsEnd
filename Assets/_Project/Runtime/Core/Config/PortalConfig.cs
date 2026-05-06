using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_PortalConfig", menuName = "Rainbows End/Config/Portal")]
    public sealed class PortalConfig : ScriptableObject
    {
        [field: SerializeField] [field: Min(0.01f)] public float CooldownSeconds { get; private set; } = 3f;

        [field: SerializeField] [field: Min(1)] public int MaxActivePortals { get; private set; } = 2;

        [field: SerializeField] [field: Min(0f)] public float ExitOffsetDistance { get; private set; } = 0.75f;

        [field: SerializeField] [field: Min(0f)] public float ReentryLockoutSeconds { get; private set; } = 0.2f;
    }
}