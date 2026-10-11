using System;
using RainbowsEnd.Audio;
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
        [SerializeField] private Animator feedbackAnimator;
        [SerializeField] private bool requireHoldKey = false;
        [SerializeField] [Min(0.5f)] private float channelDurationSeconds = 2.0f;
        [SerializeField] [Min(0.5f)] private float maxInteractionDistance = 2.5f;

        private static readonly int DrinkTriggerHash = Animator.StringToHash("Drink");
        private static readonly int IsBankingBoolHash = Animator.StringToHash("IsBanking");

        [Header("Audio (AudioCueSO Preferred)")]
        [SerializeField] private AudioCueSO bankingChantCue;
        [SerializeField] private AudioCueSO bankingCompleteCue;
        [SerializeField] private AudioCueSO coughSputterCue;

        [Header("Audio Clips (Fallback)")]
        [SerializeField] private AudioClip bankingChantClip;
        [SerializeField] private AudioClip bankingCompleteClip;
        [SerializeField] private AudioClip coughSputterClip;

        public AudioCueSO BankingCompleteCue => bankingCompleteCue;

        public void ConfigureAudioCues(AudioCueSO chant, AudioCueSO complete, AudioCueSO cough)
        {
            if (chant != null) bankingChantCue = chant;
            if (complete != null) bankingCompleteCue = complete;
            if (cough != null) coughSputterCue = cough;
        }

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

        public void Configure(PlayerRoot root, PlayerInputReader reader, PlayerPotCarryController potCarry, PlayerWallet playerWallet, Animator animator = null)
        {
            playerRoot = root;
            inputReader = reader;
            carryController = potCarry;
            wallet = playerWallet;
            if (animator != null)
            {
                feedbackAnimator = animator;
            }
            else if (playerRoot != null && feedbackAnimator == null)
            {
                feedbackAnimator = playerRoot.FeedbackAnimator;
            }
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
            if (feedbackAnimator == null && playerRoot != null)
            {
                feedbackAnimator = playerRoot.FeedbackAnimator;
            }
            feedbackAnimator ??= GetComponentInChildren<Animator>();
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

            if (feedbackAnimator != null)
            {
                feedbackAnimator.SetBool(IsBankingBoolHash, true);
                feedbackAnimator.SetTrigger(DrinkTriggerHash);
            }

            if (bankingChantCue != null)
            {
                RainbowsEnd.Audio.AudioManager.Instance?.PlayCue(bankingChantCue, _bankingStartPosition);
            }
            else if (bankingChantClip != null)
            {
                RainbowsEnd.Audio.AudioManager.Instance?.PlayPlayerAction(bankingChantClip, 0.8f);
            }
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

            // Key must remain held only when requireHoldKey is enabled
            if (requireHoldKey && !inputReader.DrinkGoldHeld)
            {
                CancelBanking(interrupted: true);
                return;
            }

            // Check player movement interruption
            Vector2 currentPosition = playerRoot.Body != null ? playerRoot.Body.position : (Vector2)transform.position;
            if (Vector2.Distance(currentPosition, _bankingStartPosition) > 0.5f || inputReader.MoveInput.sqrMagnitude > 0.05f)
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

            if (feedbackAnimator != null)
            {
                feedbackAnimator.SetBool(IsBankingBoolHash, false);
                feedbackAnimator.ResetTrigger(DrinkTriggerHash);
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

            if (bankingCompleteCue != null)
            {
                RainbowsEnd.Audio.AudioManager.Instance?.PlayCue(bankingCompleteCue, completedPot.transform.position);
            }
            else if (bankingCompleteClip != null)
            {
                RainbowsEnd.Audio.AudioManager.Instance?.PlayPlayerAction(bankingCompleteClip, 1.0f);
            }
            BankingCompleted?.Invoke(completedPot, goldBanked);
        }

        public void CancelBanking(bool interrupted)
        {
            if (!_isBanking)
            {
                return;
            }

            if (feedbackAnimator != null)
            {
                feedbackAnimator.SetBool(IsBankingBoolHash, false);
                feedbackAnimator.ResetTrigger(DrinkTriggerHash);
            }

            PotController interruptedPot = _targetPot;
            _isBanking = false;
            _targetPot = null;
            _currentChannelTime = 0f;

            if (interrupted)
            {
                if (coughSputterCue != null)
                {
                    RainbowsEnd.Audio.AudioManager.Instance?.PlayCue(coughSputterCue, transform.position);
                }
                else if (coughSputterClip != null)
                {
                    RainbowsEnd.Audio.AudioManager.Instance?.PlayPlayerAction(coughSputterClip, 0.9f);
                }
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
