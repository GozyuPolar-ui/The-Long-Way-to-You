using System;
using TMPro;
using UnityEngine;

namespace TargetIndicators.Samples
{
    /// <summary>
    /// Control the instance of a <c>padded</c>, <c>absolute</c>, and <c>unbounded</c> visual indicator.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class VisualIndicator : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField, Tooltip("Required. The indicator's main visuals, such as the icon and background. Parent them " +
                                 "to this RectTransform.")]
        protected RectTransform _coreContent;

        [SerializeField, Tooltip("Required. The content that rotates to point at the target, such as the arrow. Parent it " +
                                 "to this RectTransform, pointing right (+X) and offset along +X.")]
        protected RectTransform _rotationContent;

        [SerializeField, Tooltip("The text label to display distance to the target.")]
        TextMeshProUGUI _distanceLabel;

        [Header("Settings")]
        [SerializeField, Tooltip("The condition for when the core content should be visible.")]
        protected IndicatorVisibility _coreContentVisibility = IndicatorVisibility.Always;

        [SerializeField, Tooltip("The condition for when the rotation content should be visible.")]
        protected IndicatorVisibility _rotationContentVisibility = IndicatorVisibility.OutsideBoundary;

        [SerializeField, Tooltip("The condition for when the distance label should be visible.")]
        DistanceLabelVisibility _distanceLabelVisibility = DistanceLabelVisibility.LookAt;

        [SerializeField, Range(0, 1), Tooltip("How directly the camera must look at the target to show the distance " +
                                              "label when Distance Label Visibility is Look At. 1 means exactly at the " +
                                              "target, 0.95 is within about 18 degrees, and 0 is within 90 degrees.")]
        float _lookAtDotThreshold = 0.95f;

        [SerializeField, Min(0), Tooltip("The duration, in seconds, it takes for the distance label to fade in or out.")]
        float _distanceLabelFadeTime = 0.15f;

        /// <summary>
        /// The scale of the canvas used to calculate the position to place the visual indicator.
        /// </summary>
        public float CanvasScale { get; set; } = 1;

        /// <summary>
        /// The ID that represents the target indicator this visual indicator is associated with.
        /// </summary>
        public TargetIndicatorId TargetIndicatorId { get; set; }

        /// <summary>
        /// Gets or sets the condition for when the core content should be visible.
        /// </summary>
        public IndicatorVisibility CoreContentVisibility
        {
            get => _coreContentVisibility;
            set => _coreContentVisibility = value;
        }

        /// <summary>
        /// Gets or sets the condition for when the rotation content should be visible.
        /// </summary>
        public IndicatorVisibility RotationContentVisibility
        {
            get => _rotationContentVisibility;
            set => _rotationContentVisibility = value;
        }

        /// <summary>
        /// Gets or sets the condition for when the distance label should be visible.
        /// </summary>
        public DistanceLabelVisibility DistanceLabelVisibility
        {
            get => _distanceLabelVisibility;
            set => _distanceLabelVisibility = value;
        }

        /// <summary>
        /// Gets or sets the required dot product threshold to show the distance label when <see cref="DistanceLabelVisibility"/>
        /// is set to <see cref="DistanceLabelVisibility.LookAt"/>.
        /// Evaluated against the <see cref="TargetIndicator.LookAtDot"/> property. Ranges from 0 (90-degree radius cone)
        /// to 1 (exact center).
        /// </summary>
        public float LookAtDotThreshold
        {
            get => _lookAtDotThreshold;
            set => _lookAtDotThreshold = value;
        }

        /// <summary>
        /// Gets or sets the duration, in seconds, it takes for the distance label to fade in or out.
        /// </summary>
        public float DistanceLabelFadeTime
        {
            get => _distanceLabelFadeTime;
            set => _distanceLabelFadeTime = value;
        }

        /// <summary>
        /// The name of the GameObject that represents the rotation content that is searched for on <see cref="Reset"/>.
        /// </summary>
        protected string _rotationPivotDefaultName = "RotationContent";

        /// <summary>
        /// The <see cref="RectTransform"/> of this GameObject.
        /// </summary>
        protected RectTransform _rectTransform;

        /// <summary>
        /// The GameObject of the core content. This is used to cache GameObject lookups from the core content
        /// <see cref="RectTransform"/>.
        /// </summary>
        protected GameObject _contentGO;

        /// <summary>
        /// The GameObject of the rotation content. This is used to cache GameObject lookups from the rotation content
        /// <see cref="RectTransform"/>.
        /// </summary>
        protected GameObject _rotationContentGO;

        int _lastDistanceInt = -1;
        float _distanceLabelCurrentAlpha = -1f;

        protected virtual void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();

            for (var i = 0; i < transform.childCount; i += 1)
            {
                var child = transform.GetChild(i);
                if (child.name == _rotationPivotDefaultName)
                    _rotationContent = child.GetComponent<RectTransform>();
            }
        }

        protected virtual void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rotationContent == null)
                Debug.LogException(new NullReferenceException($"{nameof(_rotationContent)} is null"), this);

            _contentGO = _coreContent.gameObject;
            _rotationContentGO = _rotationContent.gameObject;

            SetAnchorsAndPivot();
        }

        /// <summary>
        /// Sets the min anchor and max anchor of this GameObject's RectTransform to (0, 0) and sets the pivot to (0.5f, 0.5f).
        /// </summary>
        protected virtual void SetAnchorsAndPivot()
        {
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;
            _rectTransform.pivot = Vector2.one * 0.5f;
        }

        /// <summary>
        /// Sets this GameObject active.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Sets this GameObject inactive.
        /// </summary>
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the visual indicator with the data from a <see cref="TargetIndicator"/>.
        /// </summary>
        /// <param name="targetIndicator">The target indicator data to apply to the visual indicator.</param>
        public virtual void UpdateVisualIndicator(TargetIndicator targetIndicator)
        {
            UpdateVisualIndicator(
                targetIndicator.ScreenPose,
                targetIndicator.IsOutsideBoundary,
                targetIndicator.Distance,
                targetIndicator.LookAtDot);
        }

        /// <summary>
        /// Sets the pose and visibility of the core content and rotation content.
        /// </summary>
        /// <param name="screenPose">The screen pose to apply to the visual indicator.</param>
        /// <param name="isOutsideBoundary">The state of the screen pose if it is outside the boundary.</param>
        /// <param name="distance">The world space distance from the reference point to the target.</param>
        /// <param name="lookAtDot">The dot product of the reference forward vector and the direction to the target.</param>
        public virtual void UpdateVisualIndicator(Pose screenPose, bool isOutsideBoundary, float distance, float lookAtDot)
        {
            UpdateDistanceLabel(distance, lookAtDot);

            _rectTransform.anchoredPosition = screenPose.position / CanvasScale;
            _rotationContent.rotation = screenPose.rotation;

            switch (_coreContentVisibility)
            {
                case IndicatorVisibility.Never:
                    SetActive(_contentGO, false);
                    break;
                case IndicatorVisibility.Always:
                    SetActive(_contentGO, true);
                    break;
                case IndicatorVisibility.OutsideBoundary:
                    SetActive(_contentGO, isOutsideBoundary);
                    break;
                case IndicatorVisibility.InsideBoundary:
                    SetActive(_contentGO, !isOutsideBoundary);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (_rotationContentVisibility)
            {
                case IndicatorVisibility.Never:
                    SetActive(_rotationContentGO, false);
                    break;
                case IndicatorVisibility.Always:
                    SetActive(_rotationContentGO, true);
                    break;
                case IndicatorVisibility.OutsideBoundary:
                    SetActive(_rotationContentGO, isOutsideBoundary);
                    break;
                case IndicatorVisibility.InsideBoundary:
                    SetActive(_rotationContentGO, !isOutsideBoundary);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void UpdateDistanceLabel(float distance, float lookAtDot)
        {
            if (_distanceLabel == null)
                return;

            var shouldShowDistance =
                _distanceLabelVisibility == DistanceLabelVisibility.Always ||
                (_distanceLabelVisibility == DistanceLabelVisibility.LookAt && lookAtDot >= _lookAtDotThreshold);

            var targetAlpha = shouldShowDistance ? 1f : 0f;
            if (_distanceLabelCurrentAlpha < 0f)
            {
                _distanceLabelCurrentAlpha = targetAlpha;
                _distanceLabel.canvasRenderer.SetAlpha(_distanceLabelCurrentAlpha);
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_distanceLabelCurrentAlpha != targetAlpha)
            {
                var fadeSpeed = _distanceLabelFadeTime > 0.001f ? 1f / _distanceLabelFadeTime : 1000f;
                _distanceLabelCurrentAlpha = Mathf.MoveTowards(
                    _distanceLabelCurrentAlpha,
                    targetAlpha,
                    Time.deltaTime * fadeSpeed);
                _distanceLabel.canvasRenderer.SetAlpha(_distanceLabelCurrentAlpha);
            }

            SetActive(_distanceLabel.gameObject, _distanceLabelCurrentAlpha > 0f);

            if (_distanceLabelCurrentAlpha <= 0f)
                return;

            var currentDistanceInt = Mathf.RoundToInt(distance);
            if (currentDistanceInt == _lastDistanceInt)
                return;

            _lastDistanceInt = currentDistanceInt;
            _distanceLabel.SetText("{0} m", currentDistanceInt);
        }

        protected static void SetActive(GameObject go, bool state)
        {
            if (go.activeSelf != state)
                go.SetActive(state);
        }
    }
}
