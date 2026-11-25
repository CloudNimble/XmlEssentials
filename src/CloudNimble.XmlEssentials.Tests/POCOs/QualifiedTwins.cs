using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members mapped to the same local name in the same namespace, which is one name.
    /// </summary>
    public sealed class QualifiedTwins
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first value.
        /// </summary>
        [XmlElement("x", "urn:a")]
        public string? First { get; set; }

        /// <summary>
        /// Gets or sets the second value.
        /// </summary>
        [XmlElement("x", "urn:a")]
        public string? Second { get; set; }

        #endregion

    }

}
