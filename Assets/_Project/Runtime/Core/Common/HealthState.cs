using UnityEngine;

namespace RainbowsEnd.Core.Common
{
    public sealed class HealthState
    {
        public float CurrentHealth { get; private set; }

        public float MaxHealth { get; private set; }

        public bool IsDepleted => CurrentHealth <= 0f;

        public float NormalizedHealth => MaxHealth <= 0f ? 0f : CurrentHealth / MaxHealth;

        public void Initialize(float maxHealth, bool refillCurrent = true)
        {
            MaxHealth = Mathf.Max(0.01f, maxHealth);

            if (refillCurrent)
            {
                CurrentHealth = MaxHealth;
                return;
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
        }

        public DamageResult ApplyDamage(float amount)
        {
            if (amount <= 0f || IsDepleted)
            {
                return new DamageResult(0f, CurrentHealth, NormalizedHealth, IsDepleted);
            }

            float clampedAmount = Mathf.Min(CurrentHealth, amount);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            return new DamageResult(clampedAmount, CurrentHealth, NormalizedHealth, IsDepleted);
        }

        public float Restore(float amount)
        {
            if (amount <= 0f || MaxHealth <= 0f)
            {
                return 0f;
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
            return CurrentHealth - previousHealth;
        }

        public void ResetToFull()
        {
            CurrentHealth = MaxHealth;
        }
    }
}