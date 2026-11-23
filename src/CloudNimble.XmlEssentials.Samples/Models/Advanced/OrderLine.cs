namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// One line of an <see cref="Order"/>.
    /// </summary>
    public sealed class OrderLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the number of units ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the stock keeping unit of the product.
        /// </summary>
        public string? Sku { get; set; }

        #endregion

    }

}
