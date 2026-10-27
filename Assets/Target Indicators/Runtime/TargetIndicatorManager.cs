using System;
using System.Collections.Generic;
using UnityEngine;

namespace TargetIndicators
{
    /// <summary>
    /// Tracks targets and transforms their world space positions into screen space coordinates with options to clamp to
    /// the screen edges, absolute size, or a compass tape. Useful for displaying directions to targets and other
    /// positions in the environment.
    /// </summary>
    [HelpURL("https://jakemanfre.github.io/target-indicators.github.io/manual/user_guide/target-indicator-manager.html")]
    public class TargetIndicatorManager : MonoBehaviour
    {
        enum State
        {
            Added,
            Updated,
        }

        struct TargetData
        {
            public TargetIndicatorId Id;
            public Transform Target;
            public State State;
        }

        const int k_maxTargets = 100;
        const float k_minBoundaryScale = 0.0001f;

        [Header("Scene References")]
        [SerializeField, Tooltip("The camera used to calculate screen point coordinates. Defaults to the camera tagged " +
                                 "\"MainCamera\" and falls back to any camera in the scene.")]
        Camera _camera;

        [SerializeField, Tooltip("An optional override for the compass forward vector. Only used when Boundary Type is " +
                                 "Compass Tape. If null, falls back to the Camera. Useful for tracking targets relative to " +
                                 "an independent object (like a vehicle hull) rather than a free-look camera.")]
        Transform _compassForwardReferenceOverride;

        [Header("Settings")]
        [SerializeField, Tooltip("The type of boundary that target indicators will clamp to.")]
        BoundaryType _boundaryType;

        [Header("Configuration")]
        [SerializeField, Tooltip("The shape of the boundary that target indicators will clamp to. Only Padded and Absolute " +
                                 "boundary types use this setting.")]
        BoundaryShape _boundaryShape;

        [Tooltip("The distance from the top edge of the screen to clamp to for padded boundaries. Multiplied by " +
                 "Boundary Scale to get screen pixels.")]
        [SerializeField, Min(0)]
        float _topPadding;

        [Tooltip("The distance from the bottom edge of the screen to clamp to for padded boundaries. Multiplied by " +
                 "Boundary Scale to get screen pixels.")]
        [SerializeField, Min(0)]
        float _bottomPadding;

        [Tooltip("The distance from the left edge of the screen to clamp to for padded boundaries. Multiplied by " +
                 "Boundary Scale to get screen pixels.")]
        [SerializeField, Min(0)]
        float _leftPadding;

        [Tooltip("The distance from the right edge of the screen to clamp to for padded boundaries. Multiplied by " +
                 "Boundary Scale to get screen pixels.")]
        [SerializeField, Min(0)]
        float _rightPadding;

        [Tooltip("Absolute width of the boundary. Multiplied by Boundary Scale to get screen pixels. This value is " +
                 "ignored if boundary type is not set to Absolute.")]
        [SerializeField, Min(0)]
        float _width = 300;

        [Tooltip("Absolute height of the boundary. Multiplied by Boundary Scale to get screen pixels. This value is " +
                 "ignored if boundary type is not set to Absolute.")]
        [SerializeField, Min(0)]
        float _height = 300f;

        [Tooltip("The scale applied to the padding, width, and height values to convert them to screen pixels. Set " +
                 "this to your UI's scale factor (for example Canvas.scaleFactor) so the boundary is defined in the " +
                 "same units as your UI. A value of 1 means the values are in screen pixels.")]
        [SerializeField, Min(k_minBoundaryScale)]
        float _boundaryScale = 1f;

        [Tooltip("Calculate the Dot Product between the reference forward vector and the target. Useful for " +
                 "'Look At' UI mechanics. Disable if not needed for minor performance gain.")]
        [SerializeField]
        bool _calculateLookAtDot = true;

        /// <summary>
        /// Delegate that passes a <see cref="ReadOnlySpan{T}"/> of target indicators that were added or updated.
        /// </summary>
        public delegate void TargetIndicatorsUpdatedDelegate(ReadOnlySpan<TargetIndicator> span);

        /// <summary>
        /// Delegate that passes a <see cref="ReadOnlySpan{T}"/> of target indicators that were removed.
        /// </summary>
        public delegate void TargetIndicatorsRemovedDelegate(ReadOnlySpan<TargetIndicatorId> span);

