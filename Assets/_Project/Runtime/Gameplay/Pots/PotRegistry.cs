using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Pots
{
    public sealed class PotRegistry : MonoBehaviour
    {
        private readonly List<PotController> _registeredPots = new List<PotController>();

        public IReadOnlyList<PotController> RegisteredPots
        {
            get
            {
                RemoveNullEntries();
                return _registeredPots;
            }
        }

        public int ActivePotCount
        {
            get
            {
                RemoveNullEntries();
                int count = 0;
                for (int index = 0; index < _registeredPots.Count; index++)
                {
                    PotController pot = _registeredPots[index];
                    if (pot != null && !pot.IsDestroyed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public float TotalStoredGoldValue
        {
            get
            {
                RemoveNullEntries();
                float total = 0f;
                for (int index = 0; index < _registeredPots.Count; index++)
                {
                    PotController pot = _registeredPots[index];
                    if (pot != null && !pot.IsDestroyed)
                    {
                        total += Mathf.Max(0f, pot.StoredGold);
                    }
                }

                return total;
            }
        }

        public int TotalStoredGold => Mathf.FloorToInt(TotalStoredGoldValue);

        public bool HasHarvestingPot
        {
            get
            {
                RemoveNullEntries();
                for (int index = 0; index < _registeredPots.Count; index++)
                {
                    PotController pot = _registeredPots[index];
                    if (pot != null && !pot.IsDestroyed && !pot.IsCarried
                        && pot.HarvestStatus == PotHarvestStatus.Harvesting)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasAnyGoldOrHarvestingPot
        {
            get
            {
                RemoveNullEntries();
                for (int index = 0; index < _registeredPots.Count; index++)
                {
                    PotController pot = _registeredPots[index];
                    if (pot != null && !pot.IsDestroyed && pot.HasGoldOrHarvesting)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasAnyGoldPot
        {
            get
            {
                RemoveNullEntries();
                for (int index = 0; index < _registeredPots.Count; index++)
                {
                    PotController pot = _registeredPots[index];
                    if (pot != null && !pot.IsDestroyed && pot.HasGold)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void RegisterPot(PotController pot)
        {
            if (pot == null || _registeredPots.Contains(pot))
            {
                return;
            }

            _registeredPots.Add(pot);
        }

        public void UnregisterPot(PotController pot)
        {
            if (pot != null)
            {
                _registeredPots.Remove(pot);
            }
        }

        public float StoreGold(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            RemoveNullEntries();
            float remaining = amount;
            float acceptedTotal = 0f;
            for (int index = 0; index < _registeredPots.Count && remaining > 0.0001f; index++)
            {
                PotController pot = _registeredPots[index];
                if (pot == null || pot.IsDestroyed)
                {
                    continue;
                }

                float accepted = pot.TryStoreGold(remaining);
                acceptedTotal += accepted;
                remaining -= accepted;
            }

            return acceptedTotal;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (TotalStoredGoldValue < amount)
            {
                return false;
            }

            float remaining = amount;
            RemoveNullEntries();
            for (int index = 0; index < _registeredPots.Count && remaining > 0.0001f; index++)
            {
                PotController pot = _registeredPots[index];
                if (pot == null || pot.IsDestroyed)
                {
                    continue;
                }

                remaining -= pot.DrainGold(remaining);
            }

            return remaining <= 0.0001f;
        }

        private void Awake()
        {
            RegisterWithProjectContext();
        }

        private void OnEnable()
        {
            RegisterWithProjectContext();
        }

        private void RegisterWithProjectContext()
        {
            if (ProjectContext.Instance?.ServiceRegistry != null)
            {
                ProjectContext.Instance.ServiceRegistry.Register(this, replaceExisting: true);
            }
        }

        private void RemoveNullEntries()
        {
            for (int index = _registeredPots.Count - 1; index >= 0; index--)
            {
                if (_registeredPots[index] == null)
                {
                    _registeredPots.RemoveAt(index);
                }
            }
        }
    }
}