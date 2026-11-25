
namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// One line of a <see cref="Customer"/> order.
    /// </summary>
    public sealed class OrderLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the stock keeping unit.
        /// </summary>
        public string? Sku { get; set; }

        #endregion

    }

}
