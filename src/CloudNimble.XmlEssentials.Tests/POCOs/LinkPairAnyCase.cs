using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members whose element names differ in case and in namespace, which are one local name when case is ignored.
    /// </summary>
    public sealed class LinkPairAnyCase
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value of <c>x</c> in no namespace.
        /// </summary>
        [XmlElement("x")]
        public string? Plain { get; set; }

        /// <summary>
        /// Gets or sets the value of <c>X</c> in <c>urn:a</c>.
        /// </summary>
        [XmlElement("X", "urn:a")]
        public string? Qualified { get; set; }

        #endregion

    }

}
