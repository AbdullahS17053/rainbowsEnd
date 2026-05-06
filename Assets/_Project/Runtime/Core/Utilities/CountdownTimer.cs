using UnityEngine;

namespace RainbowsEnd.Core.Utilities
{
    public sealed class CountdownTimer
    {
        public float DurationSeconds { get; private set; }

        public float RemainingSeconds { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsComplete => !IsRunning && RemainingSeconds <= 0f && DurationSeconds > 0f;

        public float ElapsedNormalized
        {
            get
            {
                if (DurationSeconds <= 0f)
                {
                    return 1f;
                }

                return 1f - Mathf.Clamp01(RemainingSeconds / DurationSeconds);
            }
        }

        public void Start(float durationSeconds)
        {
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            RemainingSeconds = DurationSeconds;
            IsRunning = DurationSeconds > 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
            if (RemainingSeconds <= 0f)
            {
                IsRunning = false;
            }
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Reset()
        {
            DurationSeconds = 0f;
            RemainingSeconds = 0f;
            IsRunning = false;
        }
    }
}