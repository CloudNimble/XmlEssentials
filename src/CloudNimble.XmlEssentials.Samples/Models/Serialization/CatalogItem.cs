using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A product whose root element, attribute, and child elements all have names that differ from the C# names.
    /// </summary>
    [XmlRoot("product")]
    public sealed class CatalogItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the stock keeping unit, written as the <c>sku</c> attribute.
        /// </summary>
        [XmlAttribute("sku")]
        public string? Sku { get; set; }

        /// <summary>
        /// Gets or sets the product name, written as the <c>title</c> element.
        /// </summary>
        [XmlPropertyName("title")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the price of one unit, written as the <c>unit-price</c> element.
        /// </summary>
        [XmlElement("unit-price")]
        public decimal UnitPrice { get; set; }

        #endregion

    }

}
