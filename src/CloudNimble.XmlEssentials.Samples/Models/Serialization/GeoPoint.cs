namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A position on the globe, declared as a read-only record struct.
    /// </summary>
    /// <param name="Latitude">The latitude, in degrees.</param>
    /// <param name="Longitude">The longitude, in degrees.</param>
    public readonly record struct GeoPoint(double Latitude, double Longitude);

}
