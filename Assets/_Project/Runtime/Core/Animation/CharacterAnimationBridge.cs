using UnityEngine;

namespace RainbowsEnd.Core.Animation
{
    public sealed class CharacterAnimationBridge : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";

        private void Awake()
        {
            body ??= GetComponentInParent<Rigidbody2D>();
        }

        private void Update()
        {
            if (animator != null && body != null)
            {
                animator.SetFloat(speedParameter, body.linearVelocity.magnitude);
            }
        }
    }
}
