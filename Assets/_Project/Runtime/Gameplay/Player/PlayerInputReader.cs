using UnityEngine;
using UnityEngine.InputSystem;

namespace RainbowsEnd.Gameplay.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionsAsset;
        [SerializeField] private string gameplayMapName = PlayerInputActionNames.GameplayMap;

        private InputActionMap _gameplayActionMap;
        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _fireAction;
        private InputAction _interactAction;
        private InputAction _shopInteractAction;
        private InputAction _castPortalAction;
        private InputAction _pauseAction;

        public Vector2 MoveInput => _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public Vector2 AimValue => _aimAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public bool IsAimFromPointer => _aimAction?.activeControl?.device is Pointer;

        public bool IsAimFromGamepad => _aimAction?.activeControl?.device is Gamepad;

        public bool FireHeld => _fireAction?.IsPressed() ?? false;

        public bool InteractPressedThisFrame => _interactAction?.WasPerformedThisFrame() ?? false;

        public bool ShopInteractPressedThisFrame => _shopInteractAction?.WasPerformedThisFrame() ?? false;

        public bool CastPortalPressedThisFrame => _castPortalAction?.WasPerformedThisFrame() ?? false;

        public bool PausePressedThisFrame => _pauseAction?.WasPerformedThisFrame() ?? false;

        public void SetInputActions(InputActionAsset actionsAsset)
        {
            bool shouldReEnable = _gameplayActionMap != null && _gameplayActionMap.enabled;

            if (shouldReEnable)
            {
                _gameplayActionMap.Disable();
            }

            inputActionsAsset = actionsAsset;
            CacheActions();

            if (isActiveAndEnabled)
            {
                _gameplayActionMap?.Enable();
            }
        }

        private void Awake()
        {
            CacheActions();
        }

        private void OnEnable()
        {
            _gameplayActionMap?.Enable();
        }

        private void OnDisable()
        {
            _gameplayActionMap?.Disable();
        }

        private void CacheActions()
        {
            _gameplayActionMap = inputActionsAsset?.FindActionMap(gameplayMapName, false);
            _moveAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.Move, false);
            _aimAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.Aim, false);
            _fireAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.Fire, false);
            _interactAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.Interact, false);
            _shopInteractAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.ShopInteract, false);
            _castPortalAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.CastPortal, false);
            _pauseAction = _gameplayActionMap?.FindAction(PlayerInputActionNames.Pause, false);
        }
    }
}