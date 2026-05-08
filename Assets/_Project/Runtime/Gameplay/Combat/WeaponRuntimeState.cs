using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.Utilities;

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
            _config = config;
            _cooldownTimer.Reset();
        }

        public void Tick(float deltaTime)
        {
            _cooldownTimer.Tick(deltaTime);
        }

        public bool TryConsumeShot()
        {
            if (_config == null || _cooldownTimer.IsRunning)
            {
                return false;
            }

            _cooldownTimer.Start(_config.CooldownSeconds);
            return true;
        }

        public void Reset()
        {
            _cooldownTimer.Reset();
        }
    }
}