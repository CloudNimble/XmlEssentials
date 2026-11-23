using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A catalog of <see cref="BadKindProduct"/> used to bind the polymorphic attribute on a collection item type.
    /// </summary>
    public sealed class BadKindCatalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the products.
        /// </summary>
        public List<BadKindProduct> Items { get; set; } = [];

        #endregion

    }

}
