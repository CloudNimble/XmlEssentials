using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// Source-generated context for the AOT smoke. Reflection is disabled in this process.
    /// </summary>
    [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
    [XmlSerializable(typeof(Person))]
    internal partial class AppXmlContext : XmlSerializerContext
    {
    }

}
