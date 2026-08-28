using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public enum ArenaBoundarySide
    {
        Top = 0,
        Right = 1,
        Bottom = 2,
        Left = 3
    }

    public static class UnicornRoamingUtility
    {
        public static Vector2 GetWallNormal(ArenaBoundarySide side)
        {
            return side switch
            {
                ArenaBoundarySide.Top => Vector2.down,
                ArenaBoundarySide.Right => Vector2.left,
                ArenaBoundarySide.Bottom => Vector2.up,
                _ => Vector2.right
            };
        }

        public static bool TryGetBoundaryIntersection(
            Vector2 origin,
            Vector2 direction,
            Rect bounds,
            ArenaBoundarySide? excludeSide,
            out ArenaBoundarySide hitSide,
            out Vector2 hitPoint)
        {
            float closestT = float.MaxValue;
            hitSide = default;
            hitPoint = origin;
            bool anyHit = false;

            if (Mathf.Abs(direction.sqrMagnitude) < 0.0001f)
            {
                return false;
            }

            (ArenaBoundarySide side, float t)[] candidates =
            {
                (ArenaBoundarySide.Top, direction.y > 0f ? (bounds.yMax - origin.y) / direction.y : float.MaxValue),
                (ArenaBoundarySide.Bottom, direction.y < 0f ? (bounds.yMin - origin.y) / direction.y : float.MaxValue),
                (ArenaBoundarySide.Right, direction.x > 0f ? (bounds.xMax - origin.x) / direction.x : float.MaxValue),
                (ArenaBoundarySide.Left, direction.x < 0f ? (bounds.xMin - origin.x) / direction.x : float.MaxValue),
            };

            foreach (var (side, t) in candidates)
            {
                if (t <= 0f || t >= closestT)
                {
                    continue;
                }

                if (excludeSide.HasValue && side == excludeSide.Value)
                {
                    continue;
                }

                float xAtT = origin.x + direction.x * t;
                float yAtT = origin.y + direction.y * t;

                bool onSide = side switch
                {
                    ArenaBoundarySide.Top or ArenaBoundarySide.Bottom => xAtT >= bounds.xMin && xAtT <= bounds.xMax,
                    _ => yAtT >= bounds.yMin && yAtT <= bounds.yMax,
                };

                if (!onSide)
                {
                    continue;
                }

                closestT = t;
                hitSide = side;
                anyHit = true;
            }

            if (anyHit)
            {
                hitPoint = origin + direction * closestT;
            }

            return anyHit;
        }

public static ArenaBoundarySide SelectNextBoundarySide(ArenaBoundarySide? previousSide, float randomValue)
        {
            float clampedRandom = Mathf.Clamp01(randomValue);
            if (!previousSide.HasValue)
            {
                return clampedRandom < 0.25f
                    ? ArenaBoundarySide.Top
                    : clampedRandom < 0.5f
                        ? ArenaBoundarySide.Right
                        : clampedRandom < 0.75f
                            ? ArenaBoundarySide.Bottom
                            : ArenaBoundarySide.Left;
            }

            if (previousSide.Value == ArenaBoundarySide.Top)
            {
                return ArenaBoundarySide.Bottom;
            }

            return previousSide.Value switch
            {
                ArenaBoundarySide.Right => clampedRandom < 0.5f ? ArenaBoundarySide.Left : clampedRandom < 0.75f ? ArenaBoundarySide.Top : ArenaBoundarySide.Bottom,
                ArenaBoundarySide.Bottom => clampedRandom < 0.5f ? ArenaBoundarySide.Top : clampedRandom < 0.75f ? ArenaBoundarySide.Left : ArenaBoundarySide.Right,
                _ => clampedRandom < 0.5f ? ArenaBoundarySide.Right : clampedRandom < 0.75f ? ArenaBoundarySide.Top : ArenaBoundarySide.Bottom
            };
        }

        public static Vector2 SelectBoundaryTarget(Rect bounds, ArenaBoundarySide side, float sideLerp, float cornerPadding)
        {
            float clampedLerp = Mathf.Clamp01(sideLerp);
            float horizontalPadding = Mathf.Clamp(cornerPadding, 0f, bounds.width * 0.45f);
            float verticalPadding = Mathf.Clamp(cornerPadding, 0f, bounds.height * 0.45f);
            float x = Mathf.Lerp(bounds.xMin + horizontalPadding, bounds.xMax - horizontalPadding, clampedLerp);
            float y = Mathf.Lerp(bounds.yMin + verticalPadding, bounds.yMax - verticalPadding, clampedLerp);

            return side switch
            {
                ArenaBoundarySide.Top => new Vector2(x, bounds.yMax),
                ArenaBoundarySide.Right => new Vector2(bounds.xMax, y),
                ArenaBoundarySide.Bottom => new Vector2(x, bounds.yMin),
                _ => new Vector2(bounds.xMin, y)
            };
        }

        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float speed, float deltaTime)
        {
            return Vector2.MoveTowards(current, target, Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
        }

        public static Vector2 ClampToRect(Vector2 point, Rect bounds)
        {
            return new Vector2(
                Mathf.Clamp(point.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(point.y, bounds.yMin, bounds.yMax));
        }
    }
}