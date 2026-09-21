using System;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.UI;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public enum KillstreakTier
    {
        None = 0,
        WarmUp = 1,
        DoubleTrouble = 1,
        OnFire = 2,
        PotStirrer = 2,
        JustOneMan = 3,
        WeeFury = 4,
        SwiftlyGoRun = 5,
        RainbowsEnd = 6,
        ZoneOfDanger = 7,
        UnicornsWake = 8
    }

    public sealed class KillstreakManager : MonoBehaviour
    {
        private static KillstreakManager _instance;

        [SerializeField] private float streakDecaySeconds = 3.0f;
        [SerializeField] private PlayerRoot playerRoot;

        private int _currentKillsInStreak;
        private int _currentTier;
        private float _streakTimer;
        private bool _previousStreakFailed;
        private bool _isStreakActive;

        public static event Action<int, string> KillstreakTierReached; // (tier, title)
        public static event Action KillstreakReset;

        public static KillstreakManager Instance => _instance;

        public int CurrentStreak => _currentKillsInStreak;
        public KillstreakTier CurrentTier => (KillstreakTier)_currentTier;
        public int CurrentTierIndex => _currentTier;
        public bool IsStreakActive => _isStreakActive;

        public void RecordKill(Vector3 enemyPosition) => RegisterKill(enemyPosition);

        public float SaturationRadiusMultiplier => _currentTier switch
        {
            >= 7 => 3.0f,
            >= 5 => 2.0f,
            >= 3 => 1.5f,
            _ => 1.0f
        };

        public float SaturationBoost => _isStreakActive ? Mathf.Clamp01(0.04f * (_currentTier + 1)) : 0f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolvePlayer();
        }

        private void OnEnable()
        {
            ResolvePlayer();
        }

        private void Update()
        {
            if (!_isStreakActive)
            {
                return;
            }

            _streakTimer -= Time.deltaTime;
            if (_streakTimer <= 0f)
            {
                EndStreak();
            }
        }

        public void RegisterKill(Vector3 enemyPosition)
        {
            ResolvePlayer();
            Vector3 calloutPosition = playerRoot != null ? playerRoot.transform.position : enemyPosition;

            if (!_isStreakActive)
            {
                _isStreakActive = true;
                _currentKillsInStreak = 1;
                _streakTimer = streakDecaySeconds;

                // Check 13% chance for Lucky Shot if previous streak failed
                if (_previousStreakFailed && UnityEngine.Random.value < 0.13f)
                {
                    TriggerTier(0, "Lucky Shot", calloutPosition, scale: 1.1f);
                }

                _previousStreakFailed = false;
                return;
            }

            _currentKillsInStreak++;
            _streakTimer = streakDecaySeconds;

            int newTier = ResolveTierForKills(_currentKillsInStreak);
            if (newTier > _currentTier)
            {
                _currentTier = newTier;
                string title = GetTierTitle(_currentTier);
                float scale = 1.0f + (_currentTier * 0.15f);

                if (_currentTier >= 7)
                {
                    CombatFeedbackController.Instance?.TriggerHitStop(0.08f);
                }

                TriggerTier(_currentTier, title, calloutPosition, scale);
            }
        }

        private void TriggerTier(int tier, string title, Vector3 position, float scale)
        {
            FloatingNumberSpawner.Instance?.SpawnKillstreakCallout(position, title, scale);
            KillstreakTierReached?.Invoke(tier, title);
        }

        private void EndStreak()
        {
            _previousStreakFailed = _currentKillsInStreak >= 2;
            _isStreakActive = false;
            _currentKillsInStreak = 0;
            _currentTier = 0;
            _streakTimer = 0f;
            KillstreakReset?.Invoke();
        }

        private static int ResolveTierForKills(int kills)
        {
            if (kills >= 12) return 8; // Unicorn's Wake
            if (kills >= 10) return 7; // Zone of Danger
            if (kills >= 8)  return 6; // Rainbow's End
            if (kills >= 6)  return 5; // Swiftly, Go. Run!
            if (kills >= 5)  return 4; // Wee Fury
            if (kills >= 4)  return 3; // Just One Man
            if (kills >= 3)  return 2; // Pot Stirrer
            if (kills >= 2)  return 1; // Double Trouble
            return 0;
        }

        private static string GetTierTitle(int tier)
        {
            return tier switch
            {
                1 => "Double Trouble",
                2 => "Pot Stirrer",
                3 => "Just One Man",
                4 => "Wee Fury",
                5 => "Swiftly, Go. Run!",
                6 => "Rainbow's End",
                7 => "Zone of Danger",
                8 => "Unicorn's Wake",
                _ => "Lucky Shot"
            };
        }

        private void ResolvePlayer()
        {
            if (playerRoot == null)
            {
                playerRoot = FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Exclude);
            }
        }
    }
}
