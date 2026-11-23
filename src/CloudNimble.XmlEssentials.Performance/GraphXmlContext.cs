using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Source-generated context for the nested Customer graph used by <see cref="XmlSerializerGraphBenchmarks"/>.
    /// </summary>
    [XmlSerializable(typeof(Customer))]
    internal partial class GraphXmlContext : XmlSerializerContext
    {
    }

}
