using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.Utilities;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class WeaponRuntimeState
    {
        private readonly CountdownTimer _cooldownTimer = new CountdownTimer();
        private WeaponConfig _config;

        public WeaponConfig Config => _config;

        public float CooldownRemainingSeconds => _cooldownTimer.RemainingSeconds;

        public bool IsReady => _config != null && !_cooldownTimer.IsRunning;

        public void Configure(WeaponConfig config)
        {
            AssignConfig(config, true);
        }

        public void AssignConfig(WeaponConfig config, bool resetCooldown)
        {
            _config = config;

            if (resetCooldown)
            {
                _cooldownTimer.Reset();
            }
        }

        public void Tick(float deltaTime)
        {
            _cooldownTimer.Tick(deltaTime);
        }

        public bool TryConsumeShot(float cooldownSeconds = -1f)
        {
            if (_config == null || _cooldownTimer.IsRunning)
            {
                return false;
            }

            float resolvedCooldownSeconds = cooldownSeconds > 0f ? cooldownSeconds : _config.CooldownSeconds;
            _cooldownTimer.Start(Mathf.Max(0.01f, resolvedCooldownSeconds));
            return true;
        }

        public void Reset()
        {
            _cooldownTimer.Reset();
        }
    }
}