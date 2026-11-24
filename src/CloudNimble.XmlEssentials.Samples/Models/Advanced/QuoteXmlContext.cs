using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A context for <see cref="Quote"/>. A converter in the options is applied to the members of the type.
    /// </summary>
    [XmlSerializable<Quote>]
    public partial class QuoteXmlContext : XmlSerializerContext
    {
    }

}
