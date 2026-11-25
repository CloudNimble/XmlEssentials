using System.Text.Json.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// The System.Text.Json counterpart of <see cref="GraphXmlContext"/>.
    /// </summary>
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(Customer))]
    internal partial class GraphJsonContext : JsonSerializerContext
    {
    }

}
