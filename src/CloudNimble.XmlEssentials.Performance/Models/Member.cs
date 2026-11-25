namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A positional record, created through its constructor. The BCL serializer cannot map it.
    /// </summary>
    /// <param name="Name">The name.</param>
    /// <param name="Age">The age in years.</param>
    /// <param name="City">The city.</param>
    /// <param name="Active">Whether the member is active.</param>
    public sealed record Member(string Name, int Age, string City, bool Active);

}
