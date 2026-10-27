using UnityEngine;

namespace TargetIndicators
{
    class CompassTapeScreenPose
    {
        const float k_twoPi = Mathf.PI * 2f;

        Vector3 _referencePosition;
        Vector3 _referenceForward;

        internal void UpdateReferenceState(in Vector3 position, in Vector3 forward)
        {
            _referencePosition = position;
            _referenceForward = forward;
        }

        internal Pose GetScreenPoseForCompassTape(Vector3 worldSpacePosition, out bool isOutsideBoundary)
        {
            isOutsideBoundary = false;

            var dx = worldSpacePosition.x - _referencePosition.x;
            var dz = worldSpacePosition.z - _referencePosition.z;

            // Fast distance check without square roots
            if (dx * dx + dz * dz < 0.0001f)
                return new Pose(new Vector3(0.5f, 0, 0), Quaternion.identity);

            var referenceAngle = Mathf.Atan2(_referenceForward.x, _referenceForward.z);
            var targetAngle = Mathf.Atan2(dx, dz);

            var deltaAngle = targetAngle - referenceAngle;

            // Wrap the delta angle to [-PI, PI] range
            if (deltaAngle > Mathf.PI)
                deltaAngle -= k_twoPi;
            else if (deltaAngle < -Mathf.PI)
                deltaAngle += k_twoPi;

            // Shift by PI (180 degrees) and scale down to the 0.0 - 1.0 range
            var x = (deltaAngle + Mathf.PI) / k_twoPi;

            return new Pose(new Vector3(x, 0f, 0f), Quaternion.identity);
        }
    }
}
