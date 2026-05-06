namespace RainbowsEnd.Core.Common
{
    public readonly struct DamageResult
    {
        public DamageResult(float appliedAmount, float currentHealth, float normalizedHealth, bool isDepleted)
        {
            AppliedAmount = appliedAmount;
            CurrentHealth = currentHealth;
            NormalizedHealth = normalizedHealth;
            IsDepleted = isDepleted;
        }

        public float AppliedAmount { get; }

        public float CurrentHealth { get; }

        public float NormalizedHealth { get; }

        public bool IsDepleted { get; }
    }
}