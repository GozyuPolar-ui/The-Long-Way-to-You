using System;
using UnityEngine;

namespace TargetIndicators.Samples
{
    /// <summary>
    /// Manages target indicators created by a <see cref="TargetIndicatorManager"/> for compass tape indicators by
    /// instantiating visual indicators and updating their position in the UI. Supports only <c>CompassTape</c> boundary types.
    /// </summary>
    public class CompassTapeVisualIndicatorManager : VisualIndicatorManager
    {
        [SerializeField]
        [Tooltip("The size of the full 360-degree tape relative to the visible tape. The visible tape covers 360 " +
                 "degrees divided by this value, so 4 shows 90 degrees and 2 shows 180 degrees. Must be above 0: " +
                 "at 0, every marker sits at the center of the tape.")]
        float _fullTapeToVisibleTapeRatio;

        /// <summary>
        /// The length ratio between the full tape and visible tape. If the full tape is twice as long as the visible tape
        /// this value should be 2.
        /// </summary>
        public float FullTapeToVisibleTapeRatio
        {
            get => _fullTapeToVisibleTapeRatio;
            set => _fullTapeToVisibleTapeRatio = value;
        }

        /// <inheritdoc/>
        protected override void OnTargetIndicatorsAdded(ReadOnlySpan<TargetIndicator> addedTargetIndicators)
        {
            if (_addIndicatorMode == AddIndicatorMode.Manual)
                return;

            if (_targetIndicatorManager.BoundaryType != BoundaryType.CompassTape)
            {
                if (_warningLogged)
                    return;

                _warningLogged = true;
                Debug.LogWarning(
                    $"{nameof(CompassTapeVisualIndicatorManager)} can only display {nameof(BoundaryType.CompassTape)} " +
                    $"target indicators. Use the {nameof(VisualIndicatorManager)} with a {nameof(VisualIndicator)} or create " +
                    $"your own system for displaying target indicator pose updates when " +
                    $"{nameof(_targetIndicatorManager.BoundaryShape)} is not set to {nameof(BoundaryType.CompassTape)}.)",
                    this);

                return;
            }

            _warningLogged = false;

            if (DefaultVisualIndicatorPrefab is not CompassTapeVisualIndicator)
            {
                Debug.LogError($"Default prefab must have a {nameof(CompassTapeVisualIndicator)} component attached.", this);
                return;
            }

            foreach (var targetIndicator in addedTargetIndicators)
            {
                CreateUITargetIndicator(DefaultVisualIndicatorPrefab, targetIndicator);
            }
        }

        /// <inheritdoc/>
        protected override void OnTargetIndicatorsUpdated(ReadOnlySpan<TargetIndicator> updatedTargetIndicators)
        {
            if (_targetIndicatorManager.BoundaryType != BoundaryType.CompassTape)
            {
                if (_warningLogged)
                    return;

                _warningLogged = true;
                Debug.LogWarning(
                    $"{nameof(CompassTapeVisualIndicatorManager)} can only display {nameof(BoundaryType.CompassTape)} " +
                    $"target indicators. Use the {nameof(VisualIndicatorManager)} with a {nameof(VisualIndicator)} or create " +
                    $"your own system for displaying target indicator pose updates when " +
                    $"{nameof(_targetIndicatorManager.BoundaryShape)} is not set to {nameof(BoundaryType.CompassTape)}.)",
                    this);

                return;
            }

            _warningLogged = false;

            var visibleTapeLength = _content.rect.width;
            foreach (var targetIndicator in updatedTargetIndicators)
            {
                if (!_trackedUITargetIndicators.TryGetValue(targetIndicator.Id, out var uiTargetIndicator))
                    continue;

                if (uiTargetIndicator is not CompassTapeVisualIndicator uiCompassTapeTargetIndicator)
                    continue;

                uiCompassTapeTargetIndicator.VisibleTapeLength = visibleTapeLength;
                uiCompassTapeTargetIndicator.FullTapeToVisibleTapeRatio = _fullTapeToVisibleTapeRatio;
                uiCompassTapeTargetIndicator.UpdateVisualIndicator(targetIndicator);
            }
        }