        /// <summary>
        /// Invoked when target indicators have been added.
        /// </summary>
        public event TargetIndicatorsUpdatedDelegate TargetIndicatorsAdded;

        /// <summary>
        /// Invoked when target indicators have been updated.
        /// </summary>
        public event TargetIndicatorsUpdatedDelegate TargetIndicatorsUpdated;

        /// <summary>
        /// Invoked when target indicators have been removed.
        /// </summary>
        public event TargetIndicatorsRemovedDelegate TargetIndicatorsRemoved;

        /// <summary>
        /// The scene camera that is used to calculate the screen pose of tracked targets.
        /// If this is null or destroyed, the manager will safely output default pose data for all targets, unless
        /// <see cref="BoundaryType"/> is <see cref="BoundaryType.CompassTape"/> and
        /// <see cref="CompassForwardReferenceOverride"/> is set, in which case the override is used as the reference.
        /// </summary>
        public Camera Camera
        {
            get => _camera;
            set
            {
                if (value == null && !UsesCompassForwardOverride)
                    Debug.LogWarning(
                        "[TargetIndicatorManager] Camera explicitly set to null. Targets will return default poses" +
                        " until a new camera is assigned.", this);

                _camera = value;
            }
        }

        /// <summary>
        /// An optional transform used to override the forward reference vector when the <see cref="BoundaryType"/>
        /// is set to <see cref="BoundaryType.CompassTape"/>. Useful for tracking targets relative to an independent
        /// object (like a vehicle hull) rather than a free-look camera. When set, compass tape poses do not require a
        /// <see cref="Camera"/>.
        /// </summary>
        public Transform CompassForwardReferenceOverride
        {
            get => _compassForwardReferenceOverride;
            set => _compassForwardReferenceOverride = value;
        }

        /// <summary>
        /// The definition of the currently configured rectangle in screen pixels, with <see cref="BoundaryScale"/>
        /// applied, if <see cref="BoundaryShape"/> is set to <see cref="BoundaryShape.Rectangle"/>.
        /// </summary>
        public Rect Rectangle
        {
            get
            {
                var left = _leftPadding * _boundaryScale;
                var right = _rightPadding * _boundaryScale;
                var top = _topPadding * _boundaryScale;
                var bottom = _bottomPadding * _boundaryScale;
                var halfWidth = _width * _boundaryScale * 0.5f;
                var halfHeight = _height * _boundaryScale * 0.5f;

                return _boundaryType switch
                {
                    BoundaryType.Padded => new Rect
                    {
                        min = new Vector2(left, bottom),
                        max = new Vector2(Screen.width - right, Screen.height - top),
                    },
                    BoundaryType.Absolute => new Rect
                    {
                        min = new Vector2(Screen.width * 0.5f - halfWidth, Screen.height * 0.5f - halfHeight),
                        max = new Vector2(Screen.width * 0.5f + halfWidth, Screen.height * 0.5f + halfHeight),
                    },
                    _ => default
                };
            }
        }

        /// <summary>
        /// The definition of the currently configured ellipse in screen pixels, with <see cref="BoundaryScale"/>
        /// applied, if <see cref="BoundaryShape"/> is set to <see cref="BoundaryShape.Ellipse"/>.
        /// </summary>
        public Ellipse Ellipse
        {
            get
            {
                var left = _leftPadding * _boundaryScale;
                var right = _rightPadding * _boundaryScale;
                var top = _topPadding * _boundaryScale;
                var bottom = _bottomPadding * _boundaryScale;

                return _boundaryType switch
                {
                    BoundaryType.Padded => new Ellipse(
                        EllipseScreenPose.GetPaddedEllipseCenter(left, right, top, bottom),
                        (Screen.width - left - right) * 0.5f,
                        (Screen.height - top - bottom) * 0.5f),
                    BoundaryType.Absolute => new Ellipse(
                        EllipseScreenPose.GetAbsoluteEllipseCenter(),
                        _width * _boundaryScale * 0.5f,
                        _height * _boundaryScale * 0.5f),
                    _ => default
                };
            }
        }

        /// <summary>
        /// The type of boundary to clamp target indicators to.
        /// </summary>
        public BoundaryType BoundaryType
        {
            get => _boundaryType;
            set
            {
                var isValid = Enum.IsDefined(typeof(BoundaryType), value);
                if (!isValid)
                {
                    Debug.LogError($"Boundary type with integer value {(int)value} is not valid.");
                    return;
                }

                _boundaryType = value;
            }
        }

