using System;
using RainbowsEnd.Audio;
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

        [Header("Audio Escalation Cues")]
        [SerializeField] private AudioCueSO tier0To2Cue; // Cue_Gold_Pickup
        [SerializeField] private AudioCueSO tier3To4Cue; // Cue_Pot_GoldBanked
        [SerializeField] private AudioCueSO tier5To6Cue; // Cue_Pot_TierUpgrade
        [SerializeField] private AudioCueSO tier7To8Cue; // Cue_Banking_Complete

        private int _currentKillsInStreak;
        private int _currentTier;
        private float _streakTimer;
        private bool _previousStreakFailed;
        private bool _isStreakActive;

        public static event Action<int, string> KillstreakTierReached; // (tier, title)
        public static event Action KillstreakReset;

        public static KillstreakManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<KillstreakManager>();
                    if (_instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("KillstreakManager");
                        _instance = go.AddComponent<KillstreakManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance == null && Application.isPlaying)
            {
                _ = Instance;
            }
        }

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

        public float CurrentSaturationRadius
        {
            get
            {
                ResolvePlayer();
                float playerWidth = playerRoot != null && playerRoot.GetComponent<Collider2D>() != null
                    ? playerRoot.GetComponent<Collider2D>().bounds.size.x
                    : 0.8f;
                return SaturationRadiusMultiplier * playerWidth;
            }
        }

        public float SaturationBoost => _isStreakActive ? Mathf.Clamp01(0.04f * (_currentTier + 1)) : 0f;

        public float GetEffectiveSaturationBoost(Vector3 position)
        {
            float streakBoost = 0f;
            if (_isStreakActive)
            {
                ResolvePlayer();
                Vector3 playerPos = playerRoot != null ? playerRoot.transform.position : position;
                float distToPlayer = Vector2.Distance(position, playerPos);
                if (distToPlayer <= CurrentSaturationRadius)
                {
                    streakBoost = SaturationBoost;
                }
            }

            float potBoost = 0f;
            Pots.PotController[] pots = FindObjectsByType<Pots.PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < pots.Length; i++)
            {
                Pots.PotController pot = pots[i];
                if (pot == null || pot.IsDestroyed) continue;
                float potRadius = pot.GetProximitySaturationRadius();
                float distToPot = Vector2.Distance(position, pot.transform.position);
                if (distToPot <= potRadius)
                {
                    potBoost = Mathf.Max(potBoost, pot.GetProximitySaturationBoost());
                }
            }

            return Mathf.Max(streakBoost, potBoost);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolvePlayer();
            ResolveAudioCues();
        }

        private void OnEnable()
        {
            ResolvePlayer();
            ResolveAudioCues();
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

                TriggerTier(_currentTier, title, calloutPosition, scale);
            }
        }

        private void TriggerTier(int tier, string title, Vector3 position, float scale)
        {
            Vector3 calloutPosition = playerRoot != null ? playerRoot.transform.position : position;

            switch (tier)
            {
                case 0: // Lucky Shot
                case 1: // Double Trouble
                case 2: // Pot Stirrer
                    FloatingNumberSpawner.Instance?.SpawnKillstreakCallout(calloutPosition, title, scale);
                    PlayTierAudio(tier, calloutPosition);
                    break;

                case 3: // Just One Man
                case 4: // Wee Fury
                    FloatingNumberSpawner.Instance?.SpawnKillstreakCallout(calloutPosition, title, scale);
                    ScreenEdgeBlurOverlay.Instance?.TriggerGoldFlash(0.28f);
                    PlayTierAudio(tier, calloutPosition);
                    break;

                case 5: // Swiftly, Go. Run!
                case 6: // Rainbow's End
                    FloatingNumberSpawner.Instance?.SpawnKillstreakCallout(calloutPosition, title, scale);
                    ScreenEdgeBlurOverlay.Instance?.TriggerEdgeGlowPulse(new Color(0.25f, 1f, 0.45f, 0.85f), 0.55f);
                    PlayTierAudio(tier, calloutPosition);
                    break;

                case 7: // Zone of Danger
                case 8: // Unicorn's Wake
                    FloatingNumberSpawner.Instance?.SpawnKillstreakBanner(title, scale * 1.4f);
                    FloatingNumberSpawner.Instance?.SpawnKillstreakCallout(calloutPosition, title, scale);
                    ScreenEdgeBlurOverlay.Instance?.TriggerEdgeGlowPulse(new Color(0.35f, 1f, 0.65f, 1f), 0.7f);
                    ScreenEdgeBlurOverlay.Instance?.TriggerGoldFlash(0.35f);
                    CombatFeedbackController.Instance?.TriggerHitStop(0.08f, 0.05f);
                    CombatFeedbackController.Instance?.PlayScreenShake(0.25f, 0.2f);
                    CombatFeedbackController.Instance?.TriggerParticleBurst(calloutPosition);
                    PlayTierAudio(tier, calloutPosition);
                    break;
            }

            KillstreakTierReached?.Invoke(tier, title);
        }

        private void PlayTierAudio(int tier, Vector3 position)
        {
            ResolveAudioCues();
            AudioCueSO cue = tier switch
            {
                >= 7 => tier7To8Cue,
                >= 5 => tier5To6Cue,
                >= 3 => tier3To4Cue,
                _ => tier0To2Cue
            };

            if (cue != null)
            {
                AudioManager.Instance?.PlayCue(cue, position);
            }
        }

        private void ResolveAudioCues()
        {
            if (tier0To2Cue == null)
            {
                Enemies.GoldPickup pickup = FindFirstObjectByType<Enemies.GoldPickup>(FindObjectsInactive.Include);
                if (pickup != null && pickup.PickupCue != null)
                {
                    tier0To2Cue = pickup.PickupCue;
                }
            }

            if (tier3To4Cue == null || tier5To6Cue == null)
            {
                Pots.PotController pot = FindFirstObjectByType<Pots.PotController>(FindObjectsInactive.Include);
                if (pot != null)
                {
                    if (tier3To4Cue == null && pot.GoldBankedCue != null)
                    {
                        tier3To4Cue = pot.GoldBankedCue;
                    }
                    if (tier5To6Cue == null && pot.TierUpgradeCue != null)
                    {
                        tier5To6Cue = pot.TierUpgradeCue;
                    }
                }
            }

            if (tier7To8Cue == null)
            {
                Player.PlayerPotBankingController banking = FindFirstObjectByType<Player.PlayerPotBankingController>(FindObjectsInactive.Include);
                if (banking != null && banking.BankingCompleteCue != null)
                {
                    tier7To8Cue = banking.BankingCompleteCue;
                }
            }
        }

        private void EndStreak()
        {
            _previousStreakFailed = _currentKillsInStreak >= 1;
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
