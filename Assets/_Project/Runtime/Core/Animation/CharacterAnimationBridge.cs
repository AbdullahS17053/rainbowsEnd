using UnityEngine;

namespace RainbowsEnd.Core.Animation
{
    public sealed class CharacterAnimationBridge : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool faceMovementDirection;
        [SerializeField] [Min(0f)] private float rotationSmoothing = 12f;

        private Quaternion _baseLocalRotation;
        private bool _baseCaptured;

        private void Awake()
        {
            body ??= GetComponentInParent<Rigidbody2D>();
            CaptureBase();
        }

        private void OnEnable()
        {
            CaptureBase();
        }

        private void CaptureBase()
        {
            if (visualRoot != null && !_baseCaptured)
            {
                _baseLocalRotation = visualRoot.localRotation;
                _baseCaptured = true;
            }
        }

        private void Update()
        {
            if (body == null)
            {
                return;
            }

            if (animator != null)
            {
                animator.SetFloat(speedParameter, body.linearVelocity.magnitude);
            }

            if (!faceMovementDirection || visualRoot == null)
            {
                return;
            }

            Vector2 vel = body.linearVelocity;
            if (vel.sqrMagnitude > 0.01f)
            {
                float facingAngle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                float targetZ = facingAngle - 90f;
                Quaternion target = Quaternion.AngleAxis(targetZ, Vector3.forward) * _baseLocalRotation;
                visualRoot.localRotation = rotationSmoothing > 0f
                    ? Quaternion.Slerp(visualRoot.localRotation, target, Time.deltaTime * rotationSmoothing)
                    : target;
            }
        }

        public void TriggerFire()
        {
            if (animator != null)
            {
                animator.SetTrigger("Fire");
            }
        }
    }
}
