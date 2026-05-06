using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    public abstract class ActorConfigBase : ScriptableObject
    {
        [field: SerializeField] [field: Min(0.01f)] public float MaxHealth { get; private set; } = 10f;

        [field: SerializeField] [field: Min(0f)] public float MoveSpeed { get; private set; } = 5f;
    }
}