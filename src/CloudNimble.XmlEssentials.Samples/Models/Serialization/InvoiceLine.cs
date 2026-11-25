namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// One line of an <see cref="Invoice"/>.
    /// </summary>
    public sealed class InvoiceLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the stock keeping unit of the product.
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Gets or sets the number of units.
        /// </summary>
        public int Quantity { get; set; }

        #endregion

    }

}
