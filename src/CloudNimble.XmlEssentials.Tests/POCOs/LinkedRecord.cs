namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A positional record that holds another of its own type, so one is read while the one around it is still being read.
    /// </summary>
    /// <param name="Id">The number of this link.</param>
    /// <param name="Next">The next link, or null at the end.</param>
    public sealed record LinkedRecord(int Id, LinkedRecord? Next = null);

}
