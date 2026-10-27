using UnityEngine;

namespace TargetIndicators
{
    /// <summary>
    /// Visualizes the boundary configured in a `TargetIndicatorManager`. Note that
    /// <see cref="BoundaryType.CompassTape">BoundaryType.CompassTape</see> and
    /// <see cref="BoundaryType.Unbounded">BoundaryType.Unbounded</see> has no visualization.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TargetIndicatorManager))]
    [HelpURL("https://jakemanfre.github.io/target-indicators.github.io/manual/user_guide/target-indicator-manager.html#visualize-the-boundary")]
    public class TargetIndicatorBoundaryVisualizer : MonoBehaviour
    {
        const float k_depthOffset = 0.1f;
        const int k_ellipseSegmentsCount = 100;

        [SerializeField, Tooltip("The color of the boundary outline.")]
        Color _boundaryLineColor = new(86 / 255f, 255 / 255f, 86 / 255f, 1f);

        [SerializeField, Range(0, 5), Tooltip("The thickness of the boundary outline. Higher values draw a thicker line.")]
        float _width = 1.0f;

        [SerializeField, HideInInspector]
        TargetIndicatorManager _targetIndicatorManager;

        [SerializeField, HideInInspector]
        LineRenderer _lineRenderer;

        [SerializeField, HideInInspector]
        Camera _camera;

        [SerializeField, HideInInspector]
        Material _lineMaterial;

        void InitializeLineRenderer()
        {
#if UNITY_EDITOR
            if (!TryGetComponent(out _lineRenderer))
                _lineRenderer = UnityEditor.Undo.AddComponent<LineRenderer>(gameObject);
#else
            if (!TryGetComponent(out _lineRenderer))
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
#endif

            _lineRenderer.hideFlags = HideFlags.HideInInspector;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.positionCount = 5;

            if (_lineMaterial == null)
                _lineMaterial = new Material(Shader.Find("UI/Unlit/Detail"));

            _lineRenderer.sharedMaterial = _lineMaterial;
            _camera = _targetIndicatorManager.Camera;
        }

        void LateUpdate()
        {
            if (_targetIndicatorManager == null && !TryGetComponent(out _targetIndicatorManager))
                return;

            if (_lineRenderer == null)
            {
                InitializeLineRenderer();
            }
            else if (_lineMaterial == null || _lineRenderer.sharedMaterial == null)
            {
                if (_lineMaterial == null)
                    _lineMaterial = new Material(Shader.Find("UI/Unlit/Detail"));

                _lineRenderer.sharedMaterial = _lineMaterial;
            }

            _lineRenderer.startColor = _boundaryLineColor;
            _lineRenderer.endColor = _boundaryLineColor;

            switch (_targetIndicatorManager.BoundaryType)
            {
                case BoundaryType.Padded:
                case BoundaryType.Absolute:
                    DrawBoundaryLines();
                    break;
                case BoundaryType.CompassTape:
                case BoundaryType.Unbounded:
                    _lineRenderer.enabled = false;
                    break;
            }
        }

        void DrawBoundaryLines()
        {
            switch (_targetIndicatorManager.BoundaryShape)
            {
                case BoundaryShape.Rectangle:
                    _lineRenderer.enabled = true;
                    DrawRectangleLines();
                    break;
                case BoundaryShape.Ellipse:
                    _lineRenderer.enabled = true;
                    DrawEllipseLines();
                    break;
            }
        }

        void DrawRectangleLines()
        {
            if (_camera == null)
                return;

            _lineRenderer.positionCount = 4;
            _lineRenderer.loop = true;
            var depth = _camera.nearClipPlane + k_depthOffset;
            var rect = _targetIndicatorManager.Rectangle;

            var point0 = _camera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMin, depth));
            var point1 = _camera.ScreenToWorldPoint(new Vector3(rect.xMin, rect.yMax, depth));
            var point2 = _camera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMax, depth));
            var point3 = _camera.ScreenToWorldPoint(new Vector3(rect.xMax, rect.yMin, depth));

            _lineRenderer.SetPosition(0, point0);
            _lineRenderer.SetPosition(1, point1);
            _lineRenderer.SetPosition(2, point2);
            _lineRenderer.SetPosition(3, point3);

            var normalizedWidth = _width / Screen.height;
            _lineRenderer.startWidth = normalizedWidth;
            _lineRenderer.endWidth = normalizedWidth;
        }

        void DrawEllipseLines()
        {
            if (_camera == null)
                return;

            _lineRenderer.positionCount = k_ellipseSegmentsCount;
            _lineRenderer.loop = true;
            var depth = _camera.nearClipPlane + k_depthOffset;
            var ellipse = _targetIndicatorManager.Ellipse;

            for (var i = 0; i < k_ellipseSegmentsCount; i += 1)
            {
                var theta = i * 2f * Mathf.PI / k_ellipseSegmentsCount;
                var pixelPos = ellipse.Center + new Vector2(
                    ellipse.SemiMajorAxisLength * Mathf.Cos(theta),
                    ellipse.SemiMinorAxisLength * Mathf.Sin(theta)
                );

                var point = _camera.ScreenToWorldPoint(new Vector3(pixelPos.x, pixelPos.y, depth));
                _lineRenderer.SetPosition(i, point);
            }

            var normalizedWidth = _width / Screen.height;
            _lineRenderer.startWidth = normalizedWidth;
            _lineRenderer.endWidth = normalizedWidth;
        }

        void OnDestroy()
        {
            // Only clean up the material if we are in the Editor and not playing.
            // In a build, scene unloading handles this naturally.
            if (!Application.isPlaying && _lineMaterial != null)
                DestroyImmediate(_lineMaterial);
        }
    }
}
