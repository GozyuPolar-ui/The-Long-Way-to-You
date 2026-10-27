using System;
using UnityEngine;

namespace TargetIndicators.Samples
{
    /// <summary>
    /// Control the instance of a compass tape visual indicator.
    /// </summary>
    public class CompassTapeVisualIndicator : VisualIndicator
    {
        /// <summary>
        /// The length of the visible tape.
        /// </summary>
        public float VisibleTapeLength { get; set; }

        /// <summary>
        /// The length ratio between the full tape and visible tape. If the full tape is twice as long as the visible tape
        /// this value should be 2.
        /// </summary>
        public float FullTapeToVisibleTapeRatio { get; set; }

        /// <inheritdoc/>
        public override void UpdateVisualIndicator(TargetIndicator targetIndicator)
        {
            UpdateVisualIndicator(
                targetIndicator.ScreenPose,
                targetIndicator.IsOutsideBoundary,
                targetIndicator.Distance,
                targetIndicator.LookAtDot);
        }

        /// <inheritdoc/>
        public override void UpdateVisualIndicator(Pose screenPose, bool isOutsideBoundary, float distance, float lookAtDot)
        {
            UpdateDistanceLabel(distance, lookAtDot);

            screenPose.position.x %= 1f;

            var positionOnFullTape = screenPose.position.x * (FullTapeToVisibleTapeRatio * VisibleTapeLength);
            var middleOfFullTape = (FullTapeToVisibleTapeRatio * VisibleTapeLength) * 0.5f;
            var middleToPositionOnFullTape = positionOnFullTape - middleOfFullTape;
            var middleOfVisibleTape = VisibleTapeLength * 0.5f;
            var pointOnVisibleTape = middleOfVisibleTape + middleToPositionOnFullTape;

            screenPose.position.x = pointOnVisibleTape;
            screenPose.position.y = 0;

            isOutsideBoundary = pointOnVisibleTape < 0 || pointOnVisibleTape > VisibleTapeLength;

            _rectTransform.anchoredPosition = screenPose.position;

            switch (_coreContentVisibility)
            {
                case IndicatorVisibility.Never:
                    SetActive(_contentGO, false);
                    break;
                case IndicatorVisibility.Always:
                    screenPose.position.x = Mathf.Clamp(pointOnVisibleTape, 0, VisibleTapeLength);
                    _rectTransform.anchoredPosition = screenPose.position;
                    SetActive(_contentGO, true);
                    break;
                case IndicatorVisibility.OutsideBoundary:
                    screenPose.position.x = Mathf.Clamp(pointOnVisibleTape, 0, VisibleTapeLength);
                    _rectTransform.anchoredPosition = screenPose.position;
                    SetActive(_contentGO, isOutsideBoundary);
                    break;
                case IndicatorVisibility.InsideBoundary:
                    SetActive(_contentGO, !isOutsideBoundary);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SetActive(_rotationContentGO, false);
        }

        /// <inheritdoc/>
        protected override void SetAnchorsAndPivot()
        {
            _rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _rectTransform.pivot = Vector2.one * 0.5f;
        }
    }
}
