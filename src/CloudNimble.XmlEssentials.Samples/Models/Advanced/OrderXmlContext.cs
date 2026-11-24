using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A source-generated context for <see cref="Order"/>. The generator adds the other half of this class.
    /// </summary>
    [XmlSerializable<Order>]
    public partial class OrderXmlContext : XmlSerializerContext
    {
    }

}
