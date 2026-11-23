using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose constructor rejects a negative count, and which has a value-type member set after construction.
    /// </summary>
    public sealed class GuardedCount
    {

        #region Public Properties

        /// <summary>
        /// Gets the count, set by the constructor.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets or sets a weight, set after construction.
        /// </summary>
        public double Weight { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Count">The count.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="Count"/> is negative.</exception>
        [XmlConstructor]
        public GuardedCount(int Count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(Count);
            this.Count = Count;
        }

        #endregion

    }

}
