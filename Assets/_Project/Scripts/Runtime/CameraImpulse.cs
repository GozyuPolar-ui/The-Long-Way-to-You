using Unity.Cinemachine;
using UnityEngine;

namespace Prince
{
    [DisallowMultipleComponent]
    public sealed class CameraImpulse : MonoBehaviour
    {
        [SerializeField] private CombatFeelSettings settings;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        private bool warnedMissingSource;
        private bool cameraListenersReady;

        private void Awake()
        {
            if (impulseSource == null)
                impulseSource = GetComponent<CinemachineImpulseSource>();
            if (settings != null && settings.cameraImpulseEnabled && settings.visualEffectsEnabled)
                cameraListenersReady = EnsureGameplayCameraListeners();
        }

        public void Emit(CombatHitStopTier tier, float accessibilityScale)
        {
            if (settings == null || !settings.cameraImpulseEnabled || !settings.visualEffectsEnabled)
                return;
            if (!cameraListenersReady)
                cameraListenersReady = EnsureGameplayCameraListeners();
            if (impulseSource == null)
            {
                if (!warnedMissingSource)
                {
                    Debug.LogWarning(
                        "CameraImpulse has no CinemachineImpulseSource; camera shake is unavailable.",
                        this);
                    warnedMissingSource = true;
                }
                return;
            }

            float strength = settings.GetCameraImpulse(tier) * Mathf.Clamp01(accessibilityScale);
            if (strength > 0f)
            {
                if (impulseSource.ImpulseDefinition != null)
                    impulseSource.ImpulseDefinition.ImpulseDuration =
                        Mathf.Clamp(settings.cameraImpulseDuration, 0.2f, 0.35f);
                impulseSource.GenerateImpulseWithForce(strength);
            }
        }

        private bool EnsureGameplayCameraListeners()
        {
            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            bool foundCamera = false;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == null || cameras[i].gameObject.scene != gameObject.scene)
                    continue;
                foundCamera = true;
                if (cameras[i].GetComponent<CinemachineImpulseListener>() == null)
                    cameras[i].gameObject.AddComponent<CinemachineImpulseListener>();
            }

            return foundCamera;
        }
    }
}
