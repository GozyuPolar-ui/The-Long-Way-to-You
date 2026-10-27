using System;
using UnityEngine;

namespace TargetIndicators.Samples
{
    /// <summary>
    /// Shifts a UI RectTransform to represent a compass tape based on the rotation of the
    /// assigned Camera or CompassForwardReferenceOverride.
    /// </summary>
    [Obsolete("CompassTapeUIController is deprecated in version 1.5.0 and will be removed in a future version. " +
              "It is not used by the samples and is no longer maintained.", false)]
    public class CompassTapeUIController : MonoBehaviour
    {
        [SerializeField, Tooltip("The TargetIndicatorManager that provides the camera and compass override references.")]
        TargetIndicatorManager _targetIndicatorManager;

        [SerializeField, Tooltip("The UI container holding the compass elements (Text, Sprites, Ticks).")]
        RectTransform _tapeContent;

        [SerializeField, Tooltip("The exact pixel width that represents a full 360-degree rotation inside _tapeContent.")]
        float _widthOf360Degrees = 2000f;

        Transform _cachedCameraTransform;

        void Awake()
        {
            if (_targetIndicatorManager == null)
                _targetIndicatorManager = FindAnyObjectByType<TargetIndicatorManager>();
        }

        void LateUpdate()
        {
            if (_targetIndicatorManager == null || _tapeContent == null)
                return;

            var referenceTransform = _targetIndicatorManager.CompassForwardReferenceOverride;

            if (referenceTransform == null)
            {
                var camera = _targetIndicatorManager.Camera;
                if (camera == null)
                    return;

                if (_cachedCameraTransform == null || _cachedCameraTransform.gameObject != camera.gameObject)
                    _cachedCameraTransform = camera.transform;

                referenceTransform = _cachedCameraTransform;
            }

            var yaw = referenceTransform.eulerAngles.y;

            // Ensure yaw stays cleanly within 0-360 before division
            yaw %= 360f;
            if (yaw < 0f)
                yaw += 360f;

            var normalizedYaw = yaw / 360f;

            // Shift the container left as the player rotates right
            var position = _tapeContent.anchoredPosition;
            position.x = -(normalizedYaw * _widthOf360Degrees);
            _tapeContent.anchoredPosition = position;
        }
    }
}
