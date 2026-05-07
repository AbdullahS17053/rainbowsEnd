using UnityEngine;

namespace RainbowsEnd.Gameplay.Player
{
    public static class PlayerMovementUtility
    {
        public static Vector2 MoveTowards(Vector2 currentVelocity, Vector2 desiredVelocity, float acceleration, float deltaTime)
        {
            float step = Mathf.Max(0f, acceleration) * Mathf.Max(0f, deltaTime);
            if (step <= 0f)
            {
                return currentVelocity;
            }

            return Vector2.MoveTowards(currentVelocity, desiredVelocity, step);
        }
    }
}