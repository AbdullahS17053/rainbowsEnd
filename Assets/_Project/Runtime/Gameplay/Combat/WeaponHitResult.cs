using RainbowsEnd.Core.Common;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public readonly struct WeaponHitResult
    {
        private WeaponHitResult(bool didHit, bool hitDamageable, Collider2D collider, Vector2 point, Vector2 normal, DamageResult damageResult)
        {
            DidHit = didHit;
            HitDamageable = hitDamageable;
            Collider = collider;
            Point = point;
            Normal = normal;
            DamageResult = damageResult;
        }

        public bool DidHit { get; }

        public bool HitDamageable { get; }

        public Collider2D Collider { get; }

        public Vector2 Point { get; }

        public Vector2 Normal { get; }

        public DamageResult DamageResult { get; }

        public static WeaponHitResult Miss(Vector2 point)
        {
            return new WeaponHitResult(false, false, null, point, Vector2.zero, default);
        }

        public static WeaponHitResult Surface(Collider2D collider, Vector2 point, Vector2 normal)
        {
            return new WeaponHitResult(true, false, collider, point, normal, default);
        }

        public static WeaponHitResult Damageable(Collider2D collider, Vector2 point, Vector2 normal, DamageResult damageResult)
        {
            return new WeaponHitResult(true, true, collider, point, normal, damageResult);
        }
    }
}