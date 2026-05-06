using System.Collections;
using RainbowsEnd.Core.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainbowsEnd.Core.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool loadMainArenaOnAwake = true;
        [SerializeField] private string mainArenaSceneName = SceneNames.MainArena;

        private static bool s_isBootstrapped;

        private void Awake()
        {
            if (s_isBootstrapped)
            {
                Destroy(gameObject);
                return;
            }

            s_isBootstrapped = true;
            DontDestroyOnLoad(gameObject);

            if (!loadMainArenaOnAwake)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name == mainArenaSceneName)
            {
                return;
            }

            StartCoroutine(LoadMainArenaAsync());
        }

        private IEnumerator LoadMainArenaAsync()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mainArenaSceneName, LoadSceneMode.Single);

            while (loadOperation is { isDone: false })
            {
                yield return null;
            }
        }
    }
}