namespace TargetIndicators
{
    /// <summary>
    /// The type of boundary that target indicators will clamp to.
    /// </summary>
    public enum BoundaryType
    {
        /// <summary>
        /// The boundary type is padded where target indicators clamp relative to the screen edges with an adjustable padding.
        /// </summary>
        Padded = 0,

        /// <summary>
        /// The boundary type is absolute where target indicators clamp relative to a defined size regardless of screen size.
        /// </summary>
        Absolute,

        /// <summary>
        /// The boundary type is compass tape where target indicators clamp between a value of 0 and 1.
        /// </summary>
        CompassTape,

        /// <summary>
        /// The boundary type is unbounded and is not clamped at all.
        /// </summary>
        Unbounded
    }
}
