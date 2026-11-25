using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The source-generated serialization metadata for the migration samples. It takes the place of a serializer assembly made by <c>sgen</c>.
    /// </summary>
    [XmlSerializable<OrderLine>]
    internal sealed partial class MigrationXmlContext : XmlSerializerContext
    {
    }

}
