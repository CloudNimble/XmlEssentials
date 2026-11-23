using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A source-generated context for <see cref="Order"/> whose default options are set with
    /// <see cref="XmlSourceGenerationOptionsAttribute"/>.
    /// </summary>
    [XmlSourceGenerationOptions(
        OmitXmlDeclaration = true,
        PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase)]
    [XmlSerializable(typeof(Order))]
    public partial class CamelCaseOrderXmlContext : XmlSerializerContext
    {
    }

}
