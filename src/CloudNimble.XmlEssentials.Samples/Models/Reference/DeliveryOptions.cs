using System;

namespace CloudNimble.XmlEssentials.Samples.Models.Reference
{

    /// <summary>
    /// Specifies the extras a customer chose for a delivery.
    /// </summary>
    [Flags]
    public enum DeliveryOptions
    {

        /// <summary>
        /// No extras.
        /// </summary>
        None = 0,

        /// <summary>
        /// The parcel is wrapped as a gift.
        /// </summary>
        Gift = 1,

        /// <summary>
        /// The parcel is sent by the fastest service.
        /// </summary>
        Express = 2,

        /// <summary>
        /// The parcel is insured.
        /// </summary>
        Insured = 4,

    }

}
