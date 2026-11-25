namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// The customer on an <see cref="Invoice"/>.
    /// </summary>
    public sealed class InvoiceCustomer
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the customer.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the email address invoices are sent to.
        /// </summary>
        public string? Email { get; set; }

        #endregion

    }

}
