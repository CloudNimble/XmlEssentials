using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A source-generated context for <see cref="Shipment"/>.
    /// </summary>
    [XmlSerializable(typeof(Shipment))]
    public partial class ShippingXmlContext : XmlSerializerContext
    {
    }

}
