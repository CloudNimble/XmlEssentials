namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A positional record mapped as child elements.
    /// </summary>
    /// <param name="Name">The given name.</param>
    /// <param name="Age">The age in years.</param>
    public sealed record PersonRecord(string Name, int Age);

}
