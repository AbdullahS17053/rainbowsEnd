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
        [SerializeField] private Transform spawnPointsRoot;
        [SerializeField] private PrefabPool policePool;
        [SerializeField] private PrefabPool projectilePool;
        [SerializeField] private PrefabPool impactPool;
        [SerializeField] private PrefabPool weaponPickupPool;
        [SerializeField] private PoliceMobConfig policeMobConfig;
        [SerializeField] private PrefabPool goldPickupPool;

        private readonly List<SpawnPointMarker> _enemySpawnPoints = new List<SpawnPointMarker>();
        private readonly HashSet<int> _activePoliceIds = new HashSet<int>();
        private ArenaBoundsAuthoring _arenaBounds;

        private int _nextSpawnPointIndex;

        public int ActivePoliceCount => _activePoliceIds.Count;

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
            ResetRuntimeState();
        }

        public bool TrySpawnPolice()
        {
            int spawnIndex = _nextSpawnPointIndex;
            if (!SpawnPolice(spawnIndex))
            {
                return false;
            }

            _nextSpawnPointIndex++;
            return true;
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
            ResetRuntimeState();
        }

        private void OnValidate()
        {
            CacheSpawnPoints();
        }

private bool SpawnPolice(int spawnIndex)
        {
            if (!CanSpawnMorePolice())
            {
                return false;
            }

            SpawnPointMarker spawnPoint = _enemySpawnPoints[spawnIndex % _enemySpawnPoints.Count];
            Vector3 rawPosition = spawnPoint != null ? spawnPoint.transform.position : transform.position;
            Vector3 spawnPosition = ResolvePerimeterSpawnPosition(rawPosition);

            GameObject policeInstance = policePool.Rent(spawnPosition, Quaternion.identity);
            PoliceController policeController = policeInstance.GetComponent<PoliceController>();
            if (policeController == null)
            {
                policePool.Return(policeInstance);
                return false;
            }

            ProjectileWeaponLauncher weaponLauncher = policeInstance.GetComponent<ProjectileWeaponLauncher>();
            WeaponImpactFeedbackSpawner feedbackSpawner = policeInstance.GetComponent<WeaponImpactFeedbackSpawner>();
            weaponLauncher?.Configure(projectilePool);
            feedbackSpawner?.Configure(impactPool);

            ResolveLoadout(
                out WeaponConfig assignedWeaponConfig,
                out WeaponConfig dropWeaponConfig,
                out int dropAmmoCount,
                out bool useShotgun);
            if (assignedWeaponConfig == null)
            {
                policePool.Return(policeInstance);
                return false;
            }

            policeController.Activate(
                this,
                policePool,
                weaponPickupPool,
                goldPickupPool,
                policeConfig,
                assignedWeaponConfig,
                dropWeaponConfig,
                dropAmmoCount,
                useShotgun,
                policeMobConfig);
            NotifyPoliceSpawned(policeController);
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

        private void ResolveLoadout(
            out WeaponConfig assignedWeaponConfig,
            out WeaponConfig dropWeaponConfig,
            out int dropAmmoCount,
            out bool useShotgun)
        {
            if (policeMobConfig == null)
            {
                assignedWeaponConfig = policeWeaponConfig;
                ResolveWeaponDrop(out dropWeaponConfig, out dropAmmoCount);
                useShotgun = false;
                return;
            }

            bool hasPistol = policeMobConfig.PistolWeapon != null;
            bool hasShotgun = policeMobConfig.ShotgunWeapon != null;
            useShotgun = !PoliceLoadoutSelectionUtility.ShouldUsePistol(
                Random.value,
                policeMobConfig.PistolChance,
                hasPistol,
                hasShotgun);

            assignedWeaponConfig = useShotgun
                ? policeMobConfig.ShotgunWeapon
                : policeMobConfig.PistolWeapon;
            dropWeaponConfig = assignedWeaponConfig;
            dropAmmoCount = useShotgun
                ? Mathf.Max(0, policeMobConfig.ShotgunDropAmmo)
                : Mathf.Max(0, policeMobConfig.PistolDropAmmo);
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
            bool hasWeaponConfig = policeMobConfig != null
                ? policeMobConfig.PistolWeapon != null || policeMobConfig.ShotgunWeapon != null
                : policeWeaponConfig != null;

            return policeConfig != null
                && hasWeaponConfig
                && policePool != null
                && _enemySpawnPoints.Count > 0;
        }

        private void ResetRuntimeState()
        {
            _activePoliceIds.Clear();
            _nextSpawnPointIndex = 0;
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
    }
}
