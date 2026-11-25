namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A payment made by bank transfer.
    /// </summary>
    public sealed class BankTransfer : Payment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the reference the bank gave the transfer.
        /// </summary>
        public string? Reference { get; set; }

        #endregion

    }

}
