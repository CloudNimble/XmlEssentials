namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A position stored in public fields.
    /// </summary>
    public sealed class Coordinates
    {

        #region Fields

        /// <summary>
        /// Represents the geodetic datum the position is measured against.
        /// </summary>
        public readonly string Datum = "WGS84";

        /// <summary>
        /// Represents the latitude, in degrees.
        /// </summary>
        public double Latitude;

        /// <summary>
        /// Represents the longitude, in degrees.
        /// </summary>
        public double Longitude;

        #endregion

    }

}
