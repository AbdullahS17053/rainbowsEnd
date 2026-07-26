using UnityEngine;
using UnityEngine.AI;

namespace RainbowsEnd.Gameplay.World
{
    public static class NavMeshPathfinder
    {
        private static readonly NavMeshPath _path = new NavMeshPath();
        private const float WaypointReachDistanceSq = 0.25f;

        public static bool TryGetPath(Vector2 from, Vector2 to, out Vector2[] waypoints)
        {
            Vector3 from3D = new Vector3(from.x, from.y, 0f);
            Vector3 to3D = new Vector3(to.x, to.y, 0f);

            if (NavMesh.CalculatePath(from3D, to3D, NavMesh.AllAreas, _path))
            {
                int corners = _path.corners.Length;
                if (corners > 0)
                {
                    waypoints = new Vector2[corners];
                    for (int i = 0; i < corners; i++)
                    {
                        waypoints[i] = _path.corners[i];
                    }

                    return true;
                }
            }

            waypoints = null;
            return false;
        }

        public static Vector2 GetSteeringDirection(Vector2 position, Vector2[] waypoints, ref int waypointIndex)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return Vector2.zero;
            }

            int count = waypoints.Length;
            waypointIndex = Mathf.Clamp(waypointIndex, 0, count - 1);
            Vector2 toWaypoint = waypoints[waypointIndex] - position;

            while (waypointIndex < count - 1 && toWaypoint.sqrMagnitude < WaypointReachDistanceSq)
            {
                waypointIndex++;
                toWaypoint = waypoints[waypointIndex] - position;
            }

            if (toWaypoint.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            return toWaypoint.normalized;
        }
    }
}
