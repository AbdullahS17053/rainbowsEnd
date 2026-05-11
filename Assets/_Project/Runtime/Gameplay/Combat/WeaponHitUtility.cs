using RainbowsEnd.Core.Common;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public static class WeaponHitUtility
    {
        public static bool TryResolveHit(
            Collider2D collider,
            Collider2D ignoredCollider,
            FactionId sourceFaction,
            float damage,
            Vector2 point,
            Vector2 normal,
            Object instigator,
            out WeaponHitResult hitResult)
        {
            if (collider == null || IsIgnoredCollider(collider, ignoredCollider))
            {
                hitResult = default;
                return false;
            }

            ITeamAffiliation teamAffiliation = ResolveInterfaceInParents<ITeamAffiliation>(collider);
            if (teamAffiliation != null && teamAffiliation.FactionId == sourceFaction)
            {
                hitResult = default;
                return false;
            }

            IDamageable damageable = ResolveInterfaceInParents<IDamageable>(collider);
            if (damageable != null)
            {
                DamageResult damageResult = damageable.ApplyDamage(new DamageInfo(damage, sourceFaction, point, instigator));
                hitResult = WeaponHitResult.Damageable(collider, point, normal, damageResult);
                return true;
            }

            if (collider.isTrigger)
            {
                hitResult = default;
                return false;
            }

            hitResult = WeaponHitResult.Surface(collider, point, normal);
            return true;
        }

        private static bool IsIgnoredCollider(Collider2D candidate, Collider2D ignoredCollider)
        {
            if (candidate == null || ignoredCollider == null)
            {
                return false;
            }

            return candidate == ignoredCollider || candidate.transform.IsChildOf(ignoredCollider.transform);
        }

        private static TInterface ResolveInterfaceInParents<TInterface>(Component component) where TInterface : class
        {
            MonoBehaviour[] behaviours = component.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is TInterface match)
                {
                    return match;
                }
            }

            return null;
        }
    }
}