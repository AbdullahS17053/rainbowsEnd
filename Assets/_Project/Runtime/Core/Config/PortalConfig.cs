using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "SO_PortalConfig", menuName = "Rainbows End/Config/Portal")]
    public sealed class PortalConfig : ScriptableObject
    {
        [field: SerializeField] [field: Min(0.01f)] public float CooldownSeconds { get; private set; } = 3f;

        [field: SerializeField] [field: Min(1)] public int MaxActivePortals { get; private set; } = 2;

        [field: SerializeField] [field: Min(0f)] public float PlacementDistance { get; private set; } = 1.5f;

        [field: SerializeField] [field: Min(0f)] public float ExitOffsetDistance { get; private set; } = 0.75f;
        [field: SerializeField] [field: Min(0.01f)]
        [field: Tooltip("Effective world-space trigger radius. This does not resize portal VFX.")]
        public float EntryRadiusWorldUnits { get; private set; } = 0.60f;


        [field: SerializeField] [field: Min(0f)] public float ReentryLockoutSeconds { get; private set; } = 0.2f;

        [field: SerializeField] public Color OldestPortalColor { get; private set; } = new Color(0.2f, 0.4f, 0.9f, 0.95f);

        [field: SerializeField] public Color NewestPortalColor { get; private set; } = new Color(1f, 0.55f, 0.1f, 0.95f);
    }
}