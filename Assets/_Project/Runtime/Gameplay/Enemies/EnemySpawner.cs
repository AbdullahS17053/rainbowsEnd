using System.Collections.Generic;
using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyConfig travellerConfig;
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private List<GoldDropOption> goldDropOptions = new List<GoldDropOption>();
        [SerializeField] private PrefabPool travellerPool;
        [SerializeField] private PrefabPool goldPickupPool;
        [SerializeField] private Transform spawnPointsRoot;

        private readonly List<SpawnPointMarker> _enemySpawnPoints = new List<SpawnPointMarker>();
        private readonly HashSet<int> _activeTravellerIds = new HashSet<int>();

        private bool _initialBurstSpawned;
        private float _spawnTimerSeconds;
        private int _nextSpawnPointIndex;

        public void Configure(
            EnemyConfig enemyConfig,
            WaveConfig waves,
            EconomyConfig economy,
            PrefabPool travellerPrefabPool,
            PrefabPool pickupPool,
            Transform sceneSpawnPointsRoot,
            params GoldDropOption[] configuredGoldDropOptions)
        {
            travellerConfig = enemyConfig;
            waveConfig = waves;
            economyConfig = economy;
            travellerPool = travellerPrefabPool;
            goldPickupPool = pickupPool;
            spawnPointsRoot = sceneSpawnPointsRoot;
            goldDropOptions.Clear();
            if (configuredGoldDropOptions != null)
            {
                goldDropOptions.AddRange(configuredGoldDropOptions);
            }

            CacheSpawnPoints();
            ResetRuntimeState();
        }

        public void NotifyTravellerSpawned(TravellerController traveller)
        {
            if (traveller != null)
            {
                _activeTravellerIds.Add(traveller.GetInstanceID());
            }
        }

        public void NotifyTravellerDespawned(TravellerController traveller)
        {
            if (traveller != null)
            {
                _activeTravellerIds.Remove(traveller.GetInstanceID());
            }
        }

        private void Awake()
        {
            ResolveDefaults();
            CacheSpawnPoints();
            ResetRuntimeState();
        }

        private void OnEnable()
        {
            ResetRuntimeState();
        }

        private void OnValidate()
        {
            CacheSpawnPoints();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!_initialBurstSpawned)
            {
                _spawnTimerSeconds -= Time.deltaTime;
                if (_spawnTimerSeconds > 0f)
                {
                    return;
                }

                SpawnInitialTravellers();
                _initialBurstSpawned = true;
                _spawnTimerSeconds = ResolveSpawnIntervalSeconds();
                return;
            }

            if (!CanSpawnMoreTravellers())
            {
                return;
            }

            _spawnTimerSeconds -= Time.deltaTime;
            if (_spawnTimerSeconds > 0f)
            {
                return;
            }

            SpawnTraveller();
            _spawnTimerSeconds = ResolveSpawnIntervalSeconds();
        }

        private void ResolveDefaults()
        {
            if (ProjectContext.Instance?.ConfigRegistry == null)
            {
                return;
            }

            travellerConfig ??= ProjectContext.Instance.ConfigRegistry.TravellerConfig;
            waveConfig ??= ProjectContext.Instance.ConfigRegistry.WaveConfig;
            economyConfig ??= ProjectContext.Instance.ConfigRegistry.EconomyConfig;
        }

        private void CacheSpawnPoints()
        {
            _enemySpawnPoints.Clear();
            if (spawnPointsRoot == null)
            {
                return;
            }

            SpawnPointMarker[] markers = spawnPointsRoot.GetComponentsInChildren<SpawnPointMarker>(true);
            for (int index = 0; index < markers.Length; index++)
            {
                if (markers[index].Kind == SpawnPointKind.EnemyEdge)
                {
                    _enemySpawnPoints.Add(markers[index]);
                }
            }

            _enemySpawnPoints.Sort((left, right) => left.Order.CompareTo(right.Order));
        }

        private void ResetRuntimeState()
        {
            _activeTravellerIds.Clear();
            _spawnTimerSeconds = ResolveInitialWaveDelaySeconds();
            _nextSpawnPointIndex = 0;
            _initialBurstSpawned = false;
        }

        private void SpawnInitialTravellers()
        {
            int initialCount = waveConfig != null ? Mathf.Max(1, waveConfig.InitialTravellerCount) : 1;
            for (int index = 0; index < initialCount && CanSpawnMoreTravellers(); index++)
            {
                SpawnTraveller();
            }
        }

        private bool CanSpawnMoreTravellers()
        {
            return travellerConfig != null
                && waveConfig != null
                && travellerPool != null
                && _enemySpawnPoints.Count > 0
                && _activeTravellerIds.Count < Mathf.Max(1, waveConfig.MaxConcurrentEnemies);
        }

        private void SpawnTraveller()
        {
            if (!CanSpawnMoreTravellers())
            {
                return;
            }

            SpawnPointMarker spawnPoint = _enemySpawnPoints[_nextSpawnPointIndex % _enemySpawnPoints.Count];
            _nextSpawnPointIndex++;

            TravellerController traveller = travellerPool.Rent<TravellerController>(spawnPoint.transform.position, Quaternion.identity);
            traveller.Activate(this, travellerPool, goldPickupPool, travellerConfig, ResolveGoldDropAmount());
        }

        private int ResolveGoldDropAmount()
        {
            int configuredGoldAmount = EnemyDropSelectionUtility.SelectGoldAmount(goldDropOptions, Random.value);
            if (configuredGoldAmount > 0)
            {
                return configuredGoldAmount;
            }

            if (economyConfig == null)
            {
                return 0;
            }

            int minGold = Mathf.Max(0, economyConfig.EnemyDropGoldMin);
            int maxGold = Mathf.Max(minGold, economyConfig.EnemyDropGoldMax);
            return Random.Range(minGold, maxGold + 1);
        }

        private float ResolveInitialWaveDelaySeconds()
        {
            return waveConfig != null ? Mathf.Max(0f, waveConfig.InitialWaveDelaySeconds) : 0f;
        }

        private float ResolveSpawnIntervalSeconds()
        {
            return waveConfig != null ? Mathf.Max(0.1f, waveConfig.SpawnIntervalSeconds) : 2f;
        }
    }
}