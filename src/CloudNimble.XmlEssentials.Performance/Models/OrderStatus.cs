namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// The state of an <see cref="Order"/>.
    /// </summary>
    public enum OrderStatus
    {

        /// <summary>
        /// Received and not yet packed.
        /// </summary>
        Pending,

        /// <summary>
        /// Packed and waiting for a carrier.
        /// </summary>
        Packed,

        /// <summary>
        /// Handed to a carrier.
        /// </summary>
        Shipped,

    }

}
