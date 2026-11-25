using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A range of dates with two constructors, one of them marked for deserialization.
    /// </summary>
    public sealed class DateRange
    {

        #region Public Properties

        /// <summary>
        /// Gets the first day of the range.
        /// </summary>
        public DateOnly Start { get; }

        /// <summary>
        /// Gets the last day of the range.
        /// </summary>
        public DateOnly End { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DateRange"/> class that covers the specified number of days.
        /// </summary>
        /// <param name="start">The first day of the range.</param>
        /// <param name="days">The number of days in the range.</param>
        public DateRange(DateOnly start, int days)
            : this(start, start.AddDays(days - 1))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateRange"/> class from its first and last day.
        /// </summary>
        /// <param name="start">The first day of the range.</param>
        /// <param name="end">The last day of the range.</param>
        [XmlConstructor]
        public DateRange(DateOnly start, DateOnly end)
        {
            Start = start;
            End = end;
        }

        #endregion

    }

}
