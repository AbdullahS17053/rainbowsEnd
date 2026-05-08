using UnityEngine;
using UnityEngine.Pool;

namespace RainbowsEnd.Systems.Pooling
{
    public sealed class PrefabPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] [Min(0)] private int prewarmCount;
        [SerializeField] private Transform containerRoot;
        [SerializeField] [Min(1)] private int maxPoolSize = 64;
        [SerializeField] private bool collectionChecks = true;

        private IObjectPool<GameObject> _pool;
        private bool _isInitialized;

        private void Awake()
        {
            EnsureInitialized();
            Prewarm();
        }

        public void Configure(GameObject pooledPrefab, int initialPrewarm = 0, Transform poolContainer = null)
        {
            prefab = pooledPrefab;
            prewarmCount = Mathf.Max(0, initialPrewarm);
            containerRoot = poolContainer;
            _pool = null;
            _isInitialized = false;
        }

        public GameObject Rent(Vector3 position, Quaternion rotation)
        {
            EnsureInitialized();

            GameObject instance = _pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public T Rent<T>(Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject instance = Rent(position, rotation);
            return instance.GetComponent<T>();
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            EnsureInitialized();
            _pool.Release(instance);
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            if (containerRoot == null)
            {
                GameObject container = new GameObject($"{name}_Pool");
                container.transform.SetParent(transform, false);
                containerRoot = container.transform;
            }

            _pool = new ObjectPool<GameObject>(
                CreateInstance,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPooledObject,
                collectionChecks,
                prewarmCount,
                Mathf.Max(1, maxPoolSize));

            _isInitialized = true;
        }

        private void Prewarm()
        {
            if (prefab == null)
            {
                return;
            }

            GameObject[] prewarmedInstances = new GameObject[prewarmCount];
            for (int index = 0; index < prewarmCount; index++)
            {
                prewarmedInstances[index] = _pool.Get();
            }

            for (int index = 0; index < prewarmedInstances.Length; index++)
            {
                _pool.Release(prewarmedInstances[index]);
            }
        }

        private GameObject CreateInstance()
        {
            if (prefab == null)
            {
                throw new MissingReferenceException($"{nameof(PrefabPool)} on {name} requires a prefab reference.");
            }

            GameObject instance = Instantiate(prefab, containerRoot);
            instance.SetActive(false);
            return instance;
        }

        private void OnTakeFromPool(GameObject instance)
        {
            instance.transform.SetParent(null, true);
            instance.SetActive(true);
            Notify(instance, true);
        }

        private void OnReturnedToPool(GameObject instance)
        {
            Notify(instance, false);
            instance.SetActive(false);
            instance.transform.SetParent(containerRoot, false);
        }

        private void OnDestroyPooledObject(GameObject instance)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private static void Notify(GameObject instance, bool isSpawned)
        {
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is not IPoolable poolable)
                {
                    continue;
                }

                if (isSpawned)
                {
                    poolable.OnSpawned();
                }
                else
                {
                    poolable.OnDespawned();
                }
            }
        }
    }
}