        /// <summary>
        /// The shape of the boundary to clamp target indicators to.
        /// </summary>
        public BoundaryShape BoundaryShape
        {
            get => _boundaryShape;
            set
            {
                var isValid = Enum.IsDefined(typeof(BoundaryShape), value);
                if (!isValid)
                {
                    Debug.LogError($"Boundary shape with integer value {(int)value} is not valid.");
                    return;
                }

                _boundaryShape = value;
            }
        }

        /// <summary>
        /// The distance from the left edge of the screen for <c>Padded</c> boundary type. Multiplied by
        /// <see cref="BoundaryScale"/> to get screen pixels.
        /// </summary>
        public float LeftPadding
        {
            get => _leftPadding;
            set => _leftPadding = value;
        }

        /// <summary>
        /// The distance from the right edge of the screen for <c>Padded</c> boundary type. Multiplied by
        /// <see cref="BoundaryScale"/> to get screen pixels.
        /// </summary>
        public float RightPadding
        {
            get => _rightPadding;
            set => _rightPadding = value;
        }

        /// <summary>
        /// The distance from the top edge of the screen for <c>Padded</c> boundary type. Multiplied by
        /// <see cref="BoundaryScale"/> to get screen pixels.
        /// </summary>
        public float TopPadding
        {
            get => _topPadding;
            set => _topPadding = value;
        }

        /// <summary>
        /// The distance from the bottom edge of the screen for <c>Padded</c> boundary type. Multiplied by
        /// <see cref="BoundaryScale"/> to get screen pixels.
        /// </summary>
        public float BottomPadding
        {
            get => _bottomPadding;
            set => _bottomPadding = value;
        }

        /// <summary>
        /// The width of the boundary centered at the screen center for <c>Absolute</c> boundary type. Multiplied by
        /// <see cref="BoundaryScale"/> to get screen pixels.
        /// </summary>
        public float Width
        {
            get => _width;
            set => _width = value;
        }

        /// <summary>
        /// The height of the boundary centered at the screen center for <c>Absolute</c> boundary type. Multiplied by
        /// <see cref="BoundaryScale"/> to get screen pixels.
        /// </summary>
        public float Height
        {
            get => _height;
            set => _height = value;
        }

        /// <summary>
        /// The scale applied to <see cref="LeftPadding"/>, <see cref="RightPadding"/>, <see cref="TopPadding"/>,
        /// <see cref="BottomPadding"/>, <see cref="Width"/>, and <see cref="Height"/> to convert them to screen pixels.
        /// Defaults to <c>1</c>, meaning those values are in screen pixels.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set this to the scale factor of the UI that displays your visual indicators so the boundary is defined in
        /// the same units as your UI and stays proportional to it at every resolution. For example, with a uGUI
        /// <c>CanvasScaler</c> set to <c>Scale With Screen Size</c>, set this to <c>Canvas.scaleFactor</c>.
        /// </para>
        /// <para>
        /// The manager does not read any UI system itself. The sample <c>VisualIndicatorManager</c> can keep this value
        /// in sync with its canvas. If you build your own visual indicator system, you are responsible for setting this
        /// value, and updating it whenever your UI scale changes (for example, when the screen resolution changes).
        /// </para>
        /// <para>
        /// Values less than or equal to zero are rejected and log an error. Only <c>Padded</c> and <c>Absolute</c>
        /// boundary types are affected.
        /// </para>
        /// </remarks>
        public float BoundaryScale
        {
            get => _boundaryScale;
            set
            {
                if (value < k_minBoundaryScale)
                {
                    Debug.LogError($"Boundary scale must be greater than zero. Value {value} was ignored.", this);
                    return;
                }

                _boundaryScale = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to calculate the dot product between the reference forward vector and the direction to
        /// the target. Enable this to use <see cref="TargetIndicator.LookAtDot"/> for advanced UI mechanics.
        /// Disable for a minor performance gain.
        /// </summary>
        public bool CalculateLookAtDot
        {
            get => _calculateLookAtDot;
            set => _calculateLookAtDot = value;
        }

        /// <summary>
        /// The max number of targets that can be tracked.
        /// </summary>
        public int MaxTargets => k_maxTargets;

        /// <summary>
        /// The current number of targets being tracked.
        /// </summary>
        public int TrackedTargetsCount => _targetDataIndexById.Count;

        readonly TargetData[] _allTargetData = new TargetData[k_maxTargets];
#if UNITY_6000_3_OR_NEWER
        readonly Dictionary<EntityId, TargetIndicatorId> _idByTargetEntityId = new(k_maxTargets);
#else
        readonly Dictionary<int, TargetIndicatorId> _idByTargetEntityId = new(k_maxTargets);
#endif
        readonly Dictionary<TargetIndicatorId, int> _targetDataIndexById = new(k_maxTargets);

        readonly TargetIndicator[] _addedTargetIndicators = new TargetIndicator[k_maxTargets];
        readonly TargetIndicator[] _updatedTargetIndicators = new TargetIndicator[k_maxTargets];
        readonly TargetIndicatorId[] _removedTargetIndicators = new TargetIndicatorId[k_maxTargets];

        int _addedSinceLastUpdate;
        int _updatedSinceLastUpdate;
        int _removedSinceLastUpdate;

        ScreenData _screenData;
        RectangleScreenPose _rectangleScreenPose;
        EllipseScreenPose _ellipseScreenPose;
        CompassTapeScreenPose _compassTapeScreenPose;
        bool _isInitialized;

        void Reset()
        {
            TryFindCamera();
        }

        void Awake()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            TryFindCamera();
            if (_camera == null && !UsesCompassForwardOverride)
            {
                Debug.LogWarning(
                    "[TargetIndicatorManager] Camera is missing. Returning default target data until a camera is " +
                    "assigned via the Camera property.", this);
            }

            _screenData = new(this);
            _rectangleScreenPose = new(_screenData);
            _ellipseScreenPose = new(_screenData);
            _compassTapeScreenPose = new();

            _isInitialized = true;
        }

        void TryFindCamera()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                _camera = FindAnyObjectByType<Camera>();
        }

