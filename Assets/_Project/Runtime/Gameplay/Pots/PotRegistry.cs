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
                    if (_registeredPots[index] != null && !_registeredPots[index].IsDestroyed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int TotalStoredGold
        {
            get
            {
                RemoveNullEntries();

                int total = 0;
                for (int index = 0; index < _registeredPots.Count; index++)
                {
                    PotController pot = _registeredPots[index];
                    if (pot != null && !pot.IsDestroyed)
                    {
                        total += Mathf.FloorToInt(pot.StoredGold);
                    }
                }

                return total;
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
                    if (pot != null && pot.HasGoldOrHarvesting)
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
            if (pot == null)
            {
                return;
            }

            _registeredPots.Remove(pot);
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