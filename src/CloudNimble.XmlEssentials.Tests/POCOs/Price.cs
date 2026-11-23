using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A price with a currency attribute and simple-content amount.
    /// </summary>
    public sealed class Price
    {

        #region Public Properties

        /// <summary>
        /// Gets the amount.
        /// </summary>
        [XmlText]
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets the currency code.
        /// </summary>
        [XmlAttribute]
        public string? Currency { get; set; }

        #endregion

    }

}
