namespace CloudNimble.XmlEssentials.Samples.Models.Reference
{

    /// <summary>
    /// A delivery of one order by a carrier.
    /// </summary>
    public sealed class Delivery
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the carrier, which can be a <see cref="CourierCarrier"/>.
        /// </summary>
        public Carrier? Carrier { get; set; }

        /// <summary>
        /// Gets or sets the number of the order that is delivered.
        /// </summary>
        public int OrderId { get; set; }

        #endregion

    }

}
