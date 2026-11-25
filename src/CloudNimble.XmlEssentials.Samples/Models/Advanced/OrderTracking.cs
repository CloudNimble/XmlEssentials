namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// The state of one order, with two enumeration members.
    /// </summary>
    public sealed class OrderTracking
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the order number.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets how urgent the order is.
        /// </summary>
        public Priority Priority { get; set; }

        /// <summary>
        /// Gets or sets how far the order has come.
        /// </summary>
        public OrderStatus Status { get; set; }

        #endregion

    }

}
