using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    public static class PlayerAimResolver
    {
        public static Vector2 ResolveAimDirection(
            Vector2 aimValue,
            bool aimFromPointer,
            Camera worldCamera,
            Vector3 actorPosition,
            Vector2 fallbackDirection,
            float controllerDeadzone = 0.2f)
        {
            if (!aimFromPointer)
            {
                float minMagnitude = Mathf.Max(0f, controllerDeadzone);
                if (aimValue.sqrMagnitude >= minMagnitude * minMagnitude)
                {
                    return aimValue.normalized;
                }
            }
            else if (worldCamera != null)
            {
                float screenDepth = Mathf.Abs(actorPosition.z - worldCamera.transform.position.z);
                Vector3 screenPoint = new Vector3(aimValue.x, aimValue.y, screenDepth);
                Vector3 worldPoint = worldCamera.ScreenToWorldPoint(screenPoint);
                Vector2 aimDirection = worldPoint - actorPosition;
                if (aimDirection.sqrMagnitude > 0.0001f)
                {
                    return aimDirection.normalized;
                }
            }

            return fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector2.up;
        }
    }
}