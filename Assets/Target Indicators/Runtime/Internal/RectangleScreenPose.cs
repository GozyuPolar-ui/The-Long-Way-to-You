using UnityEngine;

namespace TargetIndicators
{
    class RectangleScreenPose
    {
        readonly ScreenData _screenData;

        static float s_radius => Screen.width + Screen.height;

        float _rightBoundary;
        float _leftBoundary;
        float _topBoundary;
        float _bottomBoundary;

        internal RectangleScreenPose(ScreenData screenData)
        {
            _screenData = screenData;
        }

        internal bool IsOutsidePaddedBoundary(Vector3 screenPoint)
        {
            if (screenPoint.z < 0)
                return true;

            return
                screenPoint.x < _screenData.LeftPadding ||
                screenPoint.x > Screen.width - _screenData.RightPadding ||
                screenPoint.y < _screenData.BottomPadding ||
                screenPoint.y > Screen.height - _screenData.TopPadding;
        }

        internal bool IsOutsideAbsoluteBoundary(Vector3 screenPoint)
        {
            if (screenPoint.z < 0)
                return true;

            var halfAbsoluteWidth = _screenData.Width * 0.5f;
            var halfAbsoluteHeight = _screenData.Height * 0.5f;

            return
                screenPoint.x < ScreenData.ScreenCenter.x - halfAbsoluteWidth ||
                screenPoint.x > ScreenData.ScreenCenter.x + halfAbsoluteWidth ||
                screenPoint.y < ScreenData.ScreenCenter.y - halfAbsoluteHeight ||
                screenPoint.y > ScreenData.ScreenCenter.y + halfAbsoluteHeight;
        }

        internal Pose GetPaddedScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            UpdatePaddedSizeData();

            var screenPoint = _screenData.Camera.WorldToScreenPoint(worldSpacePosition);
            if (screenPoint.z < 0)
            {
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }

            isOutsideBoundary = IsOutsidePaddedBoundary(screenPoint);
            if (isOutsideBoundary)
                screenPoint = ProjectOnRectangle(screenPoint);

            var screenPoint2D = new Vector2(screenPoint.x, screenPoint.y);
            var vectorToScreenPoint = (screenPoint2D - ScreenData.ScreenCenter).normalized;
            var angle = Mathf.Atan2(vectorToScreenPoint.y, vectorToScreenPoint.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, 0, angle);

            return new Pose(screenPoint, rotation);
        }

        internal Pose GetAbsoluteScreenPose(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            UpdateAbsoluteSizeData();

            var screenPoint = _screenData.Camera.WorldToScreenPoint(worldSpacePosition);
            if (screenPoint.z < 0)
            {
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }

            isOutsideBoundary = IsOutsideAbsoluteBoundary(screenPoint);
            if (isOutsideBoundary)
                screenPoint = ProjectOnRectangle(screenPoint);

            var screenPoint2D = new Vector2(screenPoint.x, screenPoint.y);
            var vectorToScreenPoint = (screenPoint2D - ScreenData.ScreenCenter).normalized;
            var angle = Mathf.Atan2(vectorToScreenPoint.y, vectorToScreenPoint.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, 0, angle);

            return new Pose(screenPoint, rotation);
        }

        void UpdatePaddedSizeData()
        {
            _rightBoundary = ScreenData.HalfScreenWidth - _screenData.RightPadding;
            _leftBoundary = -ScreenData.HalfScreenWidth + _screenData.LeftPadding;
            _topBoundary = ScreenData.HalfScreenHeight - _screenData.TopPadding;
            _bottomBoundary = -ScreenData.HalfScreenHeight + _screenData.BottomPadding;
        }

        void UpdateAbsoluteSizeData()
        {
            _rightBoundary = _screenData.Width * 0.5f;
            _leftBoundary = -_rightBoundary;
            _topBoundary = _screenData.Height * 0.5f;
            _bottomBoundary = -_topBoundary;
        }

        Vector2 ProjectOnRectangle(Vector2 screenPoint)
        {
            var vectorToPoint = (screenPoint - ScreenData.ScreenCenter).normalized * s_radius;
            if (Mathf.Abs(vectorToPoint.x) < 0.0001f)
            {
                vectorToPoint.x = 0f;
                vectorToPoint.y = vectorToPoint.y > 0 ? _topBoundary : _bottomBoundary;
                return ScreenData.ScreenCenter + vectorToPoint;
            }

            var slope = vectorToPoint.y / vectorToPoint.x;

            // Bounded by right edge
            if (vectorToPoint.x > _rightBoundary)
            {
                vectorToPoint.x = _rightBoundary;
                vectorToPoint.y = vectorToPoint.x * slope;
            }

            // Bounded by left edge
            if (vectorToPoint.x < _leftBoundary)
            {
                vectorToPoint.x = _leftBoundary;
                vectorToPoint.y = vectorToPoint.x * slope;
            }

            // Bounded by top edge
            if (vectorToPoint.y > _topBoundary)
            {
                vectorToPoint.y = _topBoundary;
                vectorToPoint.x = vectorToPoint.y / slope;
            }

            // Bounded by bottom edge
            if (vectorToPoint.y < _bottomBoundary)
            {
                vectorToPoint.y = _bottomBoundary;
                vectorToPoint.x = vectorToPoint.y / slope;
            }

            return ScreenData.ScreenCenter + vectorToPoint;
        }
    }
}
