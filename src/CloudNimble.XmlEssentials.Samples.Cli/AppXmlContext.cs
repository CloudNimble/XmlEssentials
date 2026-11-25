using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Cli
{

    /// <summary>
    /// Source-generated context used by the CLI sample. Native AOT uses these type-info overloads.
    /// </summary>
    [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
    [XmlSerializable<Person>]
    internal partial class AppXmlContext : XmlSerializerContext
    {
    }

}
