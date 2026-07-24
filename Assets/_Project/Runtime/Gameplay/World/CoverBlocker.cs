using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CoverBlocker : MonoBehaviour
    {
        private static readonly RaycastHit2D[] _lineOfSightBuffer = new RaycastHit2D[16];

        [SerializeField] private bool blocksMovement = true;
        [SerializeField] private bool blocksLineOfSight = true;
        [SerializeField] private BoxCollider2D blockingCollider;

        public bool BlocksMovement => blocksMovement;

        public bool BlocksLineOfSight => blocksLineOfSight;

        public BoxCollider2D BlockingCollider => blockingCollider;

        public static float GetNearestBlockDistance(Vector2 origin, Vector2 direction, float maxDistance)
        {
            int count = Physics2D.RaycastNonAlloc(origin, direction, _lineOfSightBuffer, maxDistance);
            float nearest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                Collider2D c = _lineOfSightBuffer[i].collider;
                if (c == null)
                {
                    continue;
                }

                CoverBlocker blocker = c.GetComponent<CoverBlocker>();
                if (blocker != null && blocker.BlocksLineOfSight)
                {
                    float d = _lineOfSightBuffer[i].distance;
                    if (d < nearest)
                    {
                        nearest = d;
                    }
                }
            }

            return nearest;
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
            blockingCollider ??= GetComponent<BoxCollider2D>();
            blockingCollider.isTrigger = false;
        }
    }
}