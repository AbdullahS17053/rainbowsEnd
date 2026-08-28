using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class WeaponImpactFeedbackSpawner : MonoBehaviour
    {
        [SerializeField] private string poolObjectName = "WeaponImpactPool";
        [SerializeField] private PrefabPool impactPool;

        public void Configure(PrefabPool pool)
        {
            impactPool = pool;
        }

        public void Spawn(WeaponHitResult hitResult, WeaponConfig weaponConfig)
        {
            if (!hitResult.DidHit || !ResolvePool())
            {
                return;
            }

            Quaternion rotation = hitResult.Normal.sqrMagnitude > 0f
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(hitResult.Normal.y, hitResult.Normal.x) * Mathf.Rad2Deg - 90f)
                : Quaternion.identity;

            GameObject instance = impactPool.Rent(hitResult.Point, rotation);
            CombatImpactEffect impactEffect = instance.GetComponent<CombatImpactEffect>();
            if (impactEffect == null)
            {
                impactPool.Return(instance);
                return;
            }

            float lifetimeSeconds = weaponConfig != null ? weaponConfig.ImpactEffectLifetimeSeconds : 0.12f;
            float uniformScale = weaponConfig != null ? weaponConfig.ImpactEffectScale : 0.35f;
            impactEffect.Play(
                impactPool,
                lifetimeSeconds,
                uniformScale,
                weaponConfig == null || weaponConfig.Presentation == WeaponPresentation.FaeBolt);
        }

        private bool ResolvePool()
        {
            if (impactPool != null)
            {
                return true;
            }

            if (ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaSceneContext) == true && arenaSceneContext.EffectsRoot != null)
            {
                Transform poolTransform = arenaSceneContext.EffectsRoot.Find(poolObjectName);
                if (poolTransform != null)
                {
                    impactPool = poolTransform.GetComponent<PrefabPool>();
                }
            }

            return impactPool != null;
        }
    }
}