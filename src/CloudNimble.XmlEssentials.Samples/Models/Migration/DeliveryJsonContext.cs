using System.Text.Json.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The source-generated JSON metadata for <see cref="Delivery"/>.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Delivery))]
    public partial class DeliveryJsonContext : JsonSerializerContext
    {
    }

}
