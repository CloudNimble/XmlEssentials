using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// Specifies how fast a parcel travels. One member has an XML name that is not a C# identifier.
    /// </summary>
    public enum ShippingMethod
    {

        /// <summary>
        /// Delivery in the usual time.
        /// </summary>
        Standard,

        /// <summary>
        /// Delivery on the next day, written as <c>next-day</c>.
        /// </summary>
        [XmlPropertyName("next-day")]
        NextDay,

    }

}
