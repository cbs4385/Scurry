using UnityEngine;

namespace Scurry.UI
{
    /// <summary>
    /// Simple screen shake component. Attach to the Camera or a UI parent.
    /// Call Shake() to trigger a shake effect.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        private Vector3 originalPosition;
        private float shakeDuration;
        private float shakeIntensity;
        private float shakeDecay = 1.5f;

        private void Awake()
        {
            Debug.Log("[ScreenShake] Awake: initializing");
            originalPosition = transform.localPosition;
        }

        /// <summary>
        /// Triggers a screen shake effect.
        /// </summary>
        /// <param name="intensity">Maximum offset in pixels/units.</param>
        /// <param name="duration">Duration in seconds.</param>
        public void Shake(float intensity = 5f, float duration = 0.3f)
        {
            Debug.Log($"[ScreenShake] Shake: intensity={intensity}, duration={duration}");
            shakeIntensity = intensity;
            shakeDuration = duration;
            originalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (shakeDuration > 0)
            {
                float currentIntensity = shakeIntensity * (shakeDuration / (shakeDuration + Time.deltaTime * shakeDecay));
                transform.localPosition = originalPosition + (Vector3)Random.insideUnitCircle * currentIntensity;

                shakeDuration -= Time.deltaTime;

                if (shakeDuration <= 0)
                {
                    shakeDuration = 0;
                    transform.localPosition = originalPosition;
                    Debug.Log("[ScreenShake] Update: shake complete, position restored");
                }
            }
        }
    }
}
