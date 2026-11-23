namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A temperature reading, declared as a positional record.
    /// </summary>
    /// <param name="Degrees">The reading.</param>
    /// <param name="Unit">The unit of the reading.</param>
    public sealed record Temperature(double Degrees, string Unit);

}
