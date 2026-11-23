namespace CloudNimble.XmlEssentials.Samples.Models.Reference
{

    /// <summary>
    /// Specifies how far an order has come.
    /// </summary>
    public enum OrderStatus
    {

        /// <summary>
        /// The order has been placed and not yet sent.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The order has left the warehouse.
        /// </summary>
        Shipped = 1,

        /// <summary>
        /// The order has reached the customer.
        /// </summary>
        Delivered = 2,

    }

}
