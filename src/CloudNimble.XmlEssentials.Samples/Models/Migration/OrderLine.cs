using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// One line of a <see cref="PurchaseOrder"/>: an attribute and the element's text.
    /// </summary>
    public sealed class OrderLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the description, written as the text of the element.
        /// </summary>
        [XmlText]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the stock keeping unit, written as the <c>sku</c> attribute.
        /// </summary>
        [XmlAttribute("sku")]
        public string? Sku { get; set; }

        #endregion

    }

}
