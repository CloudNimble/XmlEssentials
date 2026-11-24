using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A context for <see cref="Order"/> generated in <see cref="XmlSourceGenerationMode.Metadata"/> mode.
    /// </summary>
    [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata)]
    [XmlSerializable<Order>]
    public partial class MetadataOrderXmlContext : XmlSerializerContext
    {
    }

}
