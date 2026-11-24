using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A position on the map. <see cref="GeoPointConverter"/> reads and writes it wherever it appears.
    /// </summary>
    [XmlConverter<GeoPointConverter>]
    public readonly struct GeoPoint
    {

        #region Public Properties

        /// <summary>
        /// Gets the latitude, in degrees.
        /// </summary>
        public double Latitude { get; }

        /// <summary>
        /// Gets the longitude, in degrees.
        /// </summary>
        public double Longitude { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoPoint"/> struct.
        /// </summary>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        public GeoPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        #endregion

    }

}
