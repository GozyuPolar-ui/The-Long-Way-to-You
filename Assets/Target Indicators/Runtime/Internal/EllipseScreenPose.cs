using UnityEngine;

namespace TargetIndicators
{
    class EllipseScreenPose
    {
        readonly ScreenData _screenData;

        float _ellipseSemiMajorAxisLength;
        float _ellipseSemiMinorAxisLength;
        Vector2 _ellipseCenter;

        internal EllipseScreenPose(ScreenData screenData)
        {
            _screenData = screenData;
        }

        internal static Vector2 GetPaddedEllipseCenter(float leftPadding, float rightPadding, float topPadding, float bottomPadding)
        {
            return new Vector2
            {
                x = leftPadding + (Screen.width - leftPadding - rightPadding) * 0.5f,
                y = bottomPadding + (Screen.height - bottomPadding - topPadding) * 0.5f
            };
        }

        internal static Vector2 GetAbsoluteEllipseCenter()
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        internal bool IsOutsidePaddedBoundary(Vector3 screenPoint)
        {
            if (screenPoint.z < 0)
                return true;

            UpdatePaddedSizeData();

            var xMinusCenterX = screenPoint.x - _ellipseCenter.x;
            var yMinusCenterY = screenPoint.y - _ellipseCenter.y;

            var majorAxisLengthSquared = _ellipseSemiMajorAxisLength * _ellipseSemiMajorAxisLength;
            var minorAxisLengthSquared = _ellipseSemiMinorAxisLength * _ellipseSemiMinorAxisLength;

            var part1 = xMinusCenterX * xMinusCenterX / majorAxisLengthSquared;
            var part2 = yMinusCenterY * yMinusCenterY / minorAxisLengthSquared;
            return part1 + part2 > 1.0f;
        }

        internal bool IsOutsideAbsoluteBoundary(Vector3 screenPoint)
        {
            if (screenPoint.z < 0)
                return true;

            UpdateAbsoluteSizeData();

            var xMinusCenterX = screenPoint.x - _ellipseCenter.x;
            var yMinusCenterY = screenPoint.y - _ellipseCenter.y;

            var majorAxisLengthSquared = _ellipseSemiMajorAxisLength * _ellipseSemiMajorAxisLength;
            var minorAxisLengthSquared = _ellipseSemiMinorAxisLength * _ellipseSemiMinorAxisLength;

            var part1 = xMinusCenterX * xMinusCenterX / majorAxisLengthSquared;
            var part2 = yMinusCenterY * yMinusCenterY / minorAxisLengthSquared;
            return part1 + part2 > 1.0f;
        }

        internal Pose GetPaddedScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            var screenPoint = _screenData.Camera.WorldToScreenPoint(worldSpacePosition);
            if (screenPoint.z < 0)
            {
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }

            isOutsideBoundary = IsOutsidePaddedBoundary(screenPoint);
            if (isOutsideBoundary)
                screenPoint = ProjectOnEllipse(screenPoint);

            var screenPoint2D = new Vector2(screenPoint.x, screenPoint.y);
            var vectorToScreenPoint = (screenPoint2D - ScreenData.ScreenCenter).normalized;
            var angle = Mathf.Atan2(vectorToScreenPoint.y, vectorToScreenPoint.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, 0, angle);

            return new Pose(screenPoint, rotation);
        }

        internal Pose GetAbsoluteScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            var screenPoint = _screenData.Camera.WorldToScreenPoint(worldSpacePosition);
            if (screenPoint.z < 0)
            {
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }

            isOutsideBoundary = IsOutsideAbsoluteBoundary(screenPoint);
            if (isOutsideBoundary)
                screenPoint = ProjectOnEllipse(screenPoint);

            var screenPoint2D = new Vector2(screenPoint.x, screenPoint.y);
            var vectorToScreenPoint = (screenPoint2D - (ScreenData.ScreenCenter)).normalized;
            var angle = Mathf.Atan2(vectorToScreenPoint.y, vectorToScreenPoint.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, 0, angle);

            return new Pose(screenPoint, rotation);
        }

        void UpdatePaddedSizeData()
        {
            _ellipseSemiMajorAxisLength = Mathf.Max(0.0001f, (Screen.width - _screenData.LeftPadding - _screenData.RightPadding) * 0.5f);
            _ellipseSemiMinorAxisLength = Mathf.Max(0.0001f, (Screen.height - _screenData.TopPadding - _screenData.BottomPadding) * 0.5f);

            _ellipseCenter = new Vector2
            {
                x = _screenData.LeftPadding + _ellipseSemiMajorAxisLength,
                y = _screenData.BottomPadding + _ellipseSemiMinorAxisLength
            };
        }

        void UpdateAbsoluteSizeData()
        {
            _ellipseSemiMajorAxisLength = Mathf.Max(0.0001f, _screenData.Width * 0.5f);
            _ellipseSemiMinorAxisLength = Mathf.Max(0.0001f, _screenData.Height * 0.5f);

            _ellipseCenter = GetAbsoluteEllipseCenter();
        }

        Vector2 ProjectOnEllipse(Vector2 screenPoint)
        {
            var direction = screenPoint - _ellipseCenter;

            // Safety check to prevent DivideByZero if the target is exactly at the center.
            // (Though practically impossible since this is only called when outside the boundary).
            if (Mathf.Approximately(direction.x, 0f) && Mathf.Approximately(direction.y, 0f))
                return _ellipseCenter;

            // Algebraic projection: t = 1 / sqrt((dx/a)^2 + (dy/b)^2)
            var dxOverA = direction.x / _ellipseSemiMajorAxisLength;
            var dyOverB = direction.y / _ellipseSemiMinorAxisLength;

            var denominator = Mathf.Sqrt((dxOverA * dxOverA) + (dyOverB * dyOverB));
            var t = 1f / denominator;

            return _ellipseCenter + (direction * t);
        }
    }
}
