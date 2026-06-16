using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    public sealed class ArenaBoundsAuthoring : MonoBehaviour
    {
        [SerializeField] private Vector2 center;
        [SerializeField] private Vector2 size = new Vector2(18f, 10f);

        public Vector2 Center => center;

        public Vector2 Size => size;

        public Rect WorldRect => new Rect(center - (size * 0.5f), size);

        public void Configure(Vector2 boundsCenter, Vector2 boundsSize)
        {
            center = boundsCenter;
            size = new Vector2(Mathf.Max(0.1f, boundsSize.x), Mathf.Max(0.1f, boundsSize.y));
        }

        public bool Contains(Vector2 point)
        {
            return WorldRect.Contains(point);
        }

        public Vector2 Clamp(Vector2 point)
        {
            Rect rect = WorldRect;
            return new Vector2(
                Mathf.Clamp(point.x, rect.xMin, rect.xMax),
                Mathf.Clamp(point.y, rect.yMin, rect.yMax));
        }

        public Vector2 GetNearestEdgePoint(Vector2 from)
        {
            Rect rect = WorldRect;
            Vector2 clamped = Clamp(from);

            float distLeft = Mathf.Abs(clamped.x - rect.xMin);
            float distRight = Mathf.Abs(clamped.x - rect.xMax);
            float distBottom = Mathf.Abs(clamped.y - rect.yMin);
            float distTop = Mathf.Abs(clamped.y - rect.yMax);

            float minDist = Mathf.Min(distLeft, distRight, distBottom, distTop);

            if (Mathf.Approximately(minDist, distLeft))
            {
                return new Vector2(rect.xMin, clamped.y);
            }

            if (Mathf.Approximately(minDist, distRight))
            {
                return new Vector2(rect.xMax, clamped.y);
            }

            if (Mathf.Approximately(minDist, distBottom))
            {
                return new Vector2(clamped.x, rect.yMin);
            }

            return new Vector2(clamped.x, rect.yMax);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}