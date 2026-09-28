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

        private Quaternion _fireFacingRotation;
        private bool _holdFacingDuringFire;
        private bool _fireStateStarted;

        private Quaternion _baseLocalRotation;
        private bool _baseCaptured;

        private void Awake()
        {
            body ??= GetComponentInParent<Rigidbody2D>();
            animator ??= GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
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

        private void LateUpdate()
        {
            if (body == null)
            {
                return;
            }

            if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Die"))
            {
                return;
            }

            if (animator != null)
            {
                animator.SetFloat(speedParameter, body.linearVelocity.magnitude);
            }

            if (visualRoot == null)
            {
                return;
            }

            if (faceMovementDirection)
            {
                Vector2 velocity = body.linearVelocity;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    float facingAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                    float targetZ = facingAngle - 90f;
                    Quaternion target = Quaternion.AngleAxis(targetZ, Vector3.forward) * _baseLocalRotation;
                    visualRoot.localRotation = rotationSmoothing > 0f
                        ? Quaternion.Slerp(visualRoot.localRotation, target, Time.deltaTime * rotationSmoothing)
                        : target;
                }
            }

            if (!_holdFacingDuringFire)
            {
                return;
            }

            bool isFiring = animator != null &&
                (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Shoot"));
            if (isFiring)
            {
                _fireStateStarted = true;
            }
            else if (_fireStateStarted)
            {
                _holdFacingDuringFire = false;
                return;
            }

            visualRoot.rotation = _fireFacingRotation;
        }

        public void TriggerFire(bool lockFacing = false)
        {
            if (animator == null)
            {
                return;
            }

            _holdFacingDuringFire = lockFacing && visualRoot != null;
            _fireStateStarted = false;
            if (_holdFacingDuringFire)
            {
                _fireFacingRotation = visualRoot.rotation;
            }

            animator.SetTrigger("Fire");
        }

        public void TriggerDeath()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (animator == null)
            {
                return;
            }

            _holdFacingDuringFire = false;
            animator.applyRootMotion = false;
            animator.SetTrigger("Die");
        }

        public void ResetDeath()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.applyRootMotion = false;
            animator.ResetTrigger("Die");
            animator.Play("Idle", 0, 0f);
        }
    }
}
