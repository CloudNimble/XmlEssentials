using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members whose attributes have the same local name in the same namespace, which is one name twice.
    /// </summary>
    public sealed class QualifiedAttributeTwins
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first value.
        /// </summary>
        [XmlAttribute("href", Namespace = "urn:a")]
        public string? First { get; set; }

        /// <summary>
        /// Gets or sets the second value.
        /// </summary>
        [XmlAttribute("href", Namespace = "urn:a")]
        public string? Second { get; set; }

        #endregion

    }

}
