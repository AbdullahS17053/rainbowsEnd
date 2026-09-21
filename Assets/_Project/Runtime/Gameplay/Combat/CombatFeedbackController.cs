using System;
using System.Collections;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Combat
{
    public sealed class CombatFeedbackController : MonoBehaviour
    {
        private static CombatFeedbackController _instance;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float defaultShakeDuration = 0.15f;
        [SerializeField] private float defaultShakeIntensity = 0.2f;

        private Vector3 _originalCameraLocalPos;
        private Coroutine _shakeCoroutine;
        private Coroutine _hitStopCoroutine;

        public static CombatFeedbackController Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            ResolveCamera();
        }

        private void OnEnable()
        {
            ResolveCamera();
        }

        public void PlayScreenShake(float intensity = -1f, float duration = -1f)
        {
            ResolveCamera();
            if (targetCamera == null)
            {
                return;
            }

            float dur = duration > 0f ? duration : defaultShakeDuration;
            float inten = intensity > 0f ? intensity : defaultShakeIntensity;

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                targetCamera.transform.localPosition = _originalCameraLocalPos;
            }

            _shakeCoroutine = StartCoroutine(DoScreenShake(dur, inten));
        }

        public void TriggerHitStop(float durationRealtime = 0.08f, float slowScale = 0.05f)
        {
            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                Time.timeScale = 1f;
            }

            _hitStopCoroutine = StartCoroutine(DoHitStop(durationRealtime, slowScale));
        }

        public static void DelayedDespawn(MonoBehaviour runner, Action onComplete, float delaySeconds = 3.0f)
        {
            if (runner != null && runner.gameObject.activeInHierarchy)
            {
                runner.StartCoroutine(DoDelayedAction(onComplete, delaySeconds));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private static IEnumerator DoDelayedAction(Action onComplete, float delay)
        {
            yield return new WaitForSeconds(delay);
            onComplete?.Invoke();
        }

        private IEnumerator DoScreenShake(float duration, float intensity)
        {
            _originalCameraLocalPos = targetCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float strength = (1f - (elapsed / duration)) * intensity;
                Vector3 offset = (Vector3)UnityEngine.Random.insideUnitCircle * strength;
                targetCamera.transform.localPosition = _originalCameraLocalPos + offset;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            targetCamera.transform.localPosition = _originalCameraLocalPos;
            _shakeCoroutine = null;
        }

        private IEnumerator DoHitStop(float durationRealtime, float slowScale)
        {
            float originalScale = Time.timeScale;
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(durationRealtime);
            Time.timeScale = originalScale;
            _hitStopCoroutine = null;
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                _originalCameraLocalPos = targetCamera.transform.localPosition;
            }
        }
    }
}
