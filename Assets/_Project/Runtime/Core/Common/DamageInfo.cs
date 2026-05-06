using UnityEngine;

namespace RainbowsEnd.Core.Common
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, FactionId sourceFaction, Vector2 hitPoint, Object instigator = null)
        {
            Amount = Mathf.Max(0f, amount);
            SourceFaction = sourceFaction;
            HitPoint = hitPoint;
            Instigator = instigator;
        }

        public float Amount { get; }

        public FactionId SourceFaction { get; }

        public Vector2 HitPoint { get; }

        public Object Instigator { get; }
    }
}