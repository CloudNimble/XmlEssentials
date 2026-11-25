using System.Text.Json.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// The System.Text.Json counterpart of <see cref="GraphMetadataXmlContext"/>.
    /// </summary>
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Customer))]
    internal partial class GraphMetadataJsonContext : JsonSerializerContext
    {
    }

}
