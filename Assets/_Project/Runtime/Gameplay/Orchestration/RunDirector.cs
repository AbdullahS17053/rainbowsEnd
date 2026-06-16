using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Enemies;
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
        [SerializeField] private bool startRunOnEnable = true;
        [SerializeField] private bool freezeTimeOnOutcome = true;

        private RunWaveController _waveController;
        private RunOutcome _reportedOutcome;

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
            waveConfig = config;
            enemySpawner = travellerSpawner;
            policeSpawner = rangedPoliceSpawner;
            militarySpawner = rangedMilitarySpawner;
            potRegistry = registry;
            outcomePresenter = presenter;
            InitializeRun();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
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

            RunWaveTickResult tickResult = _waveController.Tick(
                Time.deltaTime,
                enemySpawner != null ? enemySpawner.ActiveTravellerCount : 0,
                policeSpawner != null ? policeSpawner.ActivePoliceCount : 0,
                militarySpawner != null ? militarySpawner.ActiveMilitaryCount : 0,
                ResolveActivePotCount(),
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
            outcomePresenter?.Hide();
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

        private void ReportOutcome(RunOutcome outcome)
        {
            _reportedOutcome = outcome;
            outcomePresenter?.Show(
                outcome,
                _waveController != null ? _waveController.CompletedWaveCount : 0,
                _waveController != null ? _waveController.TotalWaveCount : 0);

            if (freezeTimeOnOutcome)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
