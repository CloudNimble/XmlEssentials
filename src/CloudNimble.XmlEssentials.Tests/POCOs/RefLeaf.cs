using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An object that can hold itself, an ancestor, or a sibling among its children, for reference handling.
    /// </summary>
    public sealed class RefLeaf
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the children, written as repeated <c>Children</c> elements.
        /// </summary>
        public List<RefLeaf?> Children { get; set; } = [];

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
