using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Systems.Pooling
{
    public sealed class PrefabPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] [Min(0)] private int prewarmCount;
        [SerializeField] private Transform containerRoot;

        private readonly Queue<GameObject> _availableInstances = new Queue<GameObject>();
        private bool _isInitialized;

        private void Awake()
        {
            EnsureInitialized();
            Prewarm();
        }

        public GameObject Rent(Vector3 position, Quaternion rotation)
        {
            EnsureInitialized();

            GameObject instance = _availableInstances.Count > 0 ? _availableInstances.Dequeue() : CreateInstance();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(null, true);
            instance.SetActive(true);
            Notify(instance, true);
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

            Notify(instance, false);
            instance.SetActive(false);
            instance.transform.SetParent(containerRoot, false);
            _availableInstances.Enqueue(instance);
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

            _isInitialized = true;
        }

        private void Prewarm()
        {
            if (prefab == null)
            {
                return;
            }

            while (_availableInstances.Count < prewarmCount)
            {
                GameObject instance = CreateInstance();
                instance.SetActive(false);
                _availableInstances.Enqueue(instance);
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