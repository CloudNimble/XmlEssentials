using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The source-generated XML metadata for <see cref="Delivery"/>.
    /// </summary>
    [XmlSourceGenerationOptions(
        OmitXmlDeclaration = true,
        PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase)]
    [XmlSerializable<Delivery>]
    public partial class DeliveryXmlContext : XmlSerializerContext
    {
    }

}
