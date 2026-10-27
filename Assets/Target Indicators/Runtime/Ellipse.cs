using UnityEngine;

namespace TargetIndicators
{
    /// <summary>
    /// The data that defines the ellipse of the screen boundary.
    /// </summary>
    public struct Ellipse
    {
        /// <summary>
        /// The coordinates of the center of the ellipse in screen space.
        /// </summary>
        public Vector2 Center { get; private set; }

        /// <summary>
        /// The length of the semi major axis, or half of the horizontal diameter.
        /// </summary>
        public float SemiMajorAxisLength { get; private set; }

        /// <summary>
        /// The length of the semi minor axis, or half of the vertical diameter.
        /// </summary>
        public float SemiMinorAxisLength { get; private set; }

        /// <summary>
        /// Constructs ellipse data by the center and axis lengths. This is provided by the <see cref="TargetIndicatorManager"/>.
        /// </summary>
        /// <param name="center">The coordinates of the center of the ellipse in screen space.</param>
        /// <param name="semiMajorAxisLength">The length of the semi major axis, or half of the horizontal diameter.</param>
        /// <param name="semiMinorAxisLength">The length of the semi minor axis, or half of the vertical diameter.</param>
        public Ellipse(Vector2 center, float semiMajorAxisLength, float semiMinorAxisLength)
        {
            Center = center;
            SemiMajorAxisLength = semiMajorAxisLength;
            SemiMinorAxisLength = semiMinorAxisLength;
        }
    }
}
