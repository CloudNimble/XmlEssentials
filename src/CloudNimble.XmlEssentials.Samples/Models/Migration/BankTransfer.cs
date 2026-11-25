namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A <see cref="Payment"/> made by bank transfer.
    /// </summary>
    public sealed class BankTransfer : Payment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the transfer reference.
        /// </summary>
        public string? Reference { get; set; }

        #endregion

    }

}
