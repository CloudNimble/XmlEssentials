namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A receipt. No context lists this type, so the serializer maps it with reflection.
    /// </summary>
    public sealed class Receipt
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the receipt number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the amount paid.
        /// </summary>
        public decimal Paid { get; set; }

        #endregion

    }

}
