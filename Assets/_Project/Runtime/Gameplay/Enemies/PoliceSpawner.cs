using System.Collections.Generic;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;
using UnityEngine.Serialization;

namespace RainbowsEnd.Gameplay.Enemies
{
    public sealed class PoliceSpawner : MonoBehaviour
    {
        [SerializeField] private EnemyConfig policeConfig;
        [SerializeField] private WeaponConfig policeWeaponConfig;
        [SerializeField] private List<WeaponDropOption> weaponDropOptions = new List<WeaponDropOption>();
        [FormerlySerializedAs("dropWeaponConfig")]
        [SerializeField] [HideInInspector] private WeaponConfig legacyDropWeaponConfig;
        [FormerlySerializedAs("dropAmmoCount")]
        [SerializeField] [HideInInspector] [Min(0)] private int legacyDropAmmoCount = 6;
        [SerializeField] [Min(0)] private int initialPoliceCount = 1;
        [SerializeField] [Min(1)] private int maxConcurrentPolice = 1;
        [SerializeField] [Min(0f)] private float initialSpawnDelaySeconds = 10f;
        [SerializeField] [Min(0.1f)] private float spawnIntervalSeconds = 12f;
        [SerializeField] private Transform spawnPointsRoot;
        [SerializeField] private PrefabPool policePool;
        [SerializeField] private PrefabPool projectilePool;
        [SerializeField] private PrefabPool impactPool;
        [SerializeField] private PrefabPool weaponPickupPool;

        private readonly List<SpawnPointMarker> _enemySpawnPoints = new List<SpawnPointMarker>();
        private readonly HashSet<int> _activePoliceIds = new HashSet<int>();

        private float _spawnDelayRemaining;
        private float _spawnIntervalRemaining;
        private bool _initialWaveSpawned;
        private int _nextSpawnPointIndex;

        public void Configure(
            EnemyConfig enemyConfig,
            WeaponConfig sidearmConfig,
            Transform spawnRoot,
            PrefabPool enemyPool,
            PrefabPool projectilePrefabPool,
            PrefabPool impactPrefabPool,
            PrefabPool pickupPool,
            params WeaponDropOption[] configuredWeaponDropOptions)
        {
            policeConfig = enemyConfig;
            policeWeaponConfig = sidearmConfig;
            spawnPointsRoot = spawnRoot;
            policePool = enemyPool;
            projectilePool = projectilePrefabPool;
            impactPool = impactPrefabPool;
            weaponPickupPool = pickupPool;
            weaponDropOptions.Clear();
            if (configuredWeaponDropOptions != null)
            {
                weaponDropOptions.AddRange(configuredWeaponDropOptions);
            }

            legacyDropWeaponConfig = null;
            legacyDropAmmoCount = 0;
            CacheSpawnPoints();
            ResetTimers();
        }

        public void NotifyPoliceSpawned(PoliceController policeController)
        {
            if (policeController != null)
            {
                _activePoliceIds.Add(policeController.GetInstanceID());
            }
        }

        public void NotifyPoliceDespawned(PoliceController policeController)
        {
            if (policeController != null)
            {
                _activePoliceIds.Remove(policeController.GetInstanceID());
            }
        }

        private void Awake()
        {
            CacheSpawnPoints();
            ResetTimers();
        }

        private void OnValidate()
        {
            CacheSpawnPoints();
        }

        private void Update()
        {
            if (!CanSpawnMorePolice() && !_initialWaveSpawned)
            {
                _initialWaveSpawned = true;
            }

            if (!_initialWaveSpawned)
            {
                _spawnDelayRemaining -= Time.deltaTime;
                if (_spawnDelayRemaining > 0f)
                {
                    return;
                }

                _initialWaveSpawned = true;
                for (int index = 0; index < initialPoliceCount && CanSpawnMorePolice(); index++)
                {
                    SpawnPolice(_nextSpawnPointIndex);
                    _nextSpawnPointIndex++;
                }

                _spawnIntervalRemaining = spawnIntervalSeconds;
                return;
            }

            if (!CanSpawnMorePolice())
            {
                return;
            }

            _spawnIntervalRemaining -= Time.deltaTime;
            if (_spawnIntervalRemaining > 0f)
            {
                return;
            }

            _spawnIntervalRemaining = spawnIntervalSeconds;
            SpawnPolice(_nextSpawnPointIndex);
            _nextSpawnPointIndex++;
        }

        private void SpawnPolice(int spawnIndex)
        {
            if (!CanSpawnMorePolice())
            {
                return;
            }

            SpawnPointMarker spawnPoint = _enemySpawnPoints[spawnIndex % _enemySpawnPoints.Count];
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.transform.position : transform.position;

            GameObject policeInstance = policePool.Rent(spawnPosition, Quaternion.identity);
            PoliceController policeController = policeInstance.GetComponent<PoliceController>();
            if (policeController == null)
            {
                policePool.Return(policeInstance);
                return;
            }

            ProjectileWeaponLauncher weaponLauncher = policeInstance.GetComponent<ProjectileWeaponLauncher>();
            WeaponImpactFeedbackSpawner feedbackSpawner = policeInstance.GetComponent<WeaponImpactFeedbackSpawner>();
            weaponLauncher?.Configure(projectilePool);
            feedbackSpawner?.Configure(impactPool);

            ResolveWeaponDrop(out WeaponConfig dropWeaponConfig, out int dropAmmoCount);
            policeController.Activate(this, policePool, weaponPickupPool, policeConfig, policeWeaponConfig, dropWeaponConfig, dropAmmoCount);
            NotifyPoliceSpawned(policeController);
        }

        private void ResolveWeaponDrop(out WeaponConfig dropWeaponConfig, out int dropAmmoCount)
        {
            if (EnemyDropSelectionUtility.TrySelectWeaponDrop(weaponDropOptions, Random.value, out dropWeaponConfig, out dropAmmoCount))
            {
                return;
            }

            dropWeaponConfig = legacyDropWeaponConfig;
            dropAmmoCount = dropWeaponConfig != null ? Mathf.Max(0, legacyDropAmmoCount) : 0;
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

        private bool CanSpawnMorePolice()
        {
            return policeConfig != null
                && policeWeaponConfig != null
                && policePool != null
                && _enemySpawnPoints.Count > 0
                && _activePoliceIds.Count < Mathf.Max(1, maxConcurrentPolice);
        }

        private void ResetTimers()
        {
            _activePoliceIds.Clear();
            _initialWaveSpawned = false;
            _spawnDelayRemaining = initialSpawnDelaySeconds;
            _spawnIntervalRemaining = spawnIntervalSeconds;
            _nextSpawnPointIndex = 0;
        }
    }
}