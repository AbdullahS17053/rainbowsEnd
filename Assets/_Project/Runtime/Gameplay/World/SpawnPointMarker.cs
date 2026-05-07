using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    public sealed class SpawnPointMarker : MonoBehaviour
    {
        [SerializeField] private SpawnPointKind kind;
        [SerializeField] private string markerId;
        [SerializeField] [Min(0)] private int order;

        public SpawnPointKind Kind => kind;

        public string MarkerId => markerId;

        public int Order => order;

        public void Configure(SpawnPointKind spawnPointKind, string id, int markerOrder = 0)
        {
            kind = spawnPointKind;
            markerId = id;
            order = Mathf.Max(0, markerOrder);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                SpawnPointKind.Player => Color.green,
                SpawnPointKind.Unicorn => Color.cyan,
                SpawnPointKind.Pot => new Color(1f, 0.85f, 0.2f),
                SpawnPointKind.EnemyEdge => Color.red,
                SpawnPointKind.FaeTree => new Color(0.6f, 1f, 0.6f),
                _ => Color.white
            };

            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}