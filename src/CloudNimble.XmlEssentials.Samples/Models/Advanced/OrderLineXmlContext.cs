using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A source-generated context for <see cref="OrderLine"/> that names its type info property <c>Line</c>.
    /// </summary>
    [XmlSerializable(typeof(OrderLine), TypeInfoPropertyName = "Line")]
    public partial class OrderLineXmlContext : XmlSerializerContext
    {
    }

}
