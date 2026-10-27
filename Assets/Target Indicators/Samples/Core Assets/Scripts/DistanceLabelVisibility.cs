namespace TargetIndicators.Samples
{
    /// <summary>
    /// Defines the condition under which the distance text label should be displayed on a visual indicator.
    /// </summary>
    public enum DistanceLabelVisibility
    {
        /// <summary>
        /// The distance label is never displayed.
        /// </summary>
        Never,

        /// <summary>
        /// The distance label is always displayed, regardless of where the player is looking.
        /// </summary>
        Always,

        /// <summary>
        /// The distance label is only displayed when the reference forward vector is looking at the target within a
        /// defined threshold.
        /// </summary>
        LookAt,
    }
}