        /// <summary>
        /// Manually processes world-to-screen transformations for all tracked targets and dispatches
        /// the corresponding added, updated, and removed events.
        /// </summary>
        /// <remarks>
        /// Use this to synchronize indicator data and trigger events outside the standard
        /// <c>Update</c> loop (for example, if this component is disabled, or you require mid-frame updates).
        /// </remarks>
        public void GetChanges()
        {
            Update();
        }

        void Update()
        {
            var hasValidReference = TryUpdateReference(out var referencePosition, out var referenceForward);

            ProcessTargets(hasValidReference, referencePosition, referenceForward);
            DispatchChangeEvents();
        }

        bool UsesCompassForwardOverride =>
            _boundaryType == BoundaryType.CompassTape && _compassForwardReferenceOverride != null;

        // The compass override is a complete reference on its own; every other case requires the camera.
        bool TryUpdateReference(out Vector3 referencePosition, out Vector3 referenceForward)
        {
            if (UsesCompassForwardOverride)
            {
                referencePosition = _compassForwardReferenceOverride.position;
                referenceForward = _compassForwardReferenceOverride.forward;
            }
            else if (_camera != null)
            {
                var cameraTransform = _camera.transform;
                referencePosition = cameraTransform.position;
                referenceForward = cameraTransform.forward;
            }
            else
            {
                referencePosition = Vector3.zero;
                referenceForward = Vector3.forward;
                return false;
            }

            if (_boundaryType == BoundaryType.CompassTape)
                _compassTapeScreenPose.UpdateReferenceState(referencePosition, referenceForward);

            return true;
        }

        void ProcessTargets(bool hasValidReference, Vector3 referencePosition, Vector3 referenceForward)
        {
            for (var i = _targetDataIndexById.Count - 1; i >= 0; i--)
            {
                var targetData = _allTargetData[i];
                if (targetData.Target == null)
                {
                    RemoveTargetAtIndex(i);
                    continue;
                }

                var targetPosition = targetData.Target.position;
                var distance = 0f;
                var lookAtDot = 0f;

                if (hasValidReference)
                {
                    var vectorToTarget = targetPosition - referencePosition;
                    var sqrMagnitude = vectorToTarget.sqrMagnitude;

                    if (sqrMagnitude > 0.000001f)
                    {
                        distance = Mathf.Sqrt(sqrMagnitude);

                        if (_calculateLookAtDot)
                        {
                            var normalizedDirection = vectorToTarget / distance;
                            lookAtDot = Vector3.Dot(referenceForward, normalizedDirection);
                        }
                    }
                }

                if (targetData.State == State.Added)
                {
                    _addedTargetIndicators[_addedSinceLastUpdate++] = CreateTargetIndicator(
                        targetData.Id,
                        targetData.Target,
                        distance,
                        lookAtDot,
                        hasValidReference);

                    _allTargetData[i].State = State.Updated;
                }
                else
                {
                    _updatedTargetIndicators[_updatedSinceLastUpdate++] = CreateTargetIndicator(
                        targetData.Id,
                        targetData.Target,
                        distance,
                        lookAtDot,
                        hasValidReference);
                }
            }
        }

