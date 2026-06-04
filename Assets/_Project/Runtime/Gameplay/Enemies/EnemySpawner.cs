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

        private int _nextSpawnPointIndex;

        public int ActiveTravellerCount => _activeTravellerIds.Count;

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

        public bool TrySpawnTraveller()
        {
            return SpawnTraveller();
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
            _nextSpawnPointIndex = 0;
        }

        private bool CanSpawnMoreTravellers()
        {
            return travellerConfig != null
                && travellerPool != null
                && _enemySpawnPoints.Count > 0;
        }

        private bool SpawnTraveller()
        {
            if (!CanSpawnMoreTravellers())
            {
                return false;
            }

            SpawnPointMarker spawnPoint = _enemySpawnPoints[_nextSpawnPointIndex % _enemySpawnPoints.Count];
            _nextSpawnPointIndex++;

            TravellerController traveller = travellerPool.Rent<TravellerController>(spawnPoint.transform.position, Quaternion.identity);
            if (traveller == null)
            {
                return false;
            }

            traveller.Activate(this, travellerPool, goldPickupPool, travellerConfig, ResolveGoldDropAmount());
            return true;
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
    }
}
