using System.Collections.Generic;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PotDrain : MonoBehaviour
    {
        [SerializeField] private TravellerController travellerController;
        [SerializeField] private CircleCollider2D sensorCollider;

        private readonly List<PotController> _overlappingPots = new List<PotController>();
        private PotController _lastResolvedPot;
        private float _stolenGold;

        public PotController CurrentPot { get; private set; }

        public bool IsDraining { get; private set; }

        public float StolenGold => _stolenGold;

        public void Configure(TravellerController owner, CircleCollider2D collider2D)
        {
            travellerController = owner;
            sensorCollider = collider2D;

            if (sensorCollider != null)
            {
                sensorCollider.isTrigger = true;
            }
        }

        public bool Tick(float deltaTime, float drainGoldPerSecond)
        {
            CurrentPot = ResolveBestOverlappingPot();
            UpdateCurrentPot(CurrentPot);
            if (CurrentPot == null || drainGoldPerSecond <= 0f)
            {
                IsDraining = false;
                return false;
            }

            float stolen = CurrentPot.DrainGold(drainGoldPerSecond * Mathf.Max(0f, deltaTime));
            _stolenGold += stolen;
            IsDraining = stolen > 0f;
            return stolen > 0f;
        }

        public void ResetStolenGold()
        {
            _stolenGold = 0f;
        }

        private void Reset()
        {
            travellerController ??= GetComponent<TravellerController>();
            sensorCollider ??= GetComponent<CircleCollider2D>();
        }

        private void OnValidate()
        {
            travellerController ??= GetComponent<TravellerController>();
            sensorCollider ??= GetComponent<CircleCollider2D>();
        }

        private void OnDisable()
        {
            UpdateCurrentPot(null);
            CurrentPot = null;
            IsDraining = false;
            _stolenGold = 0f;
        }

        private PotController ResolveBestOverlappingPot()
        {
            RefreshOverlappingPots();

            PotController bestPot = null;
            float bestDistanceSquared = float.MaxValue;
            Vector2 position = transform.position;

            for (int index = 0; index < _overlappingPots.Count; index++)
            {
                PotController pot = _overlappingPots[index];
                if (pot == null || pot.HarvestStatus == PotHarvestStatus.Destroyed)
                {
                    continue;
                }

                float distanceSquared = ((Vector2)pot.transform.position - position).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestPot = pot;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return bestPot;
        }

        private void UpdateCurrentPot(PotController newPot)
        {
            if (_lastResolvedPot == newPot)
            {
                return;
            }

            _lastResolvedPot?.SetDrainerActive(this, false);
            _lastResolvedPot = newPot;
            _lastResolvedPot?.SetDrainerActive(this, true);
        }

        private void RefreshOverlappingPots()
        {
            _overlappingPots.Clear();
            if (sensorCollider == null || !sensorCollider.enabled)
            {
                return;
            }

            Collider2D[] overlapResults = Physics2D.OverlapCircleAll(sensorCollider.bounds.center, ResolveWorldRadius());
            for (int index = 0; index < overlapResults.Length; index++)
            {
                Collider2D overlap = overlapResults[index];
                if (overlap == null || overlap == sensorCollider || !overlap.TryGetComponent(out PotController pot))
                {
                    continue;
                }

                if (_overlappingPots.Contains(pot))
                {
                    continue;
                }

                _overlappingPots.Add(pot);
            }
        }

        private float ResolveWorldRadius()
        {
            return sensorCollider != null
                ? Mathf.Max(sensorCollider.bounds.extents.x, sensorCollider.bounds.extents.y, 0.01f)
                : 0.01f;
        }
    }
}
