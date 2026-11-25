using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// An order: an identifier written as an attribute, a customer name, and one <c>Line</c> element per line.
    /// </summary>
    public sealed class Order
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the customer who placed the order.
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the order number.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the lines of the order. Each line is written as a <c>Line</c> element.
        /// </summary>
        [XmlElement("Line")]
        public List<OrderLine> Lines { get; set; } = [];

        #endregion

    }

}
