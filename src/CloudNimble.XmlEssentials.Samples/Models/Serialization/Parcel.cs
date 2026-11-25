namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A parcel with an enumeration value.
    /// </summary>
    public sealed class Parcel
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the tracking number.
        /// </summary>
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Gets or sets where the parcel is.
        /// </summary>
        public ParcelStatus Status { get; set; }

        #endregion

    }

}
