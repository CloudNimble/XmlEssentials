using System;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A positional record whose members are value types of several sizes.
    /// </summary>
    /// <param name="Count">A 32-bit count.</param>
    /// <param name="Total">A 64-bit total.</param>
    /// <param name="Ratio">A ratio.</param>
    /// <param name="Flagged">A flag.</param>
    /// <param name="Day">An enum.</param>
    /// <param name="Id">A 16-byte identifier.</param>
    public sealed record SampleRecord(int Count, long Total, double Ratio, bool Flagged, DayOfWeek Day, Guid Id);

}
