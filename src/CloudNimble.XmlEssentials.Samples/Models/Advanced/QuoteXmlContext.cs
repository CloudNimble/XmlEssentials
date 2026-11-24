using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A context for <see cref="Quote"/> in <see cref="XmlSourceGenerationMode.Metadata"/> mode, the mode
    /// that applies custom converters to the members of a type.
    /// </summary>
    [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata)]
    [XmlSerializable<Quote>]
    public partial class QuoteXmlContext : XmlSerializerContext
    {
    }

}
