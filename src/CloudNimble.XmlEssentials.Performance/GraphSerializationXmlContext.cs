using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// A source-generated context for the <see cref="Customer"/> graph that holds the write fast path only.
    /// </summary>
    [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Serialization)]
    [XmlSerializable<Customer>]
    internal partial class GraphSerializationXmlContext : XmlSerializerContext
    {
    }

}
