namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A positional record whose value-type members have default values.
    /// </summary>
    /// <param name="Name">The name.</param>
    /// <param name="Count">A count that is 5 when the document does not give one.</param>
    /// <param name="Scale">A scale that is 1.5 when the document does not give one.</param>
    public sealed record DefaultedRecord(string Name, int Count = 5, double Scale = 1.5);

}
