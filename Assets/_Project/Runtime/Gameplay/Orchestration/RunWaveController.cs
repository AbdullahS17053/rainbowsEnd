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
        Police = 2,
        Military = 3
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
        private readonly bool _isEndless;

        private int _currentWaveIndex;
        private int _spawnedTravellerCount;
        private int _spawnedPoliceCount;
        private int _spawnedMilitaryCount;
        private float _spawnTimerSeconds;
        private RunSpawnType _nextSpawnType = RunSpawnType.Traveller;
        private RunOutcome _outcome;

        public RunWaveController(IReadOnlyList<WaveDefinition> waves)
            : this(waves, false)
        {
        }

        public RunWaveController(IReadOnlyList<WaveDefinition> waves, bool isEndless)
        {
            _isEndless = isEndless;
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

        public bool IsEndless => _isEndless;

        public RunOutcome Outcome => _outcome;

        public int TotalWaveCount => _isEndless ? int.MaxValue : _waves.Length;

        public int CurrentWaveIndex => _isEndless ? _currentWaveIndex : Mathf.Clamp(_currentWaveIndex, 0, Mathf.Max(0, _waves.Length - 1));

        public int CurrentWaveNumber => _waves.Length == 0 ? 0 : CurrentWaveIndex + 1;

        public int CompletedWaveCount => _isEndless ? _currentWaveIndex : (_outcome == RunOutcome.Win ? _waves.Length : Mathf.Clamp(_currentWaveIndex, 0, _waves.Length));

        public int SpawnedTravellerCount => _spawnedTravellerCount;

        public int SpawnedPoliceCount => _spawnedPoliceCount;

        public int SpawnedMilitaryCount => _spawnedMilitaryCount;

        public void Reset()
        {
            _currentWaveIndex = 0;
            _spawnedTravellerCount = 0;
            _spawnedPoliceCount = 0;
            _spawnedMilitaryCount = 0;
            _nextSpawnType = RunSpawnType.Traveller;
            _outcome = RunOutcome.None;
            WaveDefinition firstWave = GetCurrentWaveDefinition();
            _spawnTimerSeconds = _waves.Length > 0 ? firstWave.StartDelaySeconds : 0f;
        }

        public RunWaveTickResult Tick(
            float deltaTime,
            int activeTravellerCount,
            int activePoliceCount,
            int activeMilitaryCount,
            int activePotCount,
            bool anyPotHasGoldOrHarvesting,
            Func<bool> trySpawnTraveller,
            Func<bool> trySpawnPolice,
            Func<bool> trySpawnMilitary)
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

            WaveDefinition currentWave = GetCurrentWaveDefinition();
            int activeEnemyCount = Mathf.Max(0, activeTravellerCount) + Mathf.Max(0, activePoliceCount) + Mathf.Max(0, activeMilitaryCount);

            if (HasSpawnedCurrentWave(currentWave))
            {
                if (activeEnemyCount > 0)
                {
                    return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: false);
                }

                _currentWaveIndex++;
                if (!_isEndless && _currentWaveIndex >= _waves.Length)
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

            if (!anyPotHasGoldOrHarvesting)
            {
                return new RunWaveTickResult(RunOutcome.None, RunSpawnType.None, waveAdvanced: false);
            }

            RunSpawnType nextSpawnType = SelectNextSpawnType(currentWave);
            bool spawned = nextSpawnType switch
            {
                RunSpawnType.Traveller => trySpawnTraveller?.Invoke() == true,
                RunSpawnType.Police => trySpawnPolice?.Invoke() == true,
                RunSpawnType.Military => trySpawnMilitary?.Invoke() == true,
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
            _spawnedMilitaryCount = 0;
            _nextSpawnType = RunSpawnType.Traveller;
            WaveDefinition currentWave = GetCurrentWaveDefinition();
            _spawnTimerSeconds = currentWave.StartDelaySeconds;
        }

        private WaveDefinition GetCurrentWaveDefinition()
        {
            if (_waves.Length == 0)
            {
                return default;
            }

            if (_currentWaveIndex < _waves.Length)
            {
                return _waves[_currentWaveIndex];
            }

            WaveDefinition baseWave = _waves[_waves.Length - 1];
            int endlessCycle = _currentWaveIndex - _waves.Length + 1;
            return new WaveDefinition(
                $"Endless {endlessCycle}",
                Mathf.Max(1f, baseWave.StartDelaySeconds),
                Mathf.Max(0.75f, baseWave.SpawnIntervalSeconds - endlessCycle * 0.05f),
                Mathf.Min(25, baseWave.MaxConcurrentEnemies + endlessCycle),
                baseWave.TravellerCount + endlessCycle * 2,
                baseWave.PoliceCount + endlessCycle * 2,
                baseWave.MilitaryCount + endlessCycle);
        }

        private bool HasSpawnedCurrentWave(WaveDefinition wave)
        {
            return _spawnedTravellerCount >= wave.TravellerCount &&
                _spawnedPoliceCount >= wave.PoliceCount &&
                _spawnedMilitaryCount >= wave.MilitaryCount;
        }

private RunSpawnType SelectNextSpawnType(WaveDefinition wave)
        {
            int[] remaining =
            {
                wave.TravellerCount - _spawnedTravellerCount,
                wave.PoliceCount - _spawnedPoliceCount,
                wave.MilitaryCount - _spawnedMilitaryCount
            };

            for (int offset = 0; offset < remaining.Length; offset++)
            {
                int candidateIndex = ((int)_nextSpawnType - 1 + offset) % remaining.Length;
                if (remaining[candidateIndex] > 0)
                {
                    return (RunSpawnType)(candidateIndex + 1);
                }
            }

            return RunSpawnType.None;
        }

private void RecordSpawn(WaveDefinition wave, RunSpawnType spawnedType)
        {
            switch (spawnedType)
            {
                case RunSpawnType.Traveller:
                    _spawnedTravellerCount++;
                    break;
                case RunSpawnType.Police:
                    _spawnedPoliceCount++;
                    break;
                case RunSpawnType.Military:
                    _spawnedMilitaryCount++;
                    break;
            }

            _nextSpawnType = spawnedType == RunSpawnType.Military
                ? RunSpawnType.Traveller
                : (RunSpawnType)((int)spawnedType + 1);
        }
    }
}
