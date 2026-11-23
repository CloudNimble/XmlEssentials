using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// An element with simple content and one attribute.
    /// </summary>
    public sealed class Money
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the amount, as the element text.
        /// </summary>
        [Bcl.XmlText]
        [Xe.XmlText]
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the currency code.
        /// </summary>
        [Bcl.XmlAttribute]
        [Xe.XmlAttribute]
        public string? Currency { get; set; }

        #endregion

    }

}
