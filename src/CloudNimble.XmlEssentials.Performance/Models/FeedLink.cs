using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// An Atom link: an empty element with attributes.
    /// </summary>
    public sealed class FeedLink
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [Bcl.XmlAttribute("href")]
        [Xe.XmlAttribute("href")]
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the relation.
        /// </summary>
        [Bcl.XmlAttribute("rel")]
        [Xe.XmlAttribute("rel")]
        public string? Rel { get; set; }

        #endregion

    }

}
