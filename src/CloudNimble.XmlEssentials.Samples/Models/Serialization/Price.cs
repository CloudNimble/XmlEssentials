using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// An amount of money, written as an element whose text is the amount and whose attribute is the currency.
    /// </summary>
    public sealed class Price
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the ISO 4217 currency code, written as an attribute.
        /// </summary>
        [XmlAttribute]
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets the amount, written as the text of the element.
        /// </summary>
        [XmlText]
        public decimal Amount { get; set; }

        #endregion

    }

}
