using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    /// <summary>
    /// Evaluates reachable perimeter exits (enemy edge spawn markers, gates, and boundary edges)
    /// to provide mobs with the shortest navigable NavMesh path to escape the arena through walls.
    /// </summary>
    public static class EnemyEscapeRoutingUtility
    {
        private static readonly List<Vector2> CandidatePoints = new List<Vector2>(16);

        public static bool TryFindBestEscapePath(
            Vector2 currentPosition,
            ArenaBoundsAuthoring arenaBounds,
            out Vector2 bestExitPoint,
            out Vector2[] bestPath)
        {
            CandidatePoints.Clear();

            // 1. Gather all EnemyEdge spawn markers (gates/doors/perimeter openings)
            SpawnPointMarker[] markers = Object.FindObjectsByType<SpawnPointMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null && markers[i].Kind == SpawnPointKind.EnemyEdge)
                {
                    CandidatePoints.Add(markers[i].transform.position);
                }
            }

            // 2. Add arena bounds edges and cardinal openings
            if (arenaBounds != null)
            {
                Rect r = arenaBounds.WorldRect;
                CandidatePoints.Add(arenaBounds.GetNearestEdgePoint(currentPosition));
                CandidatePoints.Add(new Vector2(r.center.x, r.yMax)); // North edge
                CandidatePoints.Add(new Vector2(r.center.x, r.yMin)); // South edge
                CandidatePoints.Add(new Vector2(r.xMin, r.center.y)); // West edge
                CandidatePoints.Add(new Vector2(r.xMax, r.center.y)); // East edge
            }

            Vector2 bestExit = Vector2.zero;
            Vector2[] shortestPath = null;
            float shortestDistance = float.MaxValue;

            for (int i = 0; i < CandidatePoints.Count; i++)
            {
                Vector2 candidate = CandidatePoints[i];
                if (NavMeshPathfinder.TryGetPath(currentPosition, candidate, out Vector2[] path))
                {
                    float pathDist = CalculatePathLength(path);
                    if (pathDist < shortestDistance)
                    {
                        shortestDistance = pathDist;
                        shortestPath = path;
                        bestExit = candidate;
                    }
                }
            }

            if (shortestPath != null && shortestPath.Length > 0)
            {
                bestExitPoint = bestExit;
                bestPath = shortestPath;
                return true;
            }

            // Fallback: nearest edge point on bounds
            bestExitPoint = arenaBounds != null
                ? arenaBounds.GetNearestEdgePoint(currentPosition)
                : currentPosition + Vector2.up * 20f;
            bestPath = null;
            return false;
        }

        public static float CalculatePathLength(Vector2[] waypoints)
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return 0f;
            }

            float totalLength = 0f;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                totalLength += Vector2.Distance(waypoints[i], waypoints[i + 1]);
            }

            return totalLength;
        }
    }
}
