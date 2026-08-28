using System.Collections.Generic;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Unicorn;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Spells
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PortalInstance : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private GameObject oldestVfx;
        [SerializeField] private GameObject newestVfx;

        private readonly Dictionary<int, float> _lockoutExpirationsByTargetId = new Dictionary<int, float>();

        private PortalRegistry _owningRegistry;
        private PortalConfig _portalConfig;
        private PortalInstance _pairedPortal;
        private int _portalSlotIndex = -1;

        public PortalInstance PairedPortal => _pairedPortal;

        public void Configure(PortalRegistry owningRegistry, PortalConfig portalConfig, Vector2 facingDirection, Object instigator)
        {
            _owningRegistry = owningRegistry;
            _portalConfig = portalConfig;

            Vector2 normalizedFacingDirection = facingDirection.sqrMagnitude > 0f ? facingDirection.normalized : Vector2.up;
            transform.up = normalizedFacingDirection;
            EnsureTriggerCollider();
            ApplyConfiguredEntryRadius();
            UpdatePortalVfx();
        }

        public void SetPairedPortal(PortalInstance pairedPortal)
        {
            _pairedPortal = pairedPortal;
            UpdatePortalVfx();
        }

        public void SetSlotIndex(int index)
        {
            _portalSlotIndex = index;
            UpdatePortalVfx();
        }

        public void OnSpawned()
        {
            EnsureTriggerCollider();
            _lockoutExpirationsByTargetId.Clear();
            _pairedPortal = null;
            _portalSlotIndex = -1;
            UpdatePortalVfx();
        }

        public void OnDespawned()
        {
            _lockoutExpirationsByTargetId.Clear();
            _pairedPortal = null;
            _portalSlotIndex = -1;
            UpdatePortalVfx();
        }

        private void Reset()
        {
            CacheReferences();
            EnsureTriggerCollider();
            UpdatePortalVfx();
        }

        private void OnValidate()
        {
            CacheReferences();
            EnsureTriggerCollider();
            ValidateTransformScale();
            UpdatePortalVfx();
        }

        private void Awake()
        {
            CacheReferences();
            EnsureTriggerCollider();
            UpdatePortalVfx();
        }

private void OnTriggerEnter2D(Collider2D other)
        {
            if (_pairedPortal == null || _portalConfig == null)
            {
                return;
            }

            if (!TryResolveTeleportTarget(other, out Component targetRoot, out Rigidbody2D targetBody))
            {
                return;
            }

            int targetId = targetRoot.GetInstanceID();
            if (IsLockoutActive(targetId))
            {
                return;
            }

            Vector2 exitDirection = _pairedPortal.transform.up.sqrMagnitude > 0f
                ? ((Vector2)_pairedPortal.transform.up).normalized
                : Vector2.up;
            Vector2 exitPosition = (Vector2)_pairedPortal.transform.position + exitDirection * _portalConfig.ExitOffsetDistance;

            TeleportTarget(targetRoot.transform, targetBody, exitPosition);

            UnicornController teleportedUnicorn = targetRoot as UnicornController;
            teleportedUnicorn?.NotifyTeleported();

            float lockoutUntil = Time.time + Mathf.Max(0f, _portalConfig.ReentryLockoutSeconds);
            SetLockout(targetId, lockoutUntil);
            _pairedPortal.SetLockout(targetId, lockoutUntil);
        }

        private void CacheReferences()
        {
            triggerCollider ??= GetComponent<Collider2D>();
        }

        private void EnsureTriggerCollider()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void ApplyConfiguredEntryRadius()
        {
            if (_portalConfig == null || triggerCollider is not CircleCollider2D circleCollider)
            {
                return;
            }

            if (!TryGetUniformScale(out float uniformScale))
            {
                return;
            }

            circleCollider.radius = Mathf.Max(0.01f, _portalConfig.EntryRadiusWorldUnits) / uniformScale;
        }

        private void ValidateTransformScale()
        {
            TryGetUniformScale(out _);
        }

        private bool TryGetUniformScale(out float uniformScale)
        {
            Vector3 scale = transform.lossyScale;
            float xScale = Mathf.Abs(scale.x);
            float yScale = Mathf.Abs(scale.y);
            if (xScale <= 0.0001f || yScale <= 0.0001f)
            {
                uniformScale = 1f;
                Debug.LogError($"{nameof(PortalInstance)} on {name} requires a non-zero root scale to apply its entry radius.", this);
                return false;
            }

            if (!Mathf.Approximately(xScale, yScale))
            {
                uniformScale = 1f;
                Debug.LogError($"{nameof(PortalInstance)} on {name} requires a uniform root scale to apply its circular entry radius.", this);
                return false;
            }

            uniformScale = xScale;
            return true;
        }

        private bool TryResolveTeleportTarget(Collider2D other, out Component targetRoot, out Rigidbody2D targetBody)
        {
            targetRoot = null;
            targetBody = null;

            if (other == null)
            {
                return false;
            }

            PlayerRoot playerRoot = other.GetComponentInParent<PlayerRoot>();
            if (playerRoot != null && playerRoot.CanUsePortals)
            {
                targetRoot = playerRoot;
                targetBody = playerRoot.Body;
                return true;
            }

            UnicornController unicornController = other.GetComponentInParent<UnicornController>();
            if (unicornController != null)
            {
                targetRoot = unicornController;
                targetBody = unicornController.GetComponent<Rigidbody2D>();
                return true;
            }

            PotController potController = other.GetComponentInParent<PotController>();
            if (potController != null && !potController.IsDestroyed)
            {
                targetRoot = potController;
                targetBody = potController.GetComponent<Rigidbody2D>();
                return true;
            }

            return false;
        }

        private bool IsLockoutActive(int targetId)
        {
            ClearExpiredLockouts();
            return _lockoutExpirationsByTargetId.TryGetValue(targetId, out float unlockTime) && unlockTime > Time.time;
        }

        private void SetLockout(int targetId, float unlockTime)
        {
            _lockoutExpirationsByTargetId[targetId] = unlockTime;
        }

        private void ClearExpiredLockouts()
        {
            if (_lockoutExpirationsByTargetId.Count <= 0)
            {
                return;
            }

            List<int> expiredKeys = null;
            foreach (KeyValuePair<int, float> pair in _lockoutExpirationsByTargetId)
            {
                if (pair.Value > Time.time)
                {
                    continue;
                }

                expiredKeys ??= new List<int>();
                expiredKeys.Add(pair.Key);
            }

            if (expiredKeys == null)
            {
                return;
            }

            for (int index = 0; index < expiredKeys.Count; index++)
            {
                _lockoutExpirationsByTargetId.Remove(expiredKeys[index]);
            }
        }

        private static void TeleportTarget(Transform targetTransform, Rigidbody2D targetBody, Vector2 exitPosition)
        {
            if (targetBody != null && targetBody.simulated)
            {
                targetBody.linearVelocity = Vector2.zero;
                targetBody.angularVelocity = 0f;
                targetBody.position = exitPosition;
                return;
            }

            if (targetTransform != null)
            {
                targetTransform.position = exitPosition;
            }
        }

        private void UpdatePortalVfx()
        {
            bool showOldest = _pairedPortal == null || _portalSlotIndex == 0;
            bool showNewest = _pairedPortal != null && _portalSlotIndex == 1;

            if (oldestVfx != null)
            {
                oldestVfx.SetActive(showOldest);
            }

            if (newestVfx != null)
            {
                newestVfx.SetActive(showNewest);
            }
        }
    }
}