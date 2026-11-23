using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A postal address whose elements are written in an order set by <see cref="XmlPropertyOrderAttribute"/>.
    /// </summary>
    public sealed class Address
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the postal code. It has no order, so it follows the ordered members.
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the city, written second.
        /// </summary>
        [XmlPropertyOrder(1)]
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the street, written first.
        /// </summary>
        [XmlPropertyOrder(0)]
        public string? Street { get; set; }

        #endregion

    }

}
