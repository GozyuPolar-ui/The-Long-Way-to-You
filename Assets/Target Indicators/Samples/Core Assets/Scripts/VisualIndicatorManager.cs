using System;
using System.Collections.Generic;
using UnityEngine;

namespace TargetIndicators.Samples
{
    /// <summary>
    /// Manages target indicators created by a <see cref="TargetIndicatorManager"/> by instantiating visual indicators
    /// and updating their position in the UI. Supports <c>Padded</c>, <c>Absolute</c>, and <c>Unbounded</c> boundary types.
    /// </summary>
    public class VisualIndicatorManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField, Tooltip("The prefab used for every target in Auto mode, and by TryAddVisualIndicator when no " +
                                 "prefab is passed.")]
        protected VisualIndicator _defaultVisualIndicatorPrefab;

        [Header("Scene References")]
        [SerializeField, Tooltip("The manager this visual indicator manager interacts with and responds to.")]
        protected TargetIndicatorManager _targetIndicatorManager;

        [SerializeField, Tooltip("The Canvas the visual indicator belongs to. This is used for handling changes in canvas scale.")]
        protected Canvas _canvas;

        [SerializeField, Tooltip("The RectTransform that visual indicators get parented to. Stretch it over the whole " +
                                 "Canvas (anchors 0,0 to 1,1) so indicators line up with their targets.")]
        protected RectTransform _content;

        [Header("Settings")]
        [SerializeField, Tooltip("Auto: shows every target added to the Target Indicator Manager, using the default " +
                                 "prefab.\nManual: shows only targets added through this component's " +
                                 "TryAddVisualIndicator. Targets added directly to the Target Indicator Manager are " +
                                 "ignored, so several Manual layers can share one manager.")]
        protected AddIndicatorMode _addIndicatorMode;

        [SerializeField, Tooltip("When enabled, sets the target indicator manager's Boundary Scale to the Canvas scale " +
                                 "factor so its padding, width, and height are in the same units as the Canvas.")]
        protected bool _syncBoundaryScaleWithCanvas;

        /// <summary>
        /// Get and set the default indicator prefab.
        /// </summary>
        public VisualIndicator DefaultVisualIndicatorPrefab
        {
            get => _defaultVisualIndicatorPrefab;
            set => _defaultVisualIndicatorPrefab = value;
        }

        /// <summary>
        /// Get and set the <see cref="AddIndicatorMode"/>.
        /// </summary>
        public AddIndicatorMode AddIndicatorMode
        {
            get => _addIndicatorMode;
            set => _addIndicatorMode = value;
        }

        /// <summary>
        /// Gets or sets whether this component keeps <see cref="TargetIndicatorManager.BoundaryScale"/> in sync with
        /// the <see cref="Canvas.scaleFactor"/> of its canvas. When enabled, the target indicator manager's padding,
        /// width, and height are in canvas units, so the boundary stays proportional to the visual indicators at every
        /// resolution when the canvas uses a <c>CanvasScaler</c>.
        /// </summary>
        /// <remarks>
        /// Disabled by default. Disabling it leaves <see cref="TargetIndicatorManager.BoundaryScale"/> at its current
        /// value. If several visual indicator managers share one target indicator manager, enable this on only one of
        /// them, or make sure their canvases have the same scale factor.
        /// </remarks>
        public bool SyncBoundaryScaleWithCanvas
        {
            get => _syncBoundaryScaleWithCanvas;
            set
            {
                _syncBoundaryScaleWithCanvas = value;
                SyncBoundaryScale();
            }
        }

        /// <summary>
        /// The set of targets that is being managed by its target indicator ID.
        /// </summary>
        protected readonly Dictionary<TargetIndicatorId, VisualIndicator> _trackedUITargetIndicators = new();

        /// <summary>
        /// The state if a warning log has been logged since the last `OnTargetIndicatorsAdded` or `OnTargetIndicatorsUpdated`
        /// was called to prevent spamming every frame.
        /// </summary>
        protected bool _warningLogged;

        protected virtual void Reset()
        {
            _targetIndicatorManager = FindAnyObjectByType<TargetIndicatorManager>();
        }

        protected virtual void Awake()
        {
            DefaultVisualIndicatorPrefab = _defaultVisualIndicatorPrefab;

            if (_targetIndicatorManager == null)
                _targetIndicatorManager = FindAnyObjectByType<TargetIndicatorManager>();

            if (_targetIndicatorManager == null)
                Debug.LogException(new NullReferenceException($"{nameof(_targetIndicatorManager)} is null."), this);

            if (_content == null)
                Debug.LogException(new NullReferenceException($"{nameof(_content)} is null."), this);

            if (_canvas == null)
                Debug.LogException(new NullReferenceException($"{nameof(_canvas)} is null."), this);

        }

        protected virtual void OnEnable()
        {
            _targetIndicatorManager.TargetIndicatorsAdded += OnTargetIndicatorsAdded;
            _targetIndicatorManager.TargetIndicatorsUpdated += OnTargetIndicatorsUpdated;
            _targetIndicatorManager.TargetIndicatorsRemoved += OnTargetIndicatorsRemoved;

            foreach (var uiTargetIndicator in _trackedUITargetIndicators.Values)
            {
                uiTargetIndicator.gameObject.SetActive(true);
            }

            SyncBoundaryScale();
        }

        /// <summary>
        /// Sets <see cref="TargetIndicatorManager.BoundaryScale"/> to the canvas scale factor when
        /// <see cref="SyncBoundaryScaleWithCanvas"/> is enabled.
        /// </summary>
        protected virtual void SyncBoundaryScale()
        {
            if (!_syncBoundaryScaleWithCanvas || _canvas == null || _targetIndicatorManager == null)
                return;

            var scaleFactor = _canvas.scaleFactor;
            if (scaleFactor > 0f && !Mathf.Approximately(_targetIndicatorManager.BoundaryScale, scaleFactor))
                _targetIndicatorManager.BoundaryScale = scaleFactor;
        }

        protected virtual void OnDisable()
        {
            _targetIndicatorManager.TargetIndicatorsAdded -= OnTargetIndicatorsAdded;
            _targetIndicatorManager.TargetIndicatorsUpdated -= OnTargetIndicatorsUpdated;
            _targetIndicatorManager.TargetIndicatorsRemoved -= OnTargetIndicatorsRemoved;

            foreach (var uiTargetIndicator in _trackedUITargetIndicators.Values)
            {
                if (uiTargetIndicator == null)
                    continue;

                uiTargetIndicator.gameObject.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            foreach (var uiTargetIndicator in _trackedUITargetIndicators.Values)
            {
                if (uiTargetIndicator != null)
                    Destroy(uiTargetIndicator.gameObject);
            }

            _trackedUITargetIndicators.Clear();
        }

        /// <summary>
        /// Adds a target to be tracked to the <see cref="TargetIndicatorManager"/> and will instantiate the default
        /// visual indicator prefab. If <see cref="AddIndicatorMode"/> is set to <see cref="AddIndicatorMode.Auto"/>
        /// then the <see cref="DefaultVisualIndicatorPrefab"/> will be instantiated for the target.
        /// </summary>
        /// <param name="target">The target transform to track.</param>
        /// <exception cref="ArgumentNullException"> will be thrown if <paramref name="target"/> is null.</exception>
        [Obsolete("AddTargetIndicator is deprecated in version 1.3.0. Use TryAddVisualIndicator instead.", false)]
        public virtual void AddTargetIndicator(Transform target)
        {
            TryAddVisualIndicator(target, out _);
        }

        /// <summary>
        /// Attempts to add a target to be tracked to the <see cref="TargetIndicatorManager"/> and instantiate the default
        /// visual indicator prefab. Requires <see cref="AddIndicatorMode"/> to be set to
        /// <see cref="AddIndicatorMode.Manual"/>.
        /// </summary>
        /// <param name="target">The target transform to track.</param>
        /// <param name="id">The <see cref="TargetIndicatorId"/> if the target was successfully added; otherwise,
        /// the default value.</param>
        /// <returns><c>true</c> if the target was successfully added and the UI instantiated; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"> will be thrown if <paramref name="target"/> is null.</exception>
        public virtual bool TryAddVisualIndicator(Transform target, out TargetIndicatorId id)
        {
            id = default;

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (_addIndicatorMode == AddIndicatorMode.Auto)
            {
                Debug.LogWarning(
                    $"Cannot manually add target indicator for {target.name}. " +
                    $"{nameof(VisualIndicatorManager)} is set to {nameof(AddIndicatorMode.Auto)}. " +
                    $"Set {nameof(AddIndicatorMode)} to {nameof(AddIndicatorMode.Manual)} to use this API.",
                    this);
                return false;
            }

            if (!_targetIndicatorManager.TryAddTarget(target.transform, out var targetIndicator))
                return false;

            CreateUITargetIndicator(_defaultVisualIndicatorPrefab, targetIndicator);
            id = targetIndicator.Id;

            return true;
        }

        /// <summary>
        /// Adds a target to be tracked to the <see cref="TargetIndicatorManager"/> and will instantiate a custom
        /// visual indicator prefab. Requires <see cref="AddIndicatorMode"/> to be set to
        /// <see cref="AddIndicatorMode.Manual"/>.
        /// </summary>
        /// <param name="target">The target transform to track.</param>
        /// <param name="indicatorPrefab">The visual indicator prefab to instantiate for the target.</param>
        /// <exception cref="ArgumentNullException"> will be thrown if <paramref name="target"/> or
        /// <paramref name="indicatorPrefab"/> is null.</exception>
        [Obsolete("AddTargetIndicator is deprecated in version 1.3.0. Use TryAddVisualIndicator instead.", false)]
        public virtual void AddTargetIndicator(Transform target, VisualIndicator indicatorPrefab)
        {
            TryAddVisualIndicator(target, indicatorPrefab, out _);
        }

        /// <summary>
        /// Attempts to add a target to be tracked to the <see cref="TargetIndicatorManager"/> and instantiate a custom
        /// visual indicator prefab. Requires <see cref="AddIndicatorMode"/> to be set to
        /// <see cref="AddIndicatorMode.Manual"/>.
        /// </summary>
        /// <param name="target">The target transform to track.</param>
        /// <param name="indicatorPrefab">The visual indicator prefab to instantiate for the target.</param>
        /// <param name="id">The <see cref="TargetIndicatorId"/>  if the target was successfully added; otherwise,
        /// the default value.</param>
        /// <returns><c>true</c> if the target was successfully added and the UI instantiated; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"> will be thrown if <paramref name="target"/> or
        /// <paramref name="indicatorPrefab"/> is null.</exception>
        public virtual bool TryAddVisualIndicator(Transform target, VisualIndicator indicatorPrefab, out TargetIndicatorId id)
        {
            id = default;

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (indicatorPrefab == null)
                throw new ArgumentNullException(nameof(indicatorPrefab));

            if (_addIndicatorMode == AddIndicatorMode.Auto)
            {
                Debug.LogWarning(
                    $"Cannot manually add target indicator for {target.name}. " +
                    $"{nameof(VisualIndicatorManager)} is set to {nameof(AddIndicatorMode.Auto)}. " +
                    $"Set {nameof(AddIndicatorMode)} to {nameof(AddIndicatorMode.Manual)} to use this API.",
                    this);
                return false;
            }

            if (!_targetIndicatorManager.TryAddTarget(target.transform, out var targetIndicator))
                return false;

            CreateUITargetIndicator(indicatorPrefab, targetIndicator);
            id = targetIndicator.Id;

            return true;
        }

        /// <summary>
        /// Shows the visual indicator by its ID.
        /// </summary>
        /// <param name="id">The ID of the visual indicator to show.</param>
        public virtual void ShowVisualIndicator(TargetIndicatorId id)
        {
            if (!_trackedUITargetIndicators.TryGetValue(id, out var uiTargetIndicator))
                return;

            if (uiTargetIndicator != null)
                uiTargetIndicator.Show();
        }

        /// <summary>
        /// Hides the visual indicator by its ID.
        /// </summary>
        /// <param name="id">The ID of the visual indicator to hide.</param>
        public virtual void HideVisualIndicator(TargetIndicatorId id)
        {
            if (!_trackedUITargetIndicators.TryGetValue(id, out var uiTargetIndicator))
                return;

            if (uiTargetIndicator != null)
                uiTargetIndicator.Hide();
        }

        /// <summary>
        /// Removes a target from the target indicator manager by ID.
        /// </summary>
        /// <param name="id">The target indicator ID of the target to remove.</param>
        public virtual void RemoveTargetIndicator(TargetIndicatorId id)
        {
            if (_addIndicatorMode == AddIndicatorMode.Auto)
            {
                Debug.LogWarning(
                    $"Cannot manually remove a target indicator when the {nameof(AddIndicatorMode)} is set to " +
                    $"{nameof(AddIndicatorMode.Manual)}.",
                    this);

                return;
            }

            if (!_targetIndicatorManager.TryRemoveTarget(id))
                return;

            if (!_trackedUITargetIndicators.TryGetValue(id, out var uiTargetIndicator))
                return;

            if (uiTargetIndicator != null)
                Destroy(uiTargetIndicator.gameObject);

            _trackedUITargetIndicators.Remove(id);
        }

        /// <summary>
        /// Subscribes to the <see cref="TargetIndicatorManager.TargetIndicatorsAdded"/> event and instantiates
        /// the default visual indicator prefab for the target indicator.
        /// </summary>
        /// <param name="addedTargetIndicators">The span of <see cref="TargetIndicator"/> that were added.</param>
        protected virtual void OnTargetIndicatorsAdded(ReadOnlySpan<TargetIndicator> addedTargetIndicators)
        {
            SyncBoundaryScale();

            if (_addIndicatorMode == AddIndicatorMode.Manual)
                return;

            if (_targetIndicatorManager.BoundaryType == BoundaryType.CompassTape)
            {
                if (_warningLogged)
                    return;

                _warningLogged = true;
                Debug.LogWarning(
                    $"{nameof(VisualIndicatorManager)} cannot display {nameof(BoundaryType.CompassTape)} " +
                    $"target indicators. Use the {nameof(CompassTapeVisualIndicatorManager)} with the " +
                    $"{nameof(CompassTapeVisualIndicator)} or create your own system for displaying target indicator " +
                    $"pose updates when {nameof(_targetIndicatorManager.BoundaryShape)} is set to " +
                    $"{nameof(BoundaryType.CompassTape)}.)", this);

                return;
            }

            _warningLogged = false;

            foreach (var targetIndicator in addedTargetIndicators)
            {
                CreateUITargetIndicator(_defaultVisualIndicatorPrefab, targetIndicator);
            }
        }

        /// <summary>
        /// Subscribes to the <see cref="TargetIndicatorManager.TargetIndicatorsUpdated"/> event and updates
        /// the visual indicator associated with each target indicator.
        /// </summary>
        /// <param name="updatedTargetIndicators">The span of <see cref="TargetIndicator"/> that were updated.</param>
        protected virtual void OnTargetIndicatorsUpdated(ReadOnlySpan<TargetIndicator> updatedTargetIndicators)
        {
            SyncBoundaryScale();

            if (_targetIndicatorManager.BoundaryType == BoundaryType.CompassTape)
            {
                if (_warningLogged)
                    return;

                _warningLogged = true;
                Debug.LogWarning(
                    $"{nameof(VisualIndicatorManager)} cannot display {nameof(BoundaryType.CompassTape)} " +
                    $"target indicators. Use the {nameof(CompassTapeVisualIndicatorManager)} with the " +
                    $"{nameof(CompassTapeVisualIndicator)} or create your own system for displaying target indicator " +
                    $"pose updates when {nameof(_targetIndicatorManager.BoundaryShape)} is set to " +
                    $"{nameof(BoundaryType.CompassTape)}.)", this);

                return;
            }

            _warningLogged = false;
            var currentCanvasScale = _canvas.transform.localScale.x;
            foreach (var targetIndicator in updatedTargetIndicators)
            {
                if(!_trackedUITargetIndicators.TryGetValue(targetIndicator.Id, out var uiTargetIndicator))
                    continue;

                uiTargetIndicator.CanvasScale = currentCanvasScale;
                uiTargetIndicator.UpdateVisualIndicator(targetIndicator);
            }
        }

        /// <summary>
        /// Subscribes to the <see cref="TargetIndicatorManager.TargetIndicatorsRemoved"/> event and destroys
        /// the associated <see cref="VisualIndicator"/> for each target indicator ID and removes it from the
        /// <see cref="TargetIndicatorManager"/>.
        /// </summary>
        /// <param name="removedTargetIndicators">The span of <see cref="TargetIndicatorId"/> that were removed.</param>
        protected virtual void OnTargetIndicatorsRemoved(ReadOnlySpan<TargetIndicatorId> removedTargetIndicators)
        {
            foreach (var id in removedTargetIndicators)
            {
                if (!_trackedUITargetIndicators.TryGetValue(id, out var uiTargetIndicator))
                    continue;

                if (uiTargetIndicator != null)
                    Destroy(uiTargetIndicator.gameObject);

                _trackedUITargetIndicators.Remove(id);
            }
        }

        /// <summary>
        /// Instantiates a <see cref="VisualIndicator"/> and sets its pose.
        /// </summary>
        /// <param name="indicatorPrefab">The prefab to instantiate.</param>
        /// <param name="targetIndicator">The target indicator data to apply to the <c>VisualIndicator</c>.</param>
        protected virtual void CreateUITargetIndicator(VisualIndicator indicatorPrefab, TargetIndicator targetIndicator)
        {
            if (indicatorPrefab == null)
                throw new ArgumentNullException(nameof(indicatorPrefab));

            var uiIndicator = Instantiate(indicatorPrefab, _content);
            uiIndicator.TargetIndicatorId = targetIndicator.Id;
            uiIndicator.CanvasScale = _canvas.transform.localScale.x;
            uiIndicator.UpdateVisualIndicator(targetIndicator);

            _trackedUITargetIndicators.Add(targetIndicator.Id, uiIndicator);
        }
    }
}