        /// <inheritdoc/>
        [Obsolete("AddTargetIndicator is deprecated in version 1.3.0. Please use TryAddVisualIndicator instead.", false)]
        public override void AddTargetIndicator(Transform target)
        {
            TryAddVisualIndicator(target, out _);
        }

        /// <inheritdoc/>
        public override bool TryAddVisualIndicator(Transform target, out TargetIndicatorId id)
        {
            id = default;
            if (_addIndicatorMode == AddIndicatorMode.Auto)
            {
                Debug.LogWarning(
                    $"Cannot manually add target indicator for {target.name}. " +
                    $"{nameof(VisualIndicatorManager)} is set to {nameof(AddIndicatorMode.Auto)}. " +
                    $"Set {nameof(AddIndicatorMode)} to {nameof(AddIndicatorMode.Manual)} to use this API.",
                    this);
                return false;
            }

            // Validate the default prefab BEFORE allowing the base class to register the backend target
            if (DefaultVisualIndicatorPrefab is not CompassTapeVisualIndicator)
            {
                Debug.LogError(
                    $"{nameof(CompassTapeVisualIndicatorManager)} requires the {nameof(DefaultVisualIndicatorPrefab)} to be a " +
                    $"{nameof(CompassTapeVisualIndicator)} when using the {nameof(TryAddVisualIndicator)} API.",
                    this);

                return false;
            }

            return base.TryAddVisualIndicator(target, out id);
        }

        /// <inheritdoc/>
        [Obsolete("AddTargetIndicator is deprecated in version 1.3.0. Please use TryAddVisualIndicator instead.", false)]
        public override void AddTargetIndicator(Transform target, VisualIndicator indicatorPrefab)
        {
            TryAddVisualIndicator(target, indicatorPrefab, out _);
        }

        /// <inheritdoc/>
        public override bool TryAddVisualIndicator(Transform target, VisualIndicator indicatorPrefab, out TargetIndicatorId id)
        {
            id = default;
            if (_addIndicatorMode == AddIndicatorMode.Auto)
            {
                Debug.LogWarning(
                    $"Cannot manually add target indicator for {target.name}. " +
                    $"{nameof(VisualIndicatorManager)} is set to {nameof(AddIndicatorMode.Auto)}. " +
                    $"Set {nameof(AddIndicatorMode)} to {nameof(AddIndicatorMode.Manual)} to use this API.",
                    this);
                return false;
            }

            if (indicatorPrefab is not CompassTapeVisualIndicator)
            {
                Debug.LogError(
                    $"{nameof(CompassTapeVisualIndicatorManager)} requires the {nameof(indicatorPrefab)} to be a " +
                    $"{nameof(CompassTapeVisualIndicator)} when using the {nameof(TryAddVisualIndicator)} API.",
                    this);

                return false;
            }

            return base.TryAddVisualIndicator(target, indicatorPrefab, out id);
        }

        /// <inheritdoc/>
        protected override void CreateUITargetIndicator(VisualIndicator indicatorPrefab, TargetIndicator targetIndicator)
        {
            if (indicatorPrefab == null)
                throw new ArgumentNullException(nameof(indicatorPrefab));

            if (indicatorPrefab is not CompassTapeVisualIndicator compassPrefab)
            {
                Debug.LogError($"Prefab must be a {nameof(CompassTapeVisualIndicator)}.", this);
                return;
            }

            var uiCompassTapeTargetIndicator = Instantiate(compassPrefab, _content);
            uiCompassTapeTargetIndicator.TargetIndicatorId = targetIndicator.Id;
            uiCompassTapeTargetIndicator.VisibleTapeLength = _content.rect.width;
            uiCompassTapeTargetIndicator.FullTapeToVisibleTapeRatio = _fullTapeToVisibleTapeRatio;
            uiCompassTapeTargetIndicator.CanvasScale = _canvas.transform.localScale.x;
            uiCompassTapeTargetIndicator.UpdateVisualIndicator(targetIndicator);

            _trackedUITargetIndicators.Add(targetIndicator.Id, uiCompassTapeTargetIndicator);
        }
    }
}
