namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// Specifies how far an order has come.
    /// </summary>
    public enum OrderStatus
    {

        /// <summary>
        /// The order is received and not yet packed.
        /// </summary>
        Open,

        /// <summary>
        /// The order is packed and waiting for a carrier.
        /// </summary>
        ReadyToShip,

        /// <summary>
        /// The order is with the carrier.
        /// </summary>
        Shipped,

    }

}
