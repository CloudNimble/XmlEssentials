using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A context for <see cref="Order"/> generated in <see cref="XmlSourceGenerationMode.Serialization"/> mode,
    /// which writes and cannot read.
    /// </summary>
    [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Serialization)]
    [XmlSerializable<Order>]
    public partial class SerializationOrderXmlContext : XmlSerializerContext
    {
    }

}
