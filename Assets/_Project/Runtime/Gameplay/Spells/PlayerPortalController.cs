using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Core.SceneManagement;
using RainbowsEnd.Core.Utilities;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Spells
{
    [RequireComponent(typeof(PlayerRoot))]
    public sealed class PlayerPortalController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private PortalConfig portalConfig;
        [SerializeField] private PortalRegistry portalRegistry;
        [SerializeField] [Min(0f)] private float fallbackPlacementDistance = 1.5f;

        private readonly CountdownTimer _cooldownTimer = new CountdownTimer();
        private ArenaSceneContext _arenaSceneContext;

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            ResolveDefaults();
        }

        private void Update()
        {
            _cooldownTimer.Tick(Time.deltaTime);

            if (playerRoot == null || playerRoot.IsDisabled || inputReader == null || !inputReader.CastPortalPressedThisFrame)
            {
                return;
            }

            if (_cooldownTimer.IsRunning || portalConfig == null)
            {
                return;
            }

            ResolveDefaults();
            if (portalRegistry == null)
            {
                return;
            }

            Vector2 aimDirection = aimController != null ? aimController.CurrentAimDirection : Vector2.up;
            if (aimDirection.sqrMagnitude <= 0f)
            {
                aimDirection = Vector2.up;
            }

            Vector2 portalPosition = ResolvePlacementPosition(aimDirection);
            if (!portalRegistry.TryPlacePortal(portalPosition, aimDirection, this))
            {
                return;
            }

            _cooldownTimer.Start(portalConfig.CooldownSeconds);
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            aimController ??= GetComponent<PlayerAimController>();
            portalRegistry ??= FindFirstObjectByType<PortalRegistry>(FindObjectsInactive.Include);
        }

        private void ResolveDefaults()
        {
            if (portalConfig == null && ProjectContext.Instance?.ConfigRegistry != null)
            {
                portalConfig = ProjectContext.Instance.ConfigRegistry.PortalConfig;
            }

            if (portalRegistry == null && ProjectContext.Instance?.Services.TryGet(out PortalRegistry registry) == true)
            {
                portalRegistry = registry;
            }

            if (_arenaSceneContext == null && ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaSceneContext) == true)
            {
                _arenaSceneContext = arenaSceneContext;
            }
        }

        private Vector2 ResolvePlacementPosition(Vector2 aimDirection)
        {
            Vector2 origin = playerRoot != null && playerRoot.Body != null
                ? playerRoot.Body.position
                : (Vector2)transform.position;
            float placementDistance = portalConfig != null && portalConfig.PlacementDistance > 0f
                ? portalConfig.PlacementDistance
                : fallbackPlacementDistance;
            Vector2 placementPosition = origin + aimDirection.normalized * placementDistance;

            ArenaSceneContext arenaSceneContext = _arenaSceneContext;
            if (arenaSceneContext?.ArenaBounds != null)
            {
                placementPosition = arenaSceneContext.ArenaBounds.Clamp(placementPosition);
            }

            return placementPosition;
        }
    }
}