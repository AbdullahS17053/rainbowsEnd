using System;
using System.Collections.Generic;
using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Orchestration
{
    public enum RunSpawnType
    {
        None = 0,
        Traveller = 1,
        Police = 2
    }

    public readonly struct RunWaveTickResult
    {
        public RunWaveTickResult(RunOutcome outcome, RunSpawnType spawnedType, bool waveAdvanced)
        {
            Outcome = outcome;
            SpawnedType = spawnedType;
            WaveAdvanced = waveAdvanced;
        }

        public RunOutcome Outcome { get; }

        public RunSpawnType SpawnedType { get; }

        public bool WaveAdvanced { get; }
    }

    public sealed class RunWaveController
    {
        private readonly WaveDefinition[] _waves;

        private int _currentWaveIndex;
        private int _spawnedTravellerCount;
        private int _spawnedPoliceCount;
        private float _spawnTimerSeconds;
        private bool _preferTraveller = true;
        private RunOutcome _outcome;

        public RunWaveController(IReadOnlyList<WaveDefinition> waves)
        {
            if (waves == null || waves.Count == 0)
            {
                _waves = Array.Empty<WaveDefinition>();
            }
            else
            {
                _waves = new WaveDefinition[waves.Count];
                for (int index = 0; index < waves.Count; index++)
                {
                    _waves[index] = waves[index];
                }
            }

            Reset();
        }

        public RunOutcome Outcome => _outcome;

        public int TotalWaveCount => _waves.Length;

        public int CurrentWaveIndex => Mathf.Clamp(_currentWaveIndex, 0, Mathf.Max(0, _waves.Length - 1));

        public int CurrentWaveNumber => _waves.Length == 0 ? 0 : CurrentWaveIndex + 1;

        public int CompletedWaveCount => _outcome == RunOutcome.Win ? _waves.Length : Mathf.Clamp(_currentWaveIndex, 0, _waves.Length);

        public int SpawnedTravellerCount => _spawnedTravellerCount;

        public int SpawnedPoliceCount => _spawnedPoliceCount;

        public void Reset()
        {
            _currentWaveIndex = 0;
            _spawnedTravellerCount = 0;
            _spawnedPoliceCount = 0;
            _preferTraveller = true;
            _outcome = RunOutcome.None;
            _spawnTimerSeconds = _waves.Length > 0 ? _waves[0].StartDelaySeconds : 0f;
        }

        public RunWaveTickResult Tick(
            float deltaTime,
            int activeTravellerCount,
            int activePoliceCount,
            int activePotCount,
            Func<bool> trySpawnTraveller,
            Func<bool> trySpawnPolice)
        {
            if (_outcome != RunOutcome.None)
            {
                return new RunWaveTickResult(_outcome, RunSpawnType.None, waveAdvanced: false);
            }

            if (activePotCount <= 0)
            {
                _outcome = RunOutcome.Lose;
                return new RunWaveTickResult(_outcome, RunSpawnType.None, waveAdvanced: false);
            }

            if (_waves.Length == 0)
            {
                _outcome = RunOutcome.Win;
                return new RunWaveTickResult(_outcome, RunSpawnType.None, waveAdvanced: false);
            }

            WaveDefinition currentWave = _waves[_currentWaveIndex];
            int activeEnemyCount = Mathf.Max(0, activeTravellerCount) + Mathf.Max(0, activePoliceCount);

            if (HasSpawnedCurrentWave(currentWave))
            {
                if (activeEnemyCount > 0)
                {
                    return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: false);
                }

                _currentWaveIndex++;
                if (_currentWaveIndex >= _waves.Length)
                {
                    _outcome = RunOutcome.Win;
                    return new RunWaveTickResult(_outcome, RunSpawnType.None, waveAdvanced: true);
                }

                StartCurrentWave();
                return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: true);
            }

            if (activeEnemyCount >= currentWave.MaxConcurrentEnemies)
            {
                return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: false);
            }

            _spawnTimerSeconds -= Mathf.Max(0f, deltaTime);
            if (_spawnTimerSeconds > 0f)
            {
                return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: false);
            }

            RunSpawnType nextSpawnType = SelectNextSpawnType(currentWave);
            bool spawned = nextSpawnType switch
            {
                RunSpawnType.Traveller => trySpawnTraveller?.Invoke() == true,
                RunSpawnType.Police => trySpawnPolice?.Invoke() == true,
                _ => false
            };

            if (!spawned)
            {
                return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: false);
            }

            RecordSpawn(currentWave, nextSpawnType);
            _spawnTimerSeconds = currentWave.SpawnIntervalSeconds;
            return new RunWaveTickResult(RunOutcome.None, nextSpawnType, waveAdvanced: false);
        }

        private void StartCurrentWave()
        {
            _spawnedTravellerCount = 0;
            _spawnedPoliceCount = 0;
            _preferTraveller = true;
            _spawnTimerSeconds = _waves[_currentWaveIndex].StartDelaySeconds;
        }

        private bool HasSpawnedCurrentWave(WaveDefinition wave)
        {
            return _spawnedTravellerCount >= wave.TravellerCount &&
                _spawnedPoliceCount >= wave.PoliceCount;
        }

        private RunSpawnType SelectNextSpawnType(WaveDefinition wave)
        {
            int remainingTravellers = wave.TravellerCount - _spawnedTravellerCount;
            int remainingPolice = wave.PoliceCount - _spawnedPoliceCount;

            if (remainingTravellers <= 0)
            {
                return remainingPolice > 0 ? RunSpawnType.Police : RunSpawnType.None;
            }

            if (remainingPolice <= 0)
            {
                return RunSpawnType.Traveller;
            }

            return _preferTraveller ? RunSpawnType.Traveller : RunSpawnType.Police;
        }

        private void RecordSpawn(WaveDefinition wave, RunSpawnType spawnedType)
        {
            switch (spawnedType)
            {
                case RunSpawnType.Traveller:
                    _spawnedTravellerCount++;
                    _preferTraveller = _spawnedPoliceCount < wave.PoliceCount ? false : true;
                    break;
                case RunSpawnType.Police:
                    _spawnedPoliceCount++;
                    _preferTraveller = _spawnedTravellerCount < wave.TravellerCount;
                    break;
            }
        }
    }
}
