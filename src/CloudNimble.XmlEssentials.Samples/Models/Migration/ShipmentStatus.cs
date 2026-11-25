namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// Specifies where a <see cref="Shipment"/> is.
    /// </summary>
    public enum ShipmentStatus
    {

        /// <summary>
        /// The shipment has not left the warehouse.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The shipment is on its way.
        /// </summary>
        InTransit = 1,

        /// <summary>
        /// The shipment has arrived.
        /// </summary>
        Delivered = 2,

    }

}
