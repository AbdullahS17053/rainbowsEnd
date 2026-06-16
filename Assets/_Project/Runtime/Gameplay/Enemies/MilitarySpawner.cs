using System.Collections.Generic;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using RainbowsEnd.Gameplay.World;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;
using UnityEngine.Serialization;

namespace RainbowsEnd.Gameplay.Enemies
{
    public sealed class MilitarySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyConfig militaryConfig;
        [SerializeField] private WeaponConfig militaryWeaponConfig;
        [SerializeField] private List<WeaponDropOption> weaponDropOptions = new List<WeaponDropOption>();
        [SerializeField] private List<GoldDropOption> goldDropOptions = new List<GoldDropOption>();
        [FormerlySerializedAs("dropWeaponConfig")]
        [SerializeField] [HideInInspector] private WeaponConfig legacyDropWeaponConfig;
        [FormerlySerializedAs("dropAmmoCount")]
        [SerializeField] [HideInInspector] [Min(0)] private int legacyDropAmmoCount = 30;
        [FormerlySerializedAs("dropGoldAmount")]
        [SerializeField] [HideInInspector] [Min(0)] private int legacyDropGoldAmount = 10;
        [SerializeField] [Min(0)] private int defaultGoldDropAmount = 10;
        [SerializeField] private Transform spawnPointsRoot;
        [SerializeField] private PrefabPool militaryPool;
        [SerializeField] private PrefabPool projectilePool;
        [SerializeField] private PrefabPool impactPool;
        [SerializeField] private PrefabPool weaponPickupPool;
        [SerializeField] private PrefabPool goldPickupPool;

        private readonly List<SpawnPointMarker> _enemySpawnPoints = new List<SpawnPointMarker>();
        private readonly HashSet<int> _activeMilitaryIds = new HashSet<int>();

        private int _nextSpawnPointIndex;

        public int ActiveMilitaryCount => _activeMilitaryIds.Count;

        public void Configure(
            EnemyConfig enemyConfig,
            WeaponConfig sidearmConfig,
            Transform spawnRoot,
            PrefabPool enemyPool,
            PrefabPool projectilePrefabPool,
            PrefabPool impactPrefabPool,
            PrefabPool pickupPool,
            PrefabPool goldPool,
            params WeaponDropOption[] configuredWeaponDropOptions)
        {
            militaryConfig = enemyConfig;
            militaryWeaponConfig = sidearmConfig;
            spawnPointsRoot = spawnRoot;
            militaryPool = enemyPool;
            projectilePool = projectilePrefabPool;
            impactPool = impactPrefabPool;
            weaponPickupPool = pickupPool;
            goldPickupPool = goldPool;
            weaponDropOptions.Clear();
            if (configuredWeaponDropOptions != null)
            {
                weaponDropOptions.AddRange(configuredWeaponDropOptions);
            }

            legacyDropWeaponConfig = null;
            legacyDropAmmoCount = 0;
            if (goldDropOptions.Count > 0)
            {
                legacyDropGoldAmount = 0;
            }
            CacheSpawnPoints();
            ResetRuntimeState();
        }

        public bool TrySpawnMilitary()
        {
            int spawnIndex = _nextSpawnPointIndex;
            if (!SpawnMilitary(spawnIndex))
            {
                return false;
            }

            _nextSpawnPointIndex++;
            return true;
        }

        public void NotifyMilitarySpawned(MilitaryController militaryController)
        {
            if (militaryController != null)
            {
                _activeMilitaryIds.Add(militaryController.GetInstanceID());
            }
        }

        public void NotifyMilitaryDespawned(MilitaryController militaryController)
        {
            if (militaryController != null)
            {
                _activeMilitaryIds.Remove(militaryController.GetInstanceID());
            }
        }

        private void Awake()
        {
            CacheSpawnPoints();
            ResetRuntimeState();
        }

        private void OnValidate()
        {
            CacheSpawnPoints();
        }

        private bool SpawnMilitary(int spawnIndex)
        {
            if (!CanSpawnMoreMilitary())
            {
                return false;
            }

            SpawnPointMarker spawnPoint = _enemySpawnPoints[spawnIndex % _enemySpawnPoints.Count];
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.transform.position : transform.position;

            GameObject militaryInstance = militaryPool.Rent(spawnPosition, Quaternion.identity);
            MilitaryController militaryController = militaryInstance.GetComponent<MilitaryController>();
            if (militaryController == null)
            {
                militaryPool.Return(militaryInstance);
                return false;
            }

            ProjectileWeaponLauncher weaponLauncher = militaryInstance.GetComponent<ProjectileWeaponLauncher>();
            WeaponImpactFeedbackSpawner feedbackSpawner = militaryInstance.GetComponent<WeaponImpactFeedbackSpawner>();
            weaponLauncher?.Configure(projectilePool);
            feedbackSpawner?.Configure(impactPool);

            ResolveWeaponDrop(out WeaponConfig dropWeaponConfig, out int dropAmmoCount);
            int dropGoldAmount = ResolveGoldDropAmount();
            militaryController.Activate(this, militaryPool, weaponPickupPool, goldPickupPool, militaryConfig, militaryWeaponConfig, dropWeaponConfig, dropAmmoCount, dropGoldAmount);
            NotifyMilitarySpawned(militaryController);
            return true;
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

        private int ResolveGoldDropAmount()
        {
            int configuredGoldAmount = EnemyDropSelectionUtility.SelectGoldAmount(goldDropOptions, Random.value);
            if (configuredGoldAmount > 0)
            {
                return configuredGoldAmount;
            }

            int legacyAmount = Mathf.Max(0, legacyDropGoldAmount);
            if (legacyAmount > 0)
            {
                return legacyAmount;
            }

            return Mathf.Max(0, defaultGoldDropAmount);
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

        private bool CanSpawnMoreMilitary()
        {
            return militaryConfig != null
                && militaryWeaponConfig != null
                && militaryPool != null
                && _enemySpawnPoints.Count > 0;
        }

        private void ResetRuntimeState()
        {
            _activeMilitaryIds.Clear();
            _nextSpawnPointIndex = 0;
        }
    }
}
