namespace CloudNimble.XmlEssentials.Samples.Models.Dom
{

    /// <summary>
    /// A product in a catalog, written as one child element per property.
    /// </summary>
    public sealed class Product
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the product's name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the price of one unit.
        /// </summary>
        public decimal Price { get; set; }

        #endregion

    }

}
