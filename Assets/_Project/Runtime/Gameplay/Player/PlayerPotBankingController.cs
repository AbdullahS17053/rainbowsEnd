using System;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    [RequireComponent(typeof(PlayerRoot))]
    public sealed class PlayerPotBankingController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerPotCarryController carryController;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] [Min(0.5f)] private float channelDurationSeconds = 2.0f;
        [SerializeField] [Min(0.5f)] private float maxInteractionDistance = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip bankingChantClip;
        [SerializeField] private AudioClip bankingCompleteClip;
        [SerializeField] private AudioClip coughSputterClip;

        private PotController _targetPot;
        private float _currentChannelTime;
        private bool _isBanking;
        private Vector2 _bankingStartPosition;

        public event Action<PotController> BankingStarted;
        public event Action<PotController, float> BankingProgress; // 0.0 to 1.0
        public event Action<PotController, int> BankingCompleted;
        public event Action<PotController> BankingInterrupted;

        public bool IsBanking => _isBanking;
        public float CurrentProgress => _isBanking && channelDurationSeconds > 0f ? Mathf.Clamp01(_currentChannelTime / channelDurationSeconds) : 0f;
        public PotController TargetPot => _targetPot;

        public void Configure(PlayerRoot root, PlayerInputReader reader, PlayerPotCarryController potCarry, PlayerWallet playerWallet)
        {
            playerRoot = root;
            inputReader = reader;
            carryController = potCarry;
            wallet = playerWallet;
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            if (playerRoot?.Health != null)
            {
                playerRoot.Health.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (playerRoot?.Health != null)
            {
                playerRoot.Health.Damaged -= HandleDamaged;
            }

            if (_isBanking)
            {
                CancelBanking(interrupted: false);
            }
        }

        private void Update()
        {
            if (playerRoot == null || playerRoot.IsDisabled || inputReader == null)
            {
                if (_isBanking)
                {
                    CancelBanking(interrupted: true);
                }
                return;
            }

            if (_isBanking)
            {
                TickBanking();
                return;
            }

            if (inputReader.DrinkGoldPressedThisFrame)
            {
                TryStartBanking();
            }
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            carryController ??= GetComponent<PlayerPotCarryController>();
            wallet ??= GetComponent<PlayerWallet>();
        }

        private void TryStartBanking()
        {
            PotController pot = ResolveTargetPot();
            if (pot == null || pot.IsDestroyed || pot.StoredGold < 1f)
            {
                return;
            }

            _targetPot = pot;
            _isBanking = true;
            _currentChannelTime = 0f;
            _bankingStartPosition = playerRoot.Body != null ? playerRoot.Body.position : (Vector2)transform.position;

            RainbowsEnd.Audio.AudioManager.Instance?.PlayPlayerAction(bankingChantClip, 0.8f);
            BankingStarted?.Invoke(_targetPot);
            BankingProgress?.Invoke(_targetPot, 0f);
        }

        private void TickBanking()
        {
            if (_targetPot == null || _targetPot.IsDestroyed || _targetPot.StoredGold < 1f)
            {
                CancelBanking(interrupted: true);
                return;
            }

            // Key must remain held
            if (!inputReader.DrinkGoldHeld)
            {
                CancelBanking(interrupted: true);
                return;
            }

            // Check player movement interruption
            Vector2 currentPosition = playerRoot.Body != null ? playerRoot.Body.position : (Vector2)transform.position;
            if (Vector2.Distance(currentPosition, _bankingStartPosition) > 0.25f || inputReader.MoveInput.sqrMagnitude > 0.05f)
            {
                CancelBanking(interrupted: true);
                return;
            }

            _currentChannelTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_currentChannelTime / channelDurationSeconds);
            BankingProgress?.Invoke(_targetPot, progress);

            if (_currentChannelTime >= channelDurationSeconds)
            {
                CompleteBanking();
            }
        }

        private void CompleteBanking()
        {
            if (_targetPot == null)
            {
                CancelBanking(interrupted: false);
                return;
            }

            int goldBanked = _targetPot.BankAllGold();
            if (goldBanked > 0 && wallet != null)
            {
                wallet.BankGold(goldBanked);
            }

            PotController completedPot = _targetPot;
            if (goldBanked > 0)
            {
                UI.FloatingNumberSpawner.Instance?.SpawnGoldNumber(completedPot.transform.position + new Vector3(0f, 0.8f, 0f), goldBanked, isGain: true);
            }

            _isBanking = false;
            _targetPot = null;
            _currentChannelTime = 0f;

            RainbowsEnd.Audio.AudioManager.Instance?.PlayPlayerAction(bankingCompleteClip, 1.0f);
            BankingCompleted?.Invoke(completedPot, goldBanked);
        }

        public void CancelBanking(bool interrupted)
        {
            if (!_isBanking)
            {
                return;
            }

            PotController interruptedPot = _targetPot;
            _isBanking = false;
            _targetPot = null;
            _currentChannelTime = 0f;

            if (interrupted)
            {
                RainbowsEnd.Audio.AudioManager.Instance?.PlayPlayerAction(coughSputterClip, 0.9f);
                BankingInterrupted?.Invoke(interruptedPot);
            }
        }

        private void HandleDamaged(RainbowsEnd.Core.Common.DamageResult result)
        {
            if (_isBanking)
            {
                CancelBanking(interrupted: true);
            }
        }

        private PotController ResolveTargetPot()
        {
            if (carryController != null && carryController.IsCarryingPot && carryController.CarriedPot != null)
            {
                return carryController.CarriedPot;
            }

            Vector2 playerPos = playerRoot.Body != null ? playerRoot.Body.position : (Vector2)transform.position;
            PotController[] allPots = FindObjectsByType<PotController>(FindObjectsSortMode.None);
            PotController nearest = null;
            float nearestDistSqr = maxInteractionDistance * maxInteractionDistance;

            for (int i = 0; i < allPots.Length; i++)
            {
                PotController pot = allPots[i];
                if (pot == null || pot.IsDestroyed || pot.IsBeingStolenByMilitary || pot.StoredGold < 1f)
                {
                    continue;
                }

                float distSqr = ((Vector2)pot.transform.position - playerPos).sqrMagnitude;
                if (distSqr <= nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = pot;
                }
            }

            return nearest;
        }
    }
}
