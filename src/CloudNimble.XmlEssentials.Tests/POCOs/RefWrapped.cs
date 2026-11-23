using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds objects inside a wrapper element, for reference handling of wrapped collection items.
    /// </summary>
    public sealed class RefWrapped
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the items, written as <c>RefLeaf</c> elements inside one <c>Items</c> element.
        /// </summary>
        [XmlArray]
        public List<RefLeaf?> Items { get; set; } = [];

        #endregion

    }

}
