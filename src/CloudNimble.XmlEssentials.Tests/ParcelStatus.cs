using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A status whose second member has an XML name that is not a C# identifier.
    /// </summary>
    public enum ParcelStatus
    {

        /// <summary>
        /// The parcel is registered.
        /// </summary>
        Created,

        /// <summary>
        /// The parcel is on its way. Written as <c>in-transit</c>.
        /// </summary>
        [XmlPropertyName("in-transit")]
        InTransit,

        /// <summary>
        /// The parcel has arrived.
        /// </summary>
        Delivered,
    }

}