        void SwapBackAndRemove(int index)
        {
            var dataToRemove = _allTargetData[index];
            var lastIndex = _targetDataIndexById.Count - 1;

            if (index != lastIndex)
            {
                _allTargetData[index] = _allTargetData[lastIndex];
                _targetDataIndexById[_allTargetData[index].Id] = index;
            }

            _targetDataIndexById.Remove(dataToRemove.Id);
            if (dataToRemove.Target != null)
            {
#if UNITY_6000_3_OR_NEWER
                _idByTargetEntityId.Remove(dataToRemove.Target.GetEntityId());
#else
                _idByTargetEntityId.Remove(dataToRemove.Target.GetInstanceID());
#endif
            }
        }

        void DispatchChangeEvents()
        {
            if (_addedSinceLastUpdate > 0)
            {
                var added = new ReadOnlySpan<TargetIndicator>(_addedTargetIndicators, 0, _addedSinceLastUpdate);
                _addedSinceLastUpdate = 0;
                TargetIndicatorsAdded?.Invoke(added);
            }

            if (_updatedSinceLastUpdate > 0)
            {
                var updated = new ReadOnlySpan<TargetIndicator>(_updatedTargetIndicators, 0, _updatedSinceLastUpdate);
                _updatedSinceLastUpdate = 0;
                TargetIndicatorsUpdated?.Invoke(updated);
            }

            if (_removedSinceLastUpdate > 0)
            {
                var removed = new ReadOnlySpan<TargetIndicatorId>(_removedTargetIndicators, 0, _removedSinceLastUpdate);
                _removedSinceLastUpdate = 0;
                TargetIndicatorsRemoved?.Invoke(removed);
            }
        }

        /// <summary>
        /// Attempts to add a new target to be tracked. This can only fail if the <see cref="TrackedTargetsCount"/>
        /// equals <see cref="MaxTargets"/>. If there is no valid reference (see <see cref="Camera"/>), the returned
        /// <paramref name="targetIndicator"/> will contain default pose data.
        /// </summary>
        /// <param name="target">The transform of the target to track.</param>
        /// <param name="targetIndicator">The created <see cref="TargetIndicator"/> for the current frame of the
        /// target's position.</param>
        /// <returns><c>true</c> if the target was added, otherwise <c>false</c>.</returns>
        public bool TryAddTarget(Transform target, out TargetIndicator targetIndicator)
        {
            EnsureInitialized();
            targetIndicator = TargetIndicator.Default;

            if (target == null)
                return false;

            if (_targetDataIndexById.Count >= k_maxTargets)
                return false;

#if UNITY_6000_3_OR_NEWER
            var entityId = target.GetEntityId();
#else
            var entityId = target.GetInstanceID();
#endif
            if (_idByTargetEntityId.TryGetValue(entityId, out var existingId))
                return TryGetTargetIndicator(existingId, out targetIndicator);

            var targetData = new TargetData
            {
                Id = new TargetIndicatorId(Guid.NewGuid()),
                Target = target,
                State = State.Added,
            };

            _allTargetData[TrackedTargetsCount] = targetData;
            _targetDataIndexById.Add(targetData.Id, TrackedTargetsCount);
            _idByTargetEntityId.Add(entityId, targetData.Id);

            var hasValidReference = TryUpdateReference(out var referencePosition, out var referenceForward);

            var distance = 0f;
            var lookAtDot = 0f;

            if (hasValidReference)
            {
                var vectorToTarget = target.position - referencePosition;
                var sqrMagnitude = vectorToTarget.sqrMagnitude;

                if (sqrMagnitude > 0.000001f)
                {
                    distance = Mathf.Sqrt(sqrMagnitude);

                    if (_calculateLookAtDot)
                    {
                        var normalizedDirection = vectorToTarget / distance;
                        lookAtDot = Vector3.Dot(referenceForward, normalizedDirection);
                    }
                }
            }

            targetIndicator = CreateTargetIndicator(
                targetData.Id,
                targetData.Target,
                distance,
                lookAtDot,
                hasValidReference);

            return true;
        }

