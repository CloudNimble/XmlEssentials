namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A record created through its constructor, so its values are held until its end tag has been read.
    /// </summary>
    /// <param name="Sku">The stock keeping unit.</param>
    /// <param name="Quantity">The quantity.</param>
    public sealed record ResumeLine(string Sku, int Quantity);

}
