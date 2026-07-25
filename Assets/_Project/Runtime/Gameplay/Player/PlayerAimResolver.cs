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
                Ray ray = worldCamera.ScreenPointToRay(new Vector3(aimValue.x, aimValue.y, 0f));
                Plane groundPlane = new Plane(Vector3.back, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 worldPoint = ray.GetPoint(distance);
                    Vector2 aimDirection = worldPoint - actorPosition;
                    if (aimDirection.sqrMagnitude > 0.0001f)
                    {
                        return aimDirection.normalized;
                    }
                }
            }

            return fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector2.up;
        }
    }
}