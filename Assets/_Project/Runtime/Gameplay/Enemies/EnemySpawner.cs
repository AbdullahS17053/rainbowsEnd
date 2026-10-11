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
        private ArenaBoundsAuthoring _arenaBounds;

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

            Vector3 spawnPosition = ResolvePerimeterSpawnPosition(spawnPoint != null ? spawnPoint.transform.position : transform.position);
            TravellerController traveller = travellerPool.Rent<TravellerController>(spawnPosition, Quaternion.identity);
            if (traveller == null)
            {
                return false;
            }

            traveller.Activate(this, travellerPool, goldPickupPool, travellerConfig, ResolveGoldDropAmount());
            return true;
        }

        private Vector3 ResolvePerimeterSpawnPosition(Vector3 position)
        {
            if (_arenaBounds == null)
            {
                _arenaBounds = FindFirstObjectByType<ArenaBoundsAuthoring>(FindObjectsInactive.Exclude);
            }

            if (_arenaBounds != null && _arenaBounds.WorldRect.width > 0f && _arenaBounds.WorldRect.height > 0f)
            {
                Rect r = _arenaBounds.WorldRect;
                float marginX = r.width * 0.08f;
                float marginY = r.height * 0.08f;
                Rect inner = new Rect(r.xMin + marginX, r.yMin + marginY, r.width - (2f * marginX), r.height - (2f * marginY));
                if (inner.Contains(new Vector2(position.x, position.y)))
                {
                    Vector2 edge = _arenaBounds.GetNearestEdgePoint(position);
                    return new Vector3(edge.x, edge.y, position.z);
                }
            }

            return position;
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
