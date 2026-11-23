using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A catalog of products discriminated by a <c>kind</c> attribute.
    /// </summary>
    public sealed class KindCatalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the products.
        /// </summary>
        public List<KindProduct> Items { get; set; } = [];

        #endregion

    }

}
