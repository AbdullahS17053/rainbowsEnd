using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class ProjectileWeaponLauncher : MonoBehaviour
    {
        [SerializeField] private string poolObjectName = "ArrowProjectilePool";
        [SerializeField] private PrefabPool projectilePool;

        public void Configure(PrefabPool pool)
        {
            projectilePool = pool;
        }

        public int LaunchVolley(
            Vector2 origin,
            Vector2 baseDirection,
            WeaponConfig weaponConfig,
            FactionId sourceFaction,
            Collider2D ignoredCollider,
            Object instigator,
            WeaponImpactFeedbackSpawner impactFeedbackSpawner,
            float damageOverride = -1f)
        {
            if (weaponConfig == null || !ResolvePool())
            {
                return 0;
            }

            int projectileCount = Mathf.Max(1, weaponConfig.ProjectileCount);
            int launchedCount = 0;
            for (int index = 0; index < projectileCount; index++)
            {
                Vector2 projectileDirection = WeaponVolleyUtility.CalculateProjectileDirection(
                    baseDirection,
                    index,
                    projectileCount,
                    weaponConfig.VolleySpreadDegrees);

                if (LaunchResolved(origin, projectileDirection, weaponConfig, sourceFaction, ignoredCollider, instigator, impactFeedbackSpawner, damageOverride))
                {
                    launchedCount++;
                }
            }

            return launchedCount;
        }

        public bool Launch(
            Vector2 origin,
            Vector2 direction,
            WeaponConfig weaponConfig,
            FactionId sourceFaction,
            Collider2D ignoredCollider,
            Object instigator,
            WeaponImpactFeedbackSpawner impactFeedbackSpawner,
            float damageOverride = -1f)
        {
            if (weaponConfig == null || !ResolvePool())
            {
                return false;
            }

            return LaunchResolved(origin, direction, weaponConfig, sourceFaction, ignoredCollider, instigator, impactFeedbackSpawner, damageOverride);
        }

        private bool LaunchResolved(
            Vector2 origin,
            Vector2 direction,
            WeaponConfig weaponConfig,
            FactionId sourceFaction,
            Collider2D ignoredCollider,
            Object instigator,
            WeaponImpactFeedbackSpawner impactFeedbackSpawner,
            float damageOverride)
        {
            if (weaponConfig == null || projectilePool == null)
            {
                return false;
            }

            Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
            float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg - 90f;
            GameObject instance = projectilePool.Rent(origin, Quaternion.Euler(0f, 0f, angle));
            ArrowProjectile projectile = instance.GetComponent<ArrowProjectile>();
            if (projectile == null)
            {
                projectilePool.Return(instance);
                return false;
            }

            projectile.Launch(projectilePool, normalizedDirection, weaponConfig, sourceFaction, ignoredCollider, instigator, impactFeedbackSpawner, damageOverride);
            return true;
        }

        private bool ResolvePool()
        {
            if (projectilePool != null)
            {
                return true;
            }

            if (ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaSceneContext) == true && arenaSceneContext.EffectsRoot != null)
            {
                Transform poolTransform = arenaSceneContext.EffectsRoot.Find(poolObjectName);
                if (poolTransform != null)
                {
                    projectilePool = poolTransform.GetComponent<PrefabPool>();
                }
            }

            return projectilePool != null;
        }
    }
}