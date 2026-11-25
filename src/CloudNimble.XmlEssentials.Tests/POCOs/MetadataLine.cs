namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type created through a constructor whose first parameter is a value type, with one member set afterwards.
    /// </summary>
    /// <param name="Qty">The quantity.</param>
    /// <param name="Sku">The stock-keeping unit.</param>
    public sealed record MetadataLine(int Qty, string Sku)
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public decimal Price { get; set; }

        #endregion

    }

}
