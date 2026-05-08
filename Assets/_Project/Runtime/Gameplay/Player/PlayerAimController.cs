using RainbowsEnd.Core.Bootstrap;
using RainbowsEnd.Core.SceneManagement;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    public sealed class PlayerAimController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform aimTransform;
        [SerializeField] private Camera worldCamera;
        [SerializeField] [Range(0f, 1f)] private float controllerDeadzone = 0.2f;

        private Vector2 _lastAimDirection = Vector2.up;

        public Vector2 CurrentAimDirection => _lastAimDirection;

        public void Configure(PlayerRoot root, PlayerInputReader reader, Transform targetTransform, Camera cameraOverride)
        {
            playerRoot = root;
            inputReader = reader;
            aimTransform = targetTransform;
            worldCamera = cameraOverride;
        }

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
        }

        private void Update()
        {
            if (playerRoot == null || inputReader == null)
            {
                return;
            }

            Camera activeCamera = ResolveCamera();
            Transform target = aimTransform != null ? aimTransform : playerRoot.VisualRoot;
            Vector2 aimDirection = PlayerAimResolver.ResolveAimDirection(
                inputReader.AimValue,
                inputReader.IsAimFromPointer,
                activeCamera,
                target != null ? target.position : transform.position,
                _lastAimDirection,
                controllerDeadzone);

            if (aimDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            _lastAimDirection = aimDirection;

            if (target == null)
            {
                return;
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            float turnSpeed = playerRoot.Config != null ? playerRoot.Config.AimTurnSpeed : 0f;

            if (turnSpeed <= 0f)
            {
                target.rotation = targetRotation;
                return;
            }

            target.rotation = Quaternion.RotateTowards(target.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            aimTransform ??= playerRoot != null ? playerRoot.VisualRoot : transform;
        }

        private Camera ResolveCamera()
        {
            if (worldCamera != null)
            {
                return worldCamera;
            }

            if (ProjectContext.Instance?.Services.TryGet(out ArenaSceneContext arenaSceneContext) == true)
            {
                worldCamera = arenaSceneContext.MainCamera;
                return worldCamera;
            }

            worldCamera = Camera.main;
            return worldCamera;
        }
    }
}