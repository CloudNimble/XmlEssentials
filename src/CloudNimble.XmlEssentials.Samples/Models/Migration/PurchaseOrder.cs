using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A purchase order mapped with the attributes that share their names with <c>System.Xml.Serialization</c>.
    /// </summary>
    [XmlRoot("purchaseOrder")]
    public sealed class PurchaseOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the customer's name, written as the <c>customer</c> element.
        /// </summary>
        [XmlElement("customer")]
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the order number, written as the <c>id</c> attribute.
        /// </summary>
        [XmlAttribute("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a note that stays out of the XML.
        /// </summary>
        [XmlIgnore]
        public string? InternalNote { get; set; }

        /// <summary>
        /// Gets or sets the order lines, written as <c>item</c> elements inside an <c>items</c> element.
        /// </summary>
        [XmlArray("items")]
        [XmlArrayItem("item")]
        public List<OrderLine> Lines { get; set; } = [];

        #endregion

    }

}
