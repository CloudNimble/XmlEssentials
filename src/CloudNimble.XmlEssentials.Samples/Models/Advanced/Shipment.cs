namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A shipment of one order.
    /// </summary>
    public sealed class Shipment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the carrier.
        /// </summary>
        public string? Carrier { get; set; }

        /// <summary>
        /// Gets or sets the number of the order that is shipped.
        /// </summary>
        public int OrderId { get; set; }

        #endregion

    }

}
