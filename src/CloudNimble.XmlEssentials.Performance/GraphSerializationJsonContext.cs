using System.Text.Json.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// The System.Text.Json counterpart of <see cref="GraphSerializationXmlContext"/>.
    /// </summary>
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
    [JsonSerializable(typeof(Customer))]
    internal partial class GraphSerializationJsonContext : JsonSerializerContext
    {
    }

}
