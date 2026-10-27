using UnityEngine;

namespace TargetIndicators
{
    class ScreenData
    {
        readonly TargetIndicatorManager _targetIndicatorManager;

        public static Vector2 ScreenCenter => new(HalfScreenWidth, HalfScreenHeight);

        public static float HalfScreenWidth => Screen.width * 0.5f;

        public static float HalfScreenHeight => Screen.height * 0.5f;

        public Camera Camera => _targetIndicatorManager.Camera;

        public float LeftPadding => _targetIndicatorManager.LeftPadding * _targetIndicatorManager.BoundaryScale;

        public float RightPadding => _targetIndicatorManager.RightPadding * _targetIndicatorManager.BoundaryScale;

        public float TopPadding => _targetIndicatorManager.TopPadding * _targetIndicatorManager.BoundaryScale;

        public float BottomPadding => _targetIndicatorManager.BottomPadding * _targetIndicatorManager.BoundaryScale;

        public float Width => _targetIndicatorManager.Width * _targetIndicatorManager.BoundaryScale;

        public float Height => _targetIndicatorManager.Height * _targetIndicatorManager.BoundaryScale;

        public ScreenData(TargetIndicatorManager targetIndicatorManager)
        {
            _targetIndicatorManager = targetIndicatorManager;
        }
    }
}
