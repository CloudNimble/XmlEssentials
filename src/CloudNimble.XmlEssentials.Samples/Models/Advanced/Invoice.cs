using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// An invoice with a <see cref="Money"/> element and two dates, one of them written as an attribute.
    /// </summary>
    public sealed class Invoice
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the day the invoice is due, written as an element.
        /// </summary>
        public DateOnly Due { get; set; }

        /// <summary>
        /// Gets or sets the day the invoice was issued, written as an attribute.
        /// </summary>
        [XmlAttribute]
        public DateOnly Issued { get; set; }

        /// <summary>
        /// Gets or sets the invoice number.
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Gets or sets the invoice total.
        /// </summary>
        public Money Total { get; set; }

        #endregion

    }

}
