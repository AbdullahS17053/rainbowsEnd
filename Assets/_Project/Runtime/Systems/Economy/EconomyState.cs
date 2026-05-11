using System;
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
        private bool _isInitialized;
        private float _currentGoldValue;

        public event Action<int> GoldChanged;

        public EconomyConfig Config => config;

        public float CurrentGoldValue => _currentGoldValue;

        public int CurrentGold => Mathf.FloorToInt(_currentGoldValue);

        public int ActiveHarvesterCount => _activeHarvesterIds.Count;

        public float CurrentHarvestMultiplier => GetHarvestMultiplier(ActiveHarvesterCount);

        public void Configure(EconomyConfig economyConfig)
        {
            config = economyConfig;
            Initialize(forceReset: true);
            RegisterWithProjectContext();
        }

        public void AddGold(float amount)
        {
            Initialize();
            if (amount <= 0f)
            {
                return;
            }

            int previousGold = CurrentGold;
            _currentGoldValue += amount;
            if (CurrentGold != previousGold)
            {
                GoldChanged?.Invoke(CurrentGold);
            }
        }

        public float RemoveGold(float amount)
        {
            Initialize();
            if (amount <= 0f)
            {
                return 0f;
            }

            int previousGold = CurrentGold;
            float removedAmount = Mathf.Min(_currentGoldValue, amount);
            _currentGoldValue -= removedAmount;
            if (CurrentGold != previousGold)
            {
                GoldChanged?.Invoke(CurrentGold);
            }

            return removedAmount;
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
            Initialize();

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
            Initialize();
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

        private void Initialize(bool forceReset = false)
        {
            if (_isInitialized && !forceReset)
            {
                return;
            }

            _currentGoldValue = config != null ? Mathf.Max(0, config.StartingGold) : 0f;
            _activeHarvesterIds.Clear();
            _isInitialized = true;
            GoldChanged?.Invoke(CurrentGold);
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