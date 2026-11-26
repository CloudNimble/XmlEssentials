using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// A source-generated context for a list of <see cref="Vertex"/> that holds metadata only, so the engine reads
    /// and writes each member through its generated accessors.
    /// </summary>
    [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata)]
    [XmlSerializable<List<Vertex>>]
    internal partial class VertexMetadataXmlContext : XmlSerializerContext
    {
    }

}
