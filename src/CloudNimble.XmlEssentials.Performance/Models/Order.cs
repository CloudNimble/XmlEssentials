using System;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A flat object of mixed scalars, mapped as child elements by every library.
    /// </summary>
    public sealed class Order
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets free text.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets when the order was placed.
        /// </summary>
        public DateTime Placed { get; set; }

        /// <summary>
        /// Gets or sets the sequence number.
        /// </summary>
        public long Sequence { get; set; }

        /// <summary>
        /// Gets or sets whether the order shipped.
        /// </summary>
        public bool Shipped { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the weight.
        /// </summary>
        public double Weight { get; set; }

        #endregion

    }

}
