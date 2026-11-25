using System;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A purchase order with one value of each common kind: a number, a string, a date, and a decimal.
    /// </summary>
    public sealed class PurchaseOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the order number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the customer who placed the order.
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the date the order was placed.
        /// </summary>
        public DateOnly Placed { get; set; }

        /// <summary>
        /// Gets or sets the order total.
        /// </summary>
        public decimal Total { get; set; }

        #endregion

    }

}
