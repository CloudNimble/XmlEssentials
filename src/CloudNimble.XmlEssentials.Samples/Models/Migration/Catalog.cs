using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The <c>Catalog</c> type of the <c>System.Text.Xml</c> proposal's polymorphism example.
    /// </summary>
    public sealed class Catalog
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the products, written inside an <c>Items</c> element.
        /// </summary>
        [XmlArray("Items")]
        public List<Product> Products { get; set; } = [];

        #endregion

    }

}
