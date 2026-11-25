using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// An invoice that holds another object and a list of objects.
    /// </summary>
    public sealed class Invoice
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the invoice number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the customer the invoice is sent to.
        /// </summary>
        public InvoiceCustomer? BillTo { get; set; }

        /// <summary>
        /// Gets or sets the lines of the invoice.
        /// </summary>
        public List<InvoiceLine> Lines { get; set; } = [];

        #endregion

    }

}
