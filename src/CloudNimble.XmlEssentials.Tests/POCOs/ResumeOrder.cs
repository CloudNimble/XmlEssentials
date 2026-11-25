using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An object with a member of every kind that a read from a stream can stop in the middle of and go on with.
    /// </summary>
    public sealed class ResumeOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets numbers in a wrapper.
        /// </summary>
        [XmlArray("Codes")]
        public List<int>? Codes { get; set; }

        /// <summary>
        /// Gets a value with no setter, which is written and never read.
        /// </summary>
        public string Computed => "computed";

        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the identifier, an attribute.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets strings with no wrapper and no setter, which are written and never read.
        /// </summary>
        public List<string> Legacy { get; } = ["kept"];

        /// <summary>
        /// Gets or sets records in a wrapper.
        /// </summary>
        [XmlArray("Lines")]
        public List<ResumeLine>? Lines { get; set; }

        /// <summary>
        /// Gets or sets the region, which a document must have.
        /// </summary>
        [XmlRequired]
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets an object.
        /// </summary>
        public ResumeAddress? ShipTo { get; set; }

        /// <summary>
        /// Gets or sets objects with no wrapper.
        /// </summary>
        public List<ResumeAddress>? Stops { get; set; }

        /// <summary>
        /// Gets or sets strings with no wrapper.
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        public decimal Total { get; set; }

        #endregion

    }

}
