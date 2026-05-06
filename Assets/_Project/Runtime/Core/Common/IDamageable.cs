namespace RainbowsEnd.Core.Common
{
    public interface IDamageable
    {
        float CurrentHealth { get; }

        float MaxHealth { get; }

        bool IsDepleted { get; }

        DamageResult ApplyDamage(in DamageInfo damageInfo);

        float Restore(float amount);
    }
}