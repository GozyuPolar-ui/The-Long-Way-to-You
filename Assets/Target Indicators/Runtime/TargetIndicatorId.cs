using System;

namespace TargetIndicators
{
    /// <summary>
    /// The unique ID that represents a `TargetIndicator`.
    /// </summary>
    public readonly struct TargetIndicatorId : IEquatable<TargetIndicatorId>
    {
        /// <summary>
        /// The underlying Guid that makes up the `TargetIndicatorId`.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// constructs the `TargetIndicatorId`.
        /// </summary>
        /// <param name="id"></param>
        public TargetIndicatorId(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        /// <param name="a">The first `TargetIndicatorId` to compare.</param>
        /// <param name="b">The second `TargetIndicatorId` to compare.</param>
        /// <returns>`true` if they are equal, otherwise `false`.</returns>
        public static bool operator ==(TargetIndicatorId a, TargetIndicatorId b)
        {
            return a.Id == b.Id;
        }

        /// <summary>
        /// Tests for inequality.
        /// </summary>
        /// <param name="a">The first `TargetIndicatorId` to compare.</param>
        /// <param name="b">The second `TargetIndicatorId` to compare.</param>
        /// <returns>`true` if they are not equal, otherwise `false`.</returns>
        public static bool operator !=(TargetIndicatorId a, TargetIndicatorId b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        /// <param name="other">The `TargetIndicatorId` to compare.</param>
        /// <returns>`true` if they are equal, otherwise `false`.</returns>
        public bool Equals(TargetIndicatorId other)
        {
            return Id == other.Id;
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>`true` if they are equal, otherwise `false`.</returns>
        public override bool Equals(object obj)
        {
            return obj is TargetIndicatorId other && Equals(other);
        }

        /// <summary>
        /// Generates a hash code suitable for use in a `Dictionary` or `Set`.
        /// </summary>
        /// <returns>A hash code for participation in certain collections.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
