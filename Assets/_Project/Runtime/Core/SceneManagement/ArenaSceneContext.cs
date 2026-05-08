using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Gameplay.World;
using UnityEngine;

namespace RainbowsEnd.Core.SceneManagement
{
    public sealed class ArenaSceneContext : MonoBehaviour
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private Transform gameplayRoot;
        [SerializeField] private Transform spawnPointsRoot;
        [SerializeField] private Transform effectsRoot;
        [SerializeField] private Transform uiRoot;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ArenaBoundsAuthoring arenaBounds;
        [SerializeField] private FaeTreeAnchor faeTreeAnchor;

        public Transform WorldRoot => worldRoot;

        public Transform EnvironmentRoot => environmentRoot;

        public Transform GameplayRoot => gameplayRoot;

        public Transform SpawnPointsRoot => spawnPointsRoot;

        public Transform EffectsRoot => effectsRoot;

        public Transform UiRoot => uiRoot;

        public Camera MainCamera => mainCamera;

        public ArenaBoundsAuthoring ArenaBounds => arenaBounds;

        public FaeTreeAnchor FaeTreeAnchor => faeTreeAnchor;

        public bool IsConfigured =>
            worldRoot != null &&
            environmentRoot != null &&
            gameplayRoot != null &&
            spawnPointsRoot != null &&
            uiRoot != null &&
            mainCamera != null &&
            arenaBounds != null;

        private void Awake()
        {
            if (!IsConfigured)
            {
                Debug.LogWarning("ArenaSceneContext is missing scene references. Re-run the Phase 0 bootstrap or assign references manually.", this);
            }

            if (ProjectContext.Instance?.ServiceRegistry != null)
            {
                ProjectContext.Instance.ServiceRegistry.Register(this, replaceExisting: true);
            }
        }

        public void Configure(
            Transform world,
            Transform environment,
            Transform gameplay,
            Transform spawnPoints,
            Transform ui,
            Camera sceneCamera)
        {
            worldRoot = world;
            environmentRoot = environment;
            gameplayRoot = gameplay;
            spawnPointsRoot = spawnPoints;
            uiRoot = ui;
            mainCamera = sceneCamera;
        }

        public void ConfigureWorld(ArenaBoundsAuthoring bounds, FaeTreeAnchor faeTree)
        {
            arenaBounds = bounds;
            faeTreeAnchor = faeTree;
        }

        public void ConfigureEffects(Transform effects)
        {
            effectsRoot = effects;
        }
    }
}