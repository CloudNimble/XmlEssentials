using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members whose elements have the same local name, one in no namespace and one in a namespace, as <c>link</c> and <c>atom:link</c> are in a feed.
    /// </summary>
    public sealed class LinkPair
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of <c>x</c> in no namespace. It is declared first, so it is the member tried first.
        /// </summary>
        [XmlElement("x")]
        public string? Plain { get; set; }

        /// <summary>
        /// Gets or sets the value of <c>x</c> in <c>urn:a</c>.
        /// </summary>
        [XmlElement("x", "urn:a")]
        public string? Qualified { get; set; }

        #endregion

    }

}
