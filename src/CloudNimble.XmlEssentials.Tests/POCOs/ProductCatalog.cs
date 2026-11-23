using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A catalog of polymorphic products written as unwrapped derived elements.
    /// </summary>
    public sealed class ProductCatalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the products.
        /// </summary>
        public List<Product> Items { get; set; } = [];

        #endregion

    }

}
