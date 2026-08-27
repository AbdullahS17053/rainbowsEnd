using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Shop;
using RainbowsEnd.Gameplay.Spells;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class TutorialSequenceController : MonoBehaviour
    {
        public const string OptOutPlayerPrefsKey = "RainbowsEnd.TutorialPromptsSuppressed";

        [SerializeField] private TutorialPromptView promptView;
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PotRegistry potRegistry;
        [SerializeField] private PortalRegistry portalRegistry;
        [SerializeField] private ShopController shopController;
        [SerializeField] private TutorialStepDefinition[] stepDefinitions = CreateDefaultStepDefinitions();

        private readonly TutorialSequenceState _sequenceState = new TutorialSequenceState();
        private bool _isInitialized;
        private bool _isSuppressed;
        private bool _definitionsAreValid;
        private bool _viewIsConfigured;

        public bool IsTutorialSuppressed => _isSuppressed;

        public bool IsFinished => _sequenceState.IsFinished;

        private void Awake()
        {
            ResolveReferences();
            _sequenceState.Reset();
            _isSuppressed = PlayerPrefs.GetInt(OptOutPlayerPrefsKey, 0) != 0;
            _definitionsAreValid = ValidateStepDefinitions();
            _isInitialized = true;

            if (_isSuppressed || !_definitionsAreValid)
            {
                promptView?.Hide();
                return;
            }

            ShowActiveStep();
        }

        private void Update()
        {
            if (!_isInitialized || _isSuppressed || !_definitionsAreValid || _sequenceState.IsFinished)
            {
                return;
            }

            ResolveReferences();

            TutorialStepId activeStep = _sequenceState.CurrentStep;
            if (!IsStepComplete(activeStep) || !_sequenceState.TryComplete(activeStep))
            {
                return;
            }

            if (_sequenceState.IsFinished)
            {
                promptView?.Hide();
                return;
            }

            ShowActiveStep();
        }

        public void SuppressTutorialPrompts()
        {
            if (_isSuppressed)
            {
                return;
            }

            _isSuppressed = true;
            PlayerPrefs.SetInt(OptOutPlayerPrefsKey, 1);
            PlayerPrefs.Save();
            _sequenceState.Reset();
            promptView?.Hide();
        }

        private void ResolveReferences()
        {
            promptView ??= GetComponentInChildren<TutorialPromptView>(true);
            if (promptView != null && !_viewIsConfigured)
            {
                promptView.Configure(SuppressTutorialPrompts);
                _viewIsConfigured = true;
            }

            playerRoot ??= FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);

            if (potRegistry == null && ProjectContext.Instance?.Services.TryGet(out PotRegistry potService) == true)
            {
                potRegistry = potService;
            }

            potRegistry ??= FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);

            if (portalRegistry == null && ProjectContext.Instance?.Services.TryGet(out PortalRegistry portalService) == true)
            {
                portalRegistry = portalService;
            }

            portalRegistry ??= FindFirstObjectByType<PortalRegistry>(FindObjectsInactive.Include);
            shopController ??= FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        }

        private bool IsStepComplete(TutorialStepId step)
        {
            switch (step)
            {
                case TutorialStepId.PickUpPot:
                    return playerRoot?.PotCarryController != null && playerRoot.PotCarryController.IsCarryingPot;

                case TutorialStepId.PlaceOnTrail:
                    return potRegistry != null && potRegistry.HasHarvestingPot;

                case TutorialStepId.PlacePortal:
                    return portalRegistry != null && portalRegistry.ActivePortals.Count > 0;

                case TutorialStepId.OpenUpgrades:
                    return shopController != null && shopController.IsShopOpen;

                default:
                    return false;
            }
        }

        private void ShowActiveStep()
        {
            if (promptView == null || _sequenceState.IsFinished)
            {
                return;
            }

            TutorialStepId activeStep = _sequenceState.CurrentStep;
            for (int index = 0; index < stepDefinitions.Length; index++)
            {
                TutorialStepDefinition definition = stepDefinitions[index];
                if (definition != null && definition.StepId == activeStep)
                {
                    promptView.Show(definition.Message);
                    return;
                }
            }

            promptView.Hide();
        }

        private bool ValidateStepDefinitions()
        {
            if (stepDefinitions == null || stepDefinitions.Length != TutorialSequenceState.OrderedSteps.Count)
            {
                Debug.LogError($"{nameof(TutorialSequenceController)} requires exactly four ordered step definitions.");
                return false;
            }

            for (int index = 0; index < stepDefinitions.Length; index++)
            {
                TutorialStepDefinition definition = stepDefinitions[index];
                if (definition == null
                    || definition.StepId != TutorialSequenceState.OrderedSteps[index]
                    || string.IsNullOrWhiteSpace(definition.Message))
                {
                    Debug.LogError($"{nameof(TutorialSequenceController)} step definitions must match the fixed tutorial order.");
                    return false;
                }
            }

            return true;
        }

        private static TutorialStepDefinition[] CreateDefaultStepDefinitions()
        {
            return new[]
            {
                new TutorialStepDefinition(TutorialStepId.PickUpPot, "Pick up and put down the Pot of Gold by pressing the 'E' button."),
                new TutorialStepDefinition(TutorialStepId.PlaceOnTrail, "Place the pot of gold on the Rainbow Trail to make gold. Protect what's yours."),
                new TutorialStepDefinition(TutorialStepId.PlacePortal, "Place a portal by right-clicking, only two portals at a time."),
                new TutorialStepDefinition(TutorialStepId.OpenUpgrades, "Spend/Upgrade by pressing the 'Q' button.")
            };
        }
    }
}