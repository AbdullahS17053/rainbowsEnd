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

        public Animator ActiveAnimator => GetActiveAnimator();

        public void SetActiveAnimator(Animator newAnimator)
        {
            animator = newAnimator;
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }

        public Animator GetActiveAnimator()
        {
            if (animator != null && animator.gameObject.activeInHierarchy)
            {
                return animator;
            }

            Animator[] activeAnimators = GetComponentsInChildren<Animator>(false);
            if (activeAnimators != null && activeAnimators.Length > 0)
            {
                animator = activeAnimators[0];
                animator.applyRootMotion = false;
                return animator;
            }

            return animator;
        }

        private void Awake()
        {
            body ??= GetComponentInParent<Rigidbody2D>();
            animator = GetActiveAnimator() ?? GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
            CaptureBase();
        }

        private void OnEnable()
        {
            CaptureBase();
            Animator active = GetActiveAnimator();
            if (active != null)
            {
                active.applyRootMotion = false;
            }
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

            Animator activeAnim = GetActiveAnimator();

            if (activeAnim != null && activeAnim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Die"))
            {
                return;
            }

            if (activeAnim != null)
            {
                float speed = body.linearVelocity.magnitude;
                activeAnim.SetFloat(speedParameter, speed);

                if (speed > 0.15f && activeAnim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Shoot"))
                {
                    activeAnim.Play("Walk", 0);
                }
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

            bool isFiring = activeAnim != null &&
                (activeAnim.IsInTransition(0) || activeAnim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Shoot"));
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
            Animator activeAnim = GetActiveAnimator();
            if (activeAnim == null)
            {
                return;
            }

            _holdFacingDuringFire = lockFacing && visualRoot != null;
            _fireStateStarted = false;
            if (_holdFacingDuringFire)
            {
                _fireFacingRotation = visualRoot.rotation;
            }

            // When actively moving, keep leg walking locomotion active so character doesn't slide
            if (body != null && body.linearVelocity.sqrMagnitude > 0.04f)
            {
                return;
            }

            activeAnim.SetTrigger("Fire");
        }

        public void TriggerDeath()
        {
            Animator activeAnim = GetActiveAnimator();
            if (activeAnim == null)
            {
                return;
            }

            _holdFacingDuringFire = false;
            activeAnim.applyRootMotion = false;
            activeAnim.SetTrigger("Die");
        }

        public void ResetDeath()
        {
            Animator activeAnim = GetActiveAnimator();
            if (activeAnim == null || activeAnim.runtimeAnimatorController == null)
            {
                return;
            }

            activeAnim.applyRootMotion = false;
            activeAnim.ResetTrigger("Die");
            activeAnim.Play("Idle", 0, 0f);
        }
    }
}
