namespace TargetIndicators.Samples
{
    /// <summary>
    /// The visibility type for visual indicator content.
    /// </summary>
    public enum IndicatorVisibility
    {
        /// <summary>
        /// Never show the visual indicator content.
        /// </summary>
        Never,

        /// <summary>
        /// Always show the visual indicator content.
        /// </summary>
        Always,

        /// <summary>
        /// Show the visual indicator content only when it is outside the boundary.
        /// </summary>
        OutsideBoundary,

        /// <summary>
        /// show the visual indicator content only when it is inside the boundary.
        /// </summary>
        InsideBoundary
    }
}
