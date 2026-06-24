using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Spells
{
    public sealed class PortalRegistry : MonoBehaviour
    {
        [SerializeField] private string poolObjectName = "PortalPool";
        [SerializeField] private PortalConfig portalConfig;
        [SerializeField] private PrefabPool portalPool;

        private readonly List<PortalInstance> _activePortals = new List<PortalInstance>();

        public IReadOnlyList<PortalInstance> ActivePortals
        {
            get
            {
                RemoveNullEntries();
                return _activePortals;
            }
        }

        private void Awake()
        {
            ResolveDefaults();
            RegisterWithProjectContext();
        }

        private void OnEnable()
        {
            RegisterWithProjectContext();
        }

        public void Configure(PortalConfig config, PrefabPool pool)
        {
            portalConfig = config;
            portalPool = pool;
        }

        public bool TryPlacePortal(Vector2 position, Vector2 facingDirection, Object instigator)
        {
            ResolveDefaults();
            RemoveNullEntries();
            if (portalConfig == null || !ResolvePool())
            {
                return false;
            }

            int maxActivePortals = Mathf.Max(1, portalConfig.MaxActivePortals);
            while (_activePortals.Count >= maxActivePortals)
            {
                RemoveOldestPortal();
            }

            Vector2 normalizedFacingDirection = facingDirection.sqrMagnitude > 0f ? facingDirection.normalized : Vector2.up;
            float angle = Mathf.Atan2(normalizedFacingDirection.y, normalizedFacingDirection.x) * Mathf.Rad2Deg - 90f;
            PortalInstance portalInstance = portalPool.Rent<PortalInstance>(position, Quaternion.Euler(0f, 0f, angle));
            if (portalInstance == null)
            {
                return false;
            }

            portalInstance.Configure(this, portalConfig, normalizedFacingDirection, instigator);
            _activePortals.Add(portalInstance);
            UpdatePortalPairings();
            return true;
        }

        public void RemovePortal(PortalInstance portalInstance)
        {
            if (portalInstance == null)
            {
                return;
            }

            int removedCount = _activePortals.RemoveAll(candidate => candidate == null || candidate == portalInstance);
            if (removedCount <= 0)
            {
                return;
            }

            if (portalPool != null && portalInstance.gameObject.activeInHierarchy)
            {
                portalPool.Return(portalInstance.gameObject);
            }

            UpdatePortalPairings();
        }

        private void ResolveDefaults()
        {
            if (portalConfig == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                portalConfig = ProjectContext.Instance.ConfigRegistry.PortalConfig;
            }
        }

        private void RegisterWithProjectContext()
        {
            if (ProjectContext.Instance?.ServiceRegistry != null)
            {
                ProjectContext.Instance.ServiceRegistry.Register(this, replaceExisting: true);
            }
        }

        private bool ResolvePool()
        {
            if (portalPool != null)
            {
                return true;
            }

            if (ProjectContext.Instance?.Services.TryGet(out Core.SceneManagement.ArenaSceneContext arenaSceneContext) == true && arenaSceneContext.EffectsRoot != null)
            {
                Transform poolTransform = arenaSceneContext.EffectsRoot.Find(poolObjectName);
                if (poolTransform != null)
                {
                    portalPool = poolTransform.GetComponent<PrefabPool>();
                }
            }

            return portalPool != null;
        }

        private void RemoveOldestPortal()
        {
            RemoveNullEntries();
            if (_activePortals.Count <= 0)
            {
                return;
            }

            PortalInstance oldestPortal = _activePortals[0];
            _activePortals.RemoveAt(0);
            if (portalPool != null && oldestPortal != null)
            {
                portalPool.Return(oldestPortal.gameObject);
            }
        }

        private void UpdatePortalPairings()
        {
            RemoveNullEntries();
            for (int index = 0; index < _activePortals.Count; index++)
            {
                PortalInstance portalInstance = _activePortals[index];
                PortalInstance pairedPortal = null;
                if (_activePortals.Count == 2)
                {
                    pairedPortal = _activePortals[1 - index];
                }

                portalInstance?.SetPairedPortal(pairedPortal);
                portalInstance?.SetSlotIndex(index);
            }
        }

        private void RemoveNullEntries()
        {
            for (int index = _activePortals.Count - 1; index >= 0; index--)
            {
                if (_activePortals[index] == null)
                {
                    _activePortals.RemoveAt(index);
                }
            }
        }
    }
}