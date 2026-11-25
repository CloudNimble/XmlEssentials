using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A catalog that holds products of several types.
    /// </summary>
    public sealed class Catalog
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the products.
        /// </summary>
        public List<Product> Products { get; set; } = [];

        #endregion

    }

}
