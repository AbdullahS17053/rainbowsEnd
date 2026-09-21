using System;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Systems.Pooling;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class WeaponPickup : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] [Min(0.1f)] private float pickupDurationSeconds = 1.0f;
        [SerializeField] private LineRenderer progressRing;
        [SerializeField] private Color ringColor = new Color(0.2f, 0.9f, 1f, 0.8f);

        private PrefabPool _owningPool;
        private Core.Config.WeaponConfig _weaponConfig;
        private int _ammoCount;
        private PlayerRoot _overlappingPlayer;
        private float _currentStayTime;

        public static event Action<WeaponPickup, Core.Config.WeaponConfig> PickupStarted;
        public static event Action<WeaponPickup, Core.Config.WeaponConfig> PickupCompleted;

        public float ProgressNormalized => pickupDurationSeconds > 0f ? Mathf.Clamp01(_currentStayTime / pickupDurationSeconds) : 0f;

        public void Activate(PrefabPool owningPool, Core.Config.WeaponConfig weaponConfig, int ammoCount)
        {
            _owningPool = owningPool;
            _weaponConfig = weaponConfig;
            _ammoCount = Mathf.Max(0, ammoCount);
            _currentStayTime = 0f;
            _overlappingPlayer = null;

            EnsureCollider();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            EnsureProgressRing();
            SetRingVisible(false);
        }

        public void OnSpawned()
        {
            EnsureCollider();
            EnsureProgressRing();
            SetRingVisible(false);
            _owningPool = null;
            _weaponConfig = null;
            _ammoCount = 0;
            _overlappingPlayer = null;
            _currentStayTime = 0f;
        }

        public void OnDespawned()
        {
            SetRingVisible(false);
            _owningPool = null;
            _weaponConfig = null;
            _ammoCount = 0;
            _overlappingPlayer = null;
            _currentStayTime = 0f;
        }

        private void Reset()
        {
            EnsureCollider();
        }

        private void OnValidate()
        {
            EnsureCollider();
        }

        private void Awake()
        {
            EnsureCollider();
            EnsureProgressRing();
        }

        private void Update()
        {
            if (_overlappingPlayer == null || _weaponConfig == null || _ammoCount <= 0)
            {
                return;
            }

            if (_overlappingPlayer.IsDisabled)
            {
                _currentStayTime = 0f;
                UpdateProgressRing();
                return;
            }

            _currentStayTime += Time.deltaTime;
            UpdateProgressRing();

            if (_currentStayTime >= pickupDurationSeconds)
            {
                CompletePickup();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_weaponConfig == null || _ammoCount <= 0)
            {
                return;
            }

            PlayerRoot player = other.GetComponentInParent<PlayerRoot>();
            if (player == null || player.IsDisabled)
            {
                return;
            }

            _overlappingPlayer = player;
            _currentStayTime = 0f;
            SetRingVisible(true);
            UpdateProgressRing();
            PickupStarted?.Invoke(this, _weaponConfig);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerRoot player = other.GetComponentInParent<PlayerRoot>();
            if (player != null && player == _overlappingPlayer)
            {
                _overlappingPlayer = null;
                _currentStayTime = 0f;
                SetRingVisible(false);
            }
        }

        private void CompletePickup()
        {
            if (_overlappingPlayer != null && _overlappingPlayer.WeaponInventory != null)
            {
                _overlappingPlayer.WeaponInventory.EquipTemporaryWeapon(_weaponConfig, _ammoCount);
                PickupCompleted?.Invoke(this, _weaponConfig);
            }

            SetRingVisible(false);
            _overlappingPlayer = null;
            _currentStayTime = 0f;

            PrefabPool owningPool = _owningPool;
            _owningPool = null;
            owningPool?.Return(gameObject);
        }

        private void EnsureCollider()
        {
            triggerCollider ??= GetComponent<Collider2D>();
        }

        private void EnsureProgressRing()
        {
            if (progressRing != null)
            {
                return;
            }

            Transform child = transform.Find("ProgressRing");
            if (child != null)
            {
                progressRing = child.GetComponent<LineRenderer>();
                if (progressRing != null)
                {
                    return;
                }
            }

            if (!Application.isPlaying)
            {
                return;
            }

            GameObject ringObj = new GameObject("ProgressRing");
            ringObj.transform.SetParent(transform, false);
            progressRing = ringObj.AddComponent<LineRenderer>();
            progressRing.useWorldSpace = false;
            progressRing.loop = false;
            progressRing.startWidth = 0.08f;
            progressRing.endWidth = 0.08f;
            progressRing.startColor = ringColor;
            progressRing.endColor = ringColor;
            progressRing.material = new Material(Shader.Find("Sprites/Default"));
            progressRing.enabled = false;
        }

        private void UpdateProgressRing()
        {
            if (progressRing == null)
            {
                return;
            }

            float progress = ProgressNormalized;
            int totalSegments = 32;
            int activePoints = Mathf.Max(2, Mathf.CeilToInt(totalSegments * progress));

            progressRing.positionCount = activePoints;
            float radius = 0.75f;

            for (int i = 0; i < activePoints; i++)
            {
                float angle = (i / (float)totalSegments) * 360f * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius;
                progressRing.SetPosition(i, new Vector3(x, y, 0f));
            }

            progressRing.startColor = ringColor;
            progressRing.endColor = ringColor;
            SetRingVisible(true);
        }

        private void SetRingVisible(bool isVisible)
        {
            if (progressRing != null)
            {
                progressRing.enabled = isVisible;
            }
        }
    }
}