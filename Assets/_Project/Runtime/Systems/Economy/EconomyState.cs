using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Systems.Economy
{
    public sealed class EconomyState : MonoBehaviour
    {
        [SerializeField] private EconomyConfig config;

        private readonly HashSet<int> _activeHarvesterIds = new HashSet<int>();

        public EconomyConfig Config => config;

        public int ActiveHarvesterCount => _activeHarvesterIds.Count;

        public float CurrentHarvestMultiplier => GetHarvestMultiplier(ActiveHarvesterCount);

        public void Configure(EconomyConfig economyConfig)
        {
            config = economyConfig;
            _activeHarvesterIds.Clear();
            RegisterWithProjectContext();
        }

        public void SetHarvesterActive(UnityEngine.Object owner, bool isActive)
        {
            if (owner == null)
            {
                return;
            }

            int ownerId = owner.GetInstanceID();
            if (isActive)
            {
                _activeHarvesterIds.Add(ownerId);
            }
            else
            {
                _activeHarvesterIds.Remove(ownerId);
            }
        }

        public float GetHarvestMultiplier(int activePotCount)
        {
            if (config == null || activePotCount <= 0)
            {
                return 1f;
            }

            return activePotCount switch
            {
                1 => Mathf.Max(0f, config.OnePotHarvestMultiplier),
                2 => Mathf.Max(0f, config.TwoPotHarvestMultiplier),
                _ => Mathf.Max(0f, config.ThreePotHarvestMultiplier)
            };
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

        private void ResolveDefaults()
        {
            if (config == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                config = ProjectContext.Instance.ConfigRegistry.EconomyConfig;
            }
        }

        private void RegisterWithProjectContext()
        {
            if (ProjectContext.Instance?.ServiceRegistry != null)
            {
                ProjectContext.Instance.ServiceRegistry.Register(this, replaceExisting: true);
            }
        }
    }
}