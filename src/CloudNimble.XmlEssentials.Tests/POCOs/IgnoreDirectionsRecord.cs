using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose never-read member fills a constructor parameter, which is still filled.
    /// </summary>
    /// <param name="Name">A value marked as never read.</param>
    /// <param name="Other">A value with no mark.</param>
    public sealed record IgnoreDirectionsRecord([property: XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)] string Name, string Other);

}
