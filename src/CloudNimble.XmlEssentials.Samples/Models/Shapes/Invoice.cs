using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// An invoice whose root element, one child element, and one attribute are each in a namespace.
    /// </summary>
    [XmlRoot("Invoice", Namespace = "urn:example:billing")]
    public sealed class Invoice
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the invoice number, written as an attribute in no namespace.
        /// </summary>
        [XmlAttribute("number")]
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the revision of the audit record, written as an attribute in the audit namespace.
        /// </summary>
        [XmlAttribute("revision", "urn:example:audit")]
        public int Revision { get; set; }

        /// <summary>
        /// Gets or sets the customer name. The element has no namespace of its own, so it is written in the namespace of the invoice.
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the delivery note, written as an element in the shipping namespace.
        /// </summary>
        [XmlElement("Note", "urn:example:shipping")]
        public string? Note { get; set; }

        #endregion

    }

}
