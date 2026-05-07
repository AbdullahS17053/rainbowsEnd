using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    public sealed class FaeTreeAnchor : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 0.45f);
        }
    }
}