namespace TargetIndicators
{
    /// <summary>
    /// The shape of the boundary for <see cref="BoundaryType.Padded"/> or <see cref="BoundaryType.Absolute"/> boundary types that target indicators will clamp to.
    /// </summary>
    public enum BoundaryShape
    {
        /// <summary>
        /// The boundary shape is a rectangle.
        /// </summary>
        Rectangle = 0,

        /// <summary>
        /// The boundary shape is an ellipse.
        /// </summary>
        Ellipse,
    }
}
