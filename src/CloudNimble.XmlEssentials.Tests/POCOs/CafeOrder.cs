using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose element names hold characters outside ASCII.
    /// </summary>
    [XmlRoot("Café")]
    public sealed class CafeOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a member whose element name is not ASCII.
        /// </summary>
        [XmlPropertyName("Crème")]
        public string? Cream { get; set; }

        /// <summary>
        /// Gets or sets a member whose element name is ASCII.
        /// </summary>
        public string? Sugar { get; set; }

        #endregion

    }

}
