using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ConcealmentZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D zoneCollider;

        public BoxCollider2D ZoneCollider => zoneCollider;

        public bool Contains(Vector2 point)
        {
            return zoneCollider != null && zoneCollider.OverlapPoint(point);
        }

        private void Reset()
        {
            EnsureCollider();
        }

        private void OnValidate()
        {
            EnsureCollider();
        }

        private void EnsureCollider()
        {
            zoneCollider ??= GetComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
        }
    }
}