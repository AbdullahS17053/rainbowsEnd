using RainbowsEnd.Core.Utilities;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Pots
{
    public sealed class PotHarvestState
    {
        private readonly CountdownTimer _cancelTimer = new CountdownTimer();
        private readonly CountdownTimer _harvestDurationTimer = new CountdownTimer();

        public PotHarvestStatus Status { get; private set; } = PotHarvestStatus.Idle;

        public bool HasCapturedTrail { get; private set; }

        public float CapturedTrailTimestampSeconds { get; private set; }

        public float BaseHarvestGoldPerSecond { get; private set; }

        public bool IsTimedHarvestActive => _harvestDurationTimer.IsRunning;

        public float HarvestRemainingSeconds => _harvestDurationTimer.IsRunning ? _harvestDurationTimer.RemainingSeconds : 0f;

        public float HarvestElapsedNormalized => _harvestDurationTimer.ElapsedNormalized;

        public void CaptureTrail(float trailTimestampSeconds, float baseHarvestGoldPerSecond, float harvestDurationSeconds)
        {
            if (Status == PotHarvestStatus.Destroyed)
            {
                return;
            }

            HasCapturedTrail = true;
            CapturedTrailTimestampSeconds = trailTimestampSeconds;
            BaseHarvestGoldPerSecond = Mathf.Max(0f, baseHarvestGoldPerSecond);
            _harvestDurationTimer.Start(Mathf.Max(0.01f, harvestDurationSeconds));
        }

        public void ReleaseTrail()
        {
            if (Status == PotHarvestStatus.Destroyed)
            {
                return;
            }

            _harvestDurationTimer.Reset();
            HasCapturedTrail = false;
            CapturedTrailTimestampSeconds = 0f;
            BaseHarvestGoldPerSecond = 0f;
            _cancelTimer.Reset();
            Status = PotHarvestStatus.Idle;
        }

        public void UpdateState(float deltaTime, bool isMoving, float harvestCancelDelaySeconds)
        {
            if (Status == PotHarvestStatus.Destroyed)
            {
                return;
            }

            _harvestDurationTimer.Tick(deltaTime);

            if (_harvestDurationTimer.IsComplete)
            {
                HasCapturedTrail = false;
                CapturedTrailTimestampSeconds = 0f;
                BaseHarvestGoldPerSecond = 0f;
                _cancelTimer.Reset();
                Status = PotHarvestStatus.Idle;
                return;
            }

            if (!HasCapturedTrail)
            {
                _cancelTimer.Reset();
                Status = PotHarvestStatus.Idle;
                return;
            }

            if (isMoving)
            {
                if (harvestCancelDelaySeconds <= 0f)
                {
                    _cancelTimer.Reset();
                    Status = PotHarvestStatus.Interrupted;
                    return;
                }

                if (!_cancelTimer.IsRunning)
                {
                    _cancelTimer.Start(harvestCancelDelaySeconds);
                }
                else
                {
                    _cancelTimer.Tick(deltaTime);
                }

                Status = _cancelTimer.IsComplete ? PotHarvestStatus.Interrupted : PotHarvestStatus.Harvesting;

                return;
            }

            _cancelTimer.Reset();
            Status = PotHarvestStatus.Harvesting;
        }

        public float GenerateGold(float deltaTime, float harvestMultiplier)
        {
            if (Status != PotHarvestStatus.Harvesting || !HasCapturedTrail)
            {
                return 0f;
            }

            return Mathf.Max(0f, BaseHarvestGoldPerSecond) * Mathf.Max(0f, harvestMultiplier) * Mathf.Max(0f, deltaTime);
        }

        public void MarkDestroyed()
        {
            _harvestDurationTimer.Reset();
            HasCapturedTrail = false;
            CapturedTrailTimestampSeconds = 0f;
            BaseHarvestGoldPerSecond = 0f;
            _cancelTimer.Reset();
            Status = PotHarvestStatus.Destroyed;
        }
    }
}