        /// <summary>
        /// Attempts to get the <see cref="TargetIndicator"/> for the corresponding <c>TargetIndicatorId</c>.
        /// If there is no valid reference (see <see cref="Camera"/>), the returned <paramref name="targetIndicator"/>
        /// will contain default pose data.
        /// </summary>
        /// <param name="targetIndicatorId">The ID of the target indicator.</param>
        /// <param name="targetIndicator">The <see cref="TargetIndicator"/> associated with
        /// <paramref name="targetIndicatorId"/>.</param>
        /// <returns><c>true</c> if the <paramref name="targetIndicatorId"/> is valid and being tracked. Otherwise,
        /// <c>false</c>.</returns>
        public bool TryGetTargetIndicator(TargetIndicatorId targetIndicatorId, out TargetIndicator targetIndicator)
        {
            EnsureInitialized();
            targetIndicator = TargetIndicator.Default;

            if (!_targetDataIndexById.TryGetValue(targetIndicatorId, out var index))
                return false;

            var targetData = _allTargetData[index];
            if (targetData.Target == null)
                return false;

            var hasValidReference = TryUpdateReference(out var referencePosition, out var referenceForward);

            var distance = 0f;
            var lookAtDot = 0f;

            if (hasValidReference)
            {
                var vectorToTarget = targetData.Target.position - referencePosition;
                var sqrMagnitude = vectorToTarget.sqrMagnitude;

                if (sqrMagnitude > 0.000001f)
                {
                    distance = Mathf.Sqrt(sqrMagnitude);

                    if (_calculateLookAtDot)
                    {
                        var normalizedDirection = vectorToTarget / distance;
                        lookAtDot = Vector3.Dot(referenceForward, normalizedDirection);
                    }
                }
            }

            targetIndicator = CreateTargetIndicator(
                targetData.Id,
                targetData.Target,
                distance,
                lookAtDot,
                hasValidReference);

            return true;
        }

        /// <summary>
        /// Attempts to remove a target indicator.
        /// </summary>
        /// <param name="targetIndicatorId">The ID of the target indicator to remove.</param>
        /// <returns><c>true</c> if the <paramref name="targetIndicatorId"/> is valid and being tracked.
        /// Otherwise <c>false</c>.</returns>
        public bool TryRemoveTarget(TargetIndicatorId targetIndicatorId)
        {
            EnsureInitialized();
            if (!_targetDataIndexById.TryGetValue(targetIndicatorId, out var index))
                return false;

            RemoveTargetAtIndex(index);
            return true;
        }

        /// <summary>
        /// Removes all targets that are being tracked.
        /// </summary>
        public void RemoveAllTargets()
        {
            EnsureInitialized();
            for (var i = _targetDataIndexById.Count - 1; i >= 0; i--)
            {
                RemoveTargetAtIndex(i);
            }
        }

        void RemoveTargetAtIndex(int index)
        {
            EnsureInitialized();
            var targetData = _allTargetData[index];
            if (targetData.State == State.Updated)
                _removedTargetIndicators[_removedSinceLastUpdate++] = targetData.Id;

            SwapBackAndRemove(index);
        }


        /// <summary>
        /// Gets the screen pose of a world space position. If there is no valid reference (see <see cref="Camera"/>),
        /// this safely returns <see cref="Pose.identity"/> and sets <paramref name="isOutsideBoundary"/> to <c>false</c>.
        /// </summary>
        /// <param name="worldSpacePosition">The world space position to convert to the screen pose.</param>
        /// <param name="isOutsideBoundary"><c>true</c> if the screen pose of <paramref name="worldSpacePosition"/> is
        /// outside of the configured boundary. Otherwise, <c>false</c>.</param>
        /// <returns>The screen space pose of <paramref name="worldSpacePosition"/>.</returns>
        public Pose GetScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            EnsureInitialized();
            if (!TryUpdateReference(out _, out _))
            {
                isOutsideBoundary = false;
                return Pose.identity;
            }

            return GetScreenPoseCore(worldSpacePosition, out isOutsideBoundary);
        }

