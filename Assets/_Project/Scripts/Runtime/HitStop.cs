using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class HitStop : MonoBehaviour
    {
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private CombatFeelSettings settings;

        private float originalAnimatorSpeed = 1f;
        private float speedMultiplier = 1f;
        private float hitStopRemaining;
        private float slowMotionRemaining;
        private float slowMotionScale = 1f;
        private bool animatorSpeedOverridden;

        public bool IsHitStopped => hitStopRemaining > 0f;

        private void Awake()
        {
            if (targetAnimator == null)
                targetAnimator = GetComponentInChildren<Animator>(true);
            if (targetAnimator != null)
                originalAnimatorSpeed = targetAnimator.speed;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            if (hitStopRemaining > 0f)
                hitStopRemaining = Mathf.Max(0f, hitStopRemaining - delta);
            if (slowMotionRemaining > 0f)
                slowMotionRemaining = Mathf.Max(0f, slowMotionRemaining - delta);
            ApplySpeed();
        }

        private void OnDisable()
        {
            hitStopRemaining = 0f;
            slowMotionRemaining = 0f;
            speedMultiplier = 1f;
            if (targetAnimator != null)
                targetAnimator.speed = originalAnimatorSpeed;
            animatorSpeedOverridden = false;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0f, multiplier);
            ApplySpeed();
        }

        public void ApplyHitStop(float duration)
        {
            if (settings == null || !settings.hitStopEnabled || !settings.accessibilityHitStopEnabled)
                return;

            float minimumFrame = Mathf.Max(Time.unscaledDeltaTime, 1f / 60f);
            hitStopRemaining = Mathf.Max(hitStopRemaining, Mathf.Max(duration, minimumFrame));
            ApplySpeed();
        }

        public void ApplyTemporarySlowMotion(float scale, float duration)
        {
            slowMotionScale = Mathf.Clamp(scale, 0.01f, 1f);
            slowMotionRemaining = Mathf.Max(slowMotionRemaining, Mathf.Max(duration, Time.unscaledDeltaTime));
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            if (targetAnimator == null)
                return;

            if (hitStopRemaining <= 0f && slowMotionRemaining <= 0f &&
                Mathf.Approximately(speedMultiplier, 1f))
            {
                if (animatorSpeedOverridden)
                {
                    targetAnimator.speed = originalAnimatorSpeed;
                    animatorSpeedOverridden = false;
                }
                return;
            }

            float effectScale = hitStopRemaining > 0f
                ? (settings != null ? Mathf.Clamp(settings.frozenAnimatorSpeed, 0.01f, 0.1f) : 0.02f)
                : slowMotionRemaining > 0f ? slowMotionScale : 1f;
            targetAnimator.speed = originalAnimatorSpeed * speedMultiplier * effectScale;
            animatorSpeedOverridden = true;
        }
    }
}
