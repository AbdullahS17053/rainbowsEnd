using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.UI;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Orchestration
{
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private PoliceSpawner policeSpawner;
        [SerializeField] private MilitarySpawner militarySpawner;
        [SerializeField] private PotRegistry potRegistry;
        [SerializeField] private RunOutcomePresenter outcomePresenter;
        [SerializeField] private PlayerWallet playerWallet;
        [SerializeField] private bool startRunOnEnable = true;
        [SerializeField] private bool freezeTimeOnOutcome = true;

        private RunWaveController _waveController;
        private RunOutcome _reportedOutcome;

        public float ElapsedRunSeconds { get; private set; }

        public int KillCount { get; private set; }

        public int SuccessfulTheftEscapeCount { get; private set; }

        public RunOutcome Outcome => _waveController?.Outcome ?? RunOutcome.None;

        public int CurrentWaveNumber => _waveController?.CurrentWaveNumber ?? 0;

        public int TotalWaveCount => _waveController?.TotalWaveCount ?? 0;

        public void Configure(
            WaveConfig config,
            EnemySpawner travellerSpawner,
            PoliceSpawner rangedPoliceSpawner,
            MilitarySpawner rangedMilitarySpawner,
            PotRegistry registry,
            RunOutcomePresenter presenter)
        {
            Configure(config, travellerSpawner, rangedPoliceSpawner, rangedMilitarySpawner, registry, presenter, null);
        }

        public void Configure(
            WaveConfig config,
            EnemySpawner travellerSpawner,
            PoliceSpawner rangedPoliceSpawner,
            MilitarySpawner rangedMilitarySpawner,
            PotRegistry registry,
            RunOutcomePresenter presenter,
            PlayerWallet wallet)
        {
            waveConfig = config;
            enemySpawner = travellerSpawner;
            policeSpawner = rangedPoliceSpawner;
            militarySpawner = rangedMilitarySpawner;
            potRegistry = registry;
            outcomePresenter = presenter;
            if (wallet != null)
            {
                playerWallet = wallet;
            }
            InitializeRun();
        }

        private void Awake()
        {
            ResolveReferences();
            RegisterWithProjectContext();
        }

        private void OnEnable()
        {
            RegisterWithProjectContext();

            if (startRunOnEnable)
            {
                InitializeRun();
            }
        }

        private void Update()
        {
            if (_waveController == null)
            {
                InitializeRun();
            }

            ResolveReferences();

            if (_reportedOutcome == RunOutcome.None)
            {
                ElapsedRunSeconds += Time.deltaTime;
            }

            RunWaveTickResult tickResult = _waveController.Tick(
                Time.deltaTime,
                enemySpawner != null ? enemySpawner.ActiveTravellerCount : 0,
                policeSpawner != null ? policeSpawner.ActivePoliceCount : 0,
                militarySpawner != null ? militarySpawner.ActiveMilitaryCount : 0,
                ResolveActivePotCount(),
                ResolveAnyPotHasGoldOrHarvesting(),
                TrySpawnTraveller,
                TrySpawnPolice,
                TrySpawnMilitary);

            if (tickResult.Outcome != RunOutcome.None && tickResult.Outcome != _reportedOutcome)
            {
                ReportOutcome(tickResult.Outcome);
            }
        }

        private void InitializeRun()
        {
            ResolveReferences();
            _waveController = new RunWaveController(waveConfig != null ? waveConfig.Waves : null);
            _reportedOutcome = RunOutcome.None;
            ElapsedRunSeconds = 0f;
            KillCount = 0;
            SuccessfulTheftEscapeCount = 0;
            outcomePresenter?.Hide();
        }

        public void RecordKill(Vector3 enemyPosition = default)
        {
            if (_reportedOutcome != RunOutcome.None)
            {
                return;
            }

            KillCount++;
            Combat.KillstreakManager.Instance?.RegisterKill(enemyPosition);
        }

        public void RecordSuccessfulTheftEscape()
        {
            if (_reportedOutcome != RunOutcome.None)
            {
                return;
            }

            SuccessfulTheftEscapeCount++;
        }

        private void RegisterWithProjectContext()
        {
            if (ProjectContext.Instance?.ServiceRegistry != null)
            {
                ProjectContext.Instance.ServiceRegistry.Register(this, replaceExisting: true);
            }
        }

        private void ResolveReferences()
        {
            if (waveConfig == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                waveConfig = ProjectContext.Instance.ConfigRegistry.WaveConfig;
            }

            enemySpawner ??= FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
            policeSpawner ??= FindFirstObjectByType<PoliceSpawner>(FindObjectsInactive.Include);
            militarySpawner ??= FindFirstObjectByType<MilitarySpawner>(FindObjectsInactive.Include);

            if (potRegistry == null && ProjectContext.Instance?.Services.TryGet(out PotRegistry registry) == true)
            {
                potRegistry = registry;
            }

            potRegistry ??= FindFirstObjectByType<PotRegistry>(FindObjectsInactive.Include);
            outcomePresenter ??= FindFirstObjectByType<RunOutcomePresenter>(FindObjectsInactive.Include);

            if (playerWallet == null && ProjectContext.Instance?.Services.TryGet(out PlayerWallet wallet) == true)
            {
                playerWallet = wallet;
            }

            playerWallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        }

        private int ResolveActivePotCount()
        {
            if (potRegistry != null)
            {
                return potRegistry.ActivePotCount;
            }

            PotController[] pots = FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int activePotCount = 0;
            for (int index = 0; index < pots.Length; index++)
            {
                if (pots[index] != null && !pots[index].IsDestroyed)
                {
                    activePotCount++;
                }
            }

            return activePotCount;
        }

        private bool ResolveAnyPotHasGoldOrHarvesting()
        {
            if (potRegistry != null)
            {
                return potRegistry.HasAnyGoldOrHarvestingPot;
            }

            PotController[] pots = FindObjectsByType<PotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < pots.Length; index++)
            {
                if (pots[index] != null && pots[index].HasGoldOrHarvesting)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TrySpawnTraveller()
        {
            return enemySpawner != null && enemySpawner.TrySpawnTraveller();
        }

        private bool TrySpawnPolice()
        {
            return policeSpawner != null && policeSpawner.TrySpawnPolice();
        }

        private bool TrySpawnMilitary()
        {
            return militarySpawner != null && militarySpawner.TrySpawnMilitary();
        }

        public const int ScorePerKill = 100;
        public const int ScorePerGold = 50;
        public const int ScorePerCompletedWave = 500;

        public int CurrentScore => CalculateScore(
            KillCount,
            ResolveCurrentGold(),
            _waveController != null ? _waveController.CompletedWaveCount : 0);

        public static int CalculateScore(int kills, int gold, int completedWaves)
        {
            return Mathf.Max(0, (kills * ScorePerKill) + (gold * ScorePerGold) + (completedWaves * ScorePerCompletedWave));
        }

        private int ResolveCurrentGold()
        {
            int potGold = potRegistry != null ? potRegistry.TotalStoredGold : 0;
            int bankedGold = playerWallet != null ? playerWallet.TotalGoldEarned : 0;
            return Mathf.Max(0, potGold + bankedGold);
        }

        private int ResolveScore()
        {
            return CurrentScore;
        }

        private void ReportOutcome(RunOutcome outcome)
        {
            _reportedOutcome = outcome;
            int score = ResolveScore();
            outcomePresenter?.Show(
                outcome,
                _waveController != null ? _waveController.CompletedWaveCount : 0,
                _waveController != null ? _waveController.TotalWaveCount : 0,
                score);

            if (freezeTimeOnOutcome)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
