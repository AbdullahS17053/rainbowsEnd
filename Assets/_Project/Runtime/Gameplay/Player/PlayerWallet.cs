using System;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.UI;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField] private int initialGold = 0;

        private int _currentBankedGold;
        private int _totalGoldEarned;

        public event Action<int> GoldChanged;
        public event Action<int> GoldBanked;
        public event Action<int> GoldSpent;

        public int CurrentBankedGold => _currentBankedGold;
        public int SpendableGold => _currentBankedGold;
        public int TotalGoldEarned => _totalGoldEarned;
        public int TotalEarnings => _totalGoldEarned;
        public int HighScoreGold => _totalGoldEarned;
        public int SavedHighScore => RunOutcomePresenter.GetSavedHighScore();

        public bool TrySpendGold(int amount) => TrySpend(amount);

        private void Awake()
        {
            _currentBankedGold = Mathf.Max(0, initialGold);
            _totalGoldEarned = _currentBankedGold;

            if (ProjectContext.Instance?.ServiceRegistry != null)
            {
                ProjectContext.Instance.ServiceRegistry.Register(this, replaceExisting: true);
            }
        }

        public void BankGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentBankedGold += amount;
            _totalGoldEarned += amount;
            GoldBanked?.Invoke(amount);
            GoldChanged?.Invoke(_currentBankedGold);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (_currentBankedGold < amount)
            {
                return false;
            }

            _currentBankedGold -= amount;
            GoldSpent?.Invoke(amount);
            GoldChanged?.Invoke(_currentBankedGold);
            return true;
        }

        public bool CanAfford(int amount)
        {
            return amount <= 0 || _currentBankedGold >= amount;
        }

        public void ResetWallet()
        {
            _currentBankedGold = Mathf.Max(0, initialGold);
            _totalGoldEarned = _currentBankedGold;
            GoldChanged?.Invoke(_currentBankedGold);
        }
    }
}
