using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds objects as repeated elements, for reference handling of collection items.
    /// </summary>
    public sealed class RefList
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the items, written as repeated <c>Items</c> elements.
        /// </summary>
        public List<RefLeaf?> Items { get; set; } = [];

        #endregion

    }

}
