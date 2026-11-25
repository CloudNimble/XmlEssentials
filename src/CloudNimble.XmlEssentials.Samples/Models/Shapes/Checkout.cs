namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// An order at checkout, with the payment that settles it.
    /// </summary>
    public sealed class Checkout
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the order number.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the payment.
        /// </summary>
        public Payment? Payment { get; set; }

        #endregion

    }

}
