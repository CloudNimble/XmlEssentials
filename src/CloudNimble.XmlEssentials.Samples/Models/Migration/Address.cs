using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The <c>Address</c> type of the <c>System.Text.Xml</c> proposal's namespace example. Each member names the address namespace.
    /// </summary>
    public sealed class Address
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        [XmlElement(Namespace = "http://example.com/address")]
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        [XmlElement(Namespace = "http://example.com/address")]
        public string? Country { get; set; }

        #endregion

    }

}
