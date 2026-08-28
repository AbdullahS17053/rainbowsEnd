using System;
using UnityEngine;

namespace RainbowsEnd.Core.Common
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] [Min(0.01f)] private float initialMaxHealth = 1f;

        private readonly HealthState _state = new HealthState();

        private bool _isInvulnerable;
        private bool _isInitialized;

        public event Action<DamageResult> Damaged;
        public event Action<float> Healed;
        public event Action Depleted;

        public float CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return _state.CurrentHealth;
            }
        }

        public float MaxHealth
        {
            get
            {
                EnsureInitialized();
                return _state.MaxHealth;
            }
        }

        public bool IsDepleted
        {
            get
            {
                EnsureInitialized();
                return _state.IsDepleted;
            }
        }

        public bool IsInvulnerable => _isInvulnerable;

        private void Awake()
        {
            Initialize(initialMaxHealth);
        }

        public void Initialize(float maxHealth)
        {
            _isInvulnerable = false;
            initialMaxHealth = Mathf.Max(0.01f, maxHealth);
            _state.Initialize(initialMaxHealth);
            _isInitialized = true;
        }

        public DamageResult ApplyDamage(in DamageInfo damageInfo)
        {
            EnsureInitialized();

            if (_isInvulnerable)
            {
                return new DamageResult(0f, _state.CurrentHealth, _state.NormalizedHealth, _state.IsDepleted);
            }

            DamageResult result = _state.ApplyDamage(damageInfo.Amount);
            if (result.AppliedAmount <= 0f)
            {
                return result;
            }

            Damaged?.Invoke(result);

            if (result.IsDepleted)
            {
                Depleted?.Invoke();
            }

            return result;
        }

        public void SetInvulnerable(bool isInvulnerable)
        {
            _isInvulnerable = isInvulnerable;
        }
        public float Restore(float amount)
        {
            EnsureInitialized();

            float restoredAmount = _state.Restore(amount);
            if (restoredAmount > 0f)
            {
                Healed?.Invoke(restoredAmount);
            }

            return restoredAmount;
        }

        public void ResetToFull()
        {
            EnsureInitialized();
            _state.ResetToFull();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            Initialize(initialMaxHealth);
        }
    }
}
