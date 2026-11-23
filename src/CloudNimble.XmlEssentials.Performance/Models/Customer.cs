using System.Collections.Generic;
using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A nested object: two child objects, an unwrapped list of objects, and a wrapped list of strings.
    /// </summary>
    public sealed class Customer
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the billing address.
        /// </summary>
        public Address? Billing { get; set; }

        /// <summary>
        /// Gets or sets the order lines, one <c>Lines</c> element each.
        /// </summary>
        [Bcl.XmlElement("Lines")]
        public List<OrderLine>? Lines { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the shipping address.
        /// </summary>
        public Address? Shipping { get; set; }

        /// <summary>
        /// Gets or sets the tags, as <c>Tag</c> elements inside a <c>Tags</c> wrapper.
        /// </summary>
        [Bcl.XmlArrayItem("Tag")]
        [Xe.XmlArray]
        [Xe.XmlArrayItem("Tag")]
        public List<string>? Tags { get; set; }

        #endregion

    }

}