        Pose GetScreenPoseCore(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            return _boundaryType switch
            {
                BoundaryType.Padded => GetPaddedScreenPose(worldSpacePosition, out isOutsideBoundary),
                BoundaryType.Absolute => GetAbsoluteScreenPose(worldSpacePosition, out isOutsideBoundary),
                BoundaryType.CompassTape => _compassTapeScreenPose.GetScreenPoseForCompassTape(worldSpacePosition, out isOutsideBoundary),
                BoundaryType.Unbounded => GetUnboundedScreenPose(worldSpacePosition, out isOutsideBoundary),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Checks if the screen space position is outside the configured boundary.
        /// If the <see cref="Camera"/> is null, this will safely return <c>false</c>.
        /// </summary>
        /// <param name="screenPoint">The screen point to test. Requires the point as a <c>Vector3</c> to
        /// allow for the inclusion of the depth from the camera in world space for faster calculations. If unknown, set
        /// <c>screenPoint.z</c> to 0.</param>
        /// <returns><c>true</c> if <paramref name="screenPoint"/> is outside the configured boundary.
        /// Otherwise, <c>false</c>.</returns>
        public bool IsOutsideBoundary(Vector3 screenPoint)
        {
            EnsureInitialized();
            if (_camera == null)
                return false;

            if (_boundaryType is BoundaryType.Padded or BoundaryType.Absolute
                && screenPoint.z < 0)
                return true;

            return _boundaryType switch
            {
                BoundaryType.Padded => IsOutsidePaddedBoundary(screenPoint),
                BoundaryType.Absolute => IsOutsideAbsoluteBoundary(screenPoint),
                BoundaryType.CompassTape => false,
                BoundaryType.Unbounded => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        TargetIndicator CreateTargetIndicator(TargetIndicatorId id, Transform target, float distance, float lookAtDot, bool hasValidReference)
        {
            if (!hasValidReference)
                return new TargetIndicator(id, target, Pose.identity, false, distance, lookAtDot);

            var screenPose = GetScreenPoseCore(target.position, out var isOutsideBoundary);
            return new TargetIndicator(id, target, screenPose, isOutsideBoundary, distance, lookAtDot);
        }

        Pose GetPaddedScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            return _boundaryShape switch
            {
                BoundaryShape.Rectangle => _rectangleScreenPose.GetPaddedScreenPose(worldSpacePosition,
                    out isOutsideBoundary),
                BoundaryShape.Ellipse => _ellipseScreenPose.GetPaddedScreenPose(worldSpacePosition,
                    out isOutsideBoundary),
                _ => throw new ArgumentOutOfRangeException(
                    $"Boundary shape with integer value {(int)_boundaryShape} is not supported.")
            };
        }

        Pose GetAbsoluteScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            return _boundaryShape switch
            {
                BoundaryShape.Rectangle => _rectangleScreenPose.GetAbsoluteScreenPose(worldSpacePosition, out isOutsideBoundary),
                BoundaryShape.Ellipse => _ellipseScreenPose.GetAbsoluteScreenPose(worldSpacePosition, out isOutsideBoundary),
                _ => throw new ArgumentOutOfRangeException(
                    $"Boundary shape with integer value {(int)_boundaryShape} is not supported.")
            };
        }

        Pose GetUnboundedScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            var screenPoint = _camera.WorldToScreenPoint(worldSpacePosition);

            if (screenPoint.z < 0)
                screenPoint.x = float.MaxValue;

            isOutsideBoundary = false;

            return new Pose(screenPoint, Quaternion.identity);
        }

        bool IsOutsidePaddedBoundary(Vector3 screenPoint)
        {
            return _boundaryShape switch
            {
                BoundaryShape.Rectangle => _rectangleScreenPose.IsOutsidePaddedBoundary(screenPoint),
                BoundaryShape.Ellipse => _ellipseScreenPose.IsOutsidePaddedBoundary(screenPoint),
                _ => throw new ArgumentOutOfRangeException(
                    $"Boundary shape with integer value {(int)_boundaryShape} is not supported.")
            };
        }

        bool IsOutsideAbsoluteBoundary(Vector3 screenPoint)
        {
            return _boundaryShape switch
            {
                BoundaryShape.Rectangle => _rectangleScreenPose.IsOutsideAbsoluteBoundary(screenPoint),
                BoundaryShape.Ellipse => _ellipseScreenPose.IsOutsideAbsoluteBoundary(screenPoint),
                _ => throw new ArgumentOutOfRangeException(
                    $"Boundary shape with integer value {(int)_boundaryShape} is not supported.")
            };
        }
    }
}
