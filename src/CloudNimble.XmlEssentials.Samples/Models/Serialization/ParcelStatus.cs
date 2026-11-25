namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// Specifies where a parcel is on its way to the recipient.
    /// </summary>
    public enum ParcelStatus
    {

        /// <summary>
        /// The parcel has not been picked up.
        /// </summary>
        Pending,

        /// <summary>
        /// The parcel is on its way.
        /// </summary>
        InTransit,

        /// <summary>
        /// The parcel has arrived.
        /// </summary>
        Delivered,

    }

}
