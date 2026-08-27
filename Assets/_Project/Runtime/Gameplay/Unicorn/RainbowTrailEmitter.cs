using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public sealed class RainbowTrailEmitter : MonoBehaviour
    {
        [SerializeField] private UnicornController unicornController;
        [SerializeField] private string poolObjectName = "RainbowTrailPool";
        [SerializeField] private PrefabPool trailPool;

        private float _spawnCooldown;
        private float _trailStartTime;

        public float TrailAgeSeconds => Time.time - _trailStartTime;

        public void Configure(UnicornController controller)
        {
            unicornController = controller;
            _spawnCooldown = 0f;
            _trailStartTime = Time.time;
        }

        public void ConfigurePool(PrefabPool pool)
        {
            trailPool = pool;
        }

        public void ClearAllTrail()
        {
            RainbowTrailSegment.ReturnAllToPool();
            _trailStartTime = Time.time;
        }

        private void Reset()
        {
            unicornController ??= GetComponent<UnicornController>();
        }

        private void OnValidate()
        {
            unicornController ??= GetComponent<UnicornController>();
        }

        private void Awake()
        {
            unicornController ??= GetComponent<UnicornController>();
            _spawnCooldown = 0f;
            _trailStartTime = Time.time;
        }

        private void Update()
        {
            if (unicornController?.Config == null || unicornController.CurrentVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            if (!ResolvePool())
            {
                return;
            }

            _spawnCooldown -= Time.deltaTime;
            if (_spawnCooldown > 0f)
            {
                return;
            }

            SpawnTrailSegment();
            _spawnCooldown = Mathf.Max(0.01f, unicornController.Config.TrailSpawnIntervalSeconds);
        }

        private void SpawnTrailSegment()
        {
            Vector2Int cell = RainbowTrailCellUtility.GetCell(transform.position, unicornController.Config.TrailCellSizeWorldUnits);
            if (RainbowTrailSegment.IsCellOccupied(cell))
            {
                return;
            }

            GameObject instance = trailPool.Rent(transform.position, Quaternion.identity);
            RainbowTrailSegment trailSegment = instance.GetComponent<RainbowTrailSegment>();
            if (trailSegment == null)
            {
                trailPool.Return(instance);
                return;
            }

            trailSegment.Activate(trailPool, Time.time, cell);
        }

        private bool ResolvePool()
        {
            if (trailPool != null)
            {
                return true;
            }

            if (ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaSceneContext) == true && arenaSceneContext.EffectsRoot != null)
            {
                Transform poolTransform = arenaSceneContext.EffectsRoot.Find(poolObjectName);
                if (poolTransform != null)
                {
                    trailPool = poolTransform.GetComponent<PrefabPool>();
                }
            }

            return trailPool != null;
        }
    }
}