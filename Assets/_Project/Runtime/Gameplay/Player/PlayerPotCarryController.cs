using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    [RequireComponent(typeof(PlayerRoot))]
    public sealed class PlayerPotCarryController : MonoBehaviour
    {
        [SerializeField] private PlayerRoot playerRoot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] [Min(0f)] private float carryDistance = 0.9f;
        [SerializeField] [Min(0f)] private float dropDistance = 1.05f;

        private PotController _carriedPot;

        public bool IsCarryingPot => _carriedPot != null;

        public PotController CarriedPot => _carriedPot;

        public void Configure(PlayerRoot root, PlayerInputReader reader, PlayerAimController playerAimController)
        {
            playerRoot = root;
            inputReader = reader;
            aimController = playerAimController;
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
            if (playerRoot == null || !playerRoot.CanInteractWithPot || inputReader == null)
            {
                return;
            }

            if (!inputReader.InteractPressedThisFrame)
            {
                return;
            }

            if (IsCarryingPot)
            {
                DropCarriedPot();
                return;
            }

            TryPickUpNearestPot();
        }

        private void LateUpdate()
        {
            if (_carriedPot == null)
            {
                return;
            }

            if (_carriedPot.IsDestroyed)
            {
                _carriedPot = null;
                return;
            }

            _carriedPot.SetCarriedWorldPosition(ResolveCarryAnchor(ResolveCarryDistance()));
        }

        private void OnDisable()
        {
            if (_carriedPot != null)
            {
                DropCarriedPot();
            }
        }

        private void CacheReferences()
        {
            playerRoot ??= GetComponent<PlayerRoot>();
            inputReader ??= GetComponent<PlayerInputReader>();
            aimController ??= GetComponent<PlayerAimController>();
        }

        private void TryPickUpNearestPot()
        {
            PotController nearestPot = ResolveNearestPickupPot();
            if (nearestPot == null)
            {
                return;
            }

            Vector2 previousPos = nearestPot.transform.position;
            if (!nearestPot.TryPickUp(playerRoot))
            {
                return;
            }

            _carriedPot = nearestPot;
            _carriedPot.SetCarriedWorldPosition(ResolveCarryAnchor(ResolveCarryDistance()));
            playerRoot.RefreshVisualState();

            PlayerUpgradeRuntime upgradeRuntime = playerRoot != null ? playerRoot.GetComponent<PlayerUpgradeRuntime>() : null;
            if (upgradeRuntime != null && upgradeRuntime.HasCloverOnPotPickup && Random.value <= 0.10f)
            {
                RainbowsEnd.Gameplay.World.CloverPatch.Spawn(previousPos);
            }
        }

        private void DropCarriedPot()
        {
            if (_carriedPot == null)
            {
                return;
            }

            PotController potToDrop = _carriedPot;
            Vector2 dropPosition = ResolveCarryAnchor(ResolveDropDistance());
            if (!potToDrop.CanDropAt(dropPosition))
            {
                return;
            }

            _carriedPot = null;
            potToDrop.Drop(dropPosition);
            playerRoot.RefreshVisualState();
        }

        private PotController ResolveNearestPickupPot()
        {
            float interactionRadius = ResolveInteractionRadius();
            if (interactionRadius <= 0f)
            {
                return null;
            }

            Vector2 origin = ResolvePlayerPosition();
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(origin, interactionRadius);
            PotController bestPot = null;
            float bestDistanceSquared = float.MaxValue;

            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];
                PotController pot = overlap != null ? overlap.GetComponentInParent<PotController>() : null;
                if (pot == null || !pot.CanBePickedUp)
                {
                    continue;
                }

                float distanceSquared = ((Vector2)pot.transform.position - origin).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestPot = pot;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return bestPot;
        }

        private Vector2 ResolveCarryAnchor(float distance)
        {
            return ResolvePlayerPosition() + ResolveCarryDirection() * distance;
        }

        private Vector2 ResolvePlayerPosition()
        {
            if (playerRoot != null && playerRoot.Body != null)
            {
                return playerRoot.Body.position;
            }

            return transform.position;
        }

        private Vector2 ResolveCarryDirection()
        {
            Vector2 aimDirection = aimController != null ? aimController.CurrentAimDirection : Vector2.zero;
            if (aimDirection.sqrMagnitude > 0.0001f)
            {
                return aimDirection.normalized;
            }

            Vector2 moveDirection = inputReader != null ? inputReader.MoveInput : Vector2.zero;
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                return moveDirection.normalized;
            }

            Transform referenceTransform = playerRoot != null ? playerRoot.VisualRoot : transform;
            Vector2 forward = referenceTransform != null ? (Vector2)referenceTransform.up : Vector2.up;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
        }

        private float ResolveInteractionRadius()
        {
            return playerRoot?.Config != null ? Mathf.Max(0f, playerRoot.Config.InteractionRadius) : 0f;
        }

        private float ResolveCarryDistance()
        {
            return carryDistance > 0f ? carryDistance : Mathf.Max(ResolveInteractionRadius() * 0.7f, 0.6f);
        }

        private float ResolveDropDistance()
        {
            return dropDistance > 0f ? dropDistance : ResolveCarryDistance();
        }
    }
}