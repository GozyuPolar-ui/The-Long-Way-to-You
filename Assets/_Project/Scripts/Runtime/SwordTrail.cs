using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class SwordTrail : MonoBehaviour
    {
        [SerializeField] private CombatFeelSettings settings;
        [SerializeField] private TrailRenderer trail;

        private void Awake()
        {
            if (trail == null)
                trail = GetComponentInChildren<TrailRenderer>(true);
            SetTrailActive(false);
        }

        public void SetTrailActive(bool active)
        {
            if (trail == null)
                return;

            bool enabledBySettings = settings != null && settings.swordTrailEnabled &&
                                     settings.visualEffectsEnabled;
            trail.emitting = active && enabledBySettings;
            trail.enabled = enabledBySettings;
            if (settings != null)
            {
                trail.startColor = settings.trailColor;
                Color endColor = settings.trailColor;
                endColor.a = 0f;
                trail.endColor = endColor;
            }
        }
    }
}
