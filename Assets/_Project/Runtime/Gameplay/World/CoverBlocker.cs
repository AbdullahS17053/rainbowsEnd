using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CoverBlocker : MonoBehaviour
    {
        [SerializeField] private bool blocksMovement = true;
        [SerializeField] private bool blocksLineOfSight = true;
        [SerializeField] private BoxCollider2D blockingCollider;

        public bool BlocksMovement => blocksMovement;

        public bool BlocksLineOfSight => blocksLineOfSight;

        public BoxCollider2D BlockingCollider => blockingCollider;

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