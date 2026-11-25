using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// A source-generated context for the <see cref="Customer"/> graph that holds metadata only, with no write fast path.
    /// </summary>
    [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata)]
    [XmlSerializable<Customer>]
    internal partial class GraphMetadataXmlContext : XmlSerializerContext
    {
    }

}
