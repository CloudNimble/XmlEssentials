using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members whose attributes have the same local name, one in no namespace and one in a namespace, as <c>href</c> and <c>xlink:href</c> are on a link.
    /// </summary>
    public sealed class HrefPair
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of <c>href</c> in no namespace. It is declared first, so it is the member tried first.
        /// </summary>
        [XmlAttribute("href")]
        public string? Plain { get; set; }

        /// <summary>
        /// Gets or sets the value of <c>href</c> in <c>urn:a</c>.
        /// </summary>
        [XmlAttribute("href", Namespace = "urn:a")]
        public string? Qualified { get; set; }

        #endregion

    }

}
