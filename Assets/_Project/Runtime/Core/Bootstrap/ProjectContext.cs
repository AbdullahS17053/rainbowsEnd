using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Core.Bootstrap
{
    public sealed class ProjectContext : MonoBehaviour
    {
        private const string DefaultConfigResourcePath = "ProjectConfigRegistry";

        [SerializeField] private ProjectConfigRegistry projectConfigRegistry;
        [SerializeField] private string configResourcePath = DefaultConfigResourcePath;

        private bool _isInitialized;

        public static ProjectContext Instance { get; private set; }

        public IReadOnlyServiceRegistry Services => ServiceRegistry;

        public ServiceRegistry ServiceRegistry { get; private set; }

        public ProjectConfigRegistry ConfigRegistry => projectConfigRegistry;

        public static ProjectContext EnsureOn(GameObject host)
        {
            if (Instance != null)
            {
                return Instance;
            }

            if (!host.TryGetComponent(out ProjectContext context))
            {
                context = host.AddComponent<ProjectContext>();
            }

            context.Initialize();
            return context;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate ProjectContext detected. Destroying duplicate component.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            projectConfigRegistry ??= LoadConfigRegistry();

            ServiceRegistry = new ServiceRegistry();
            ServiceRegistry.Register<ProjectContext>(this);
            ServiceRegistry.Register<IReadOnlyServiceRegistry>(ServiceRegistry);

            if (projectConfigRegistry != null)
            {
                ServiceRegistry.Register(projectConfigRegistry);
            }
            else
            {
                Debug.LogWarning(
                    $"ProjectConfigRegistry could not be loaded from Resources path '{configResourcePath}'. Run the Phase 1 core setup command in Unity.",
                    this);
            }

            _isInitialized = true;
        }

        private ProjectConfigRegistry LoadConfigRegistry()
        {
            if (string.IsNullOrWhiteSpace(configResourcePath))
            {
                configResourcePath = DefaultConfigResourcePath;
            }

            return Resources.Load<ProjectConfigRegistry>(configResourcePath);
        }
    }